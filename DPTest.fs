module DPTests

open Expecto
open FsCheck

open CommonTop
open CommonData
open CommonLex
open DP
open DPExecution

open VisualTest.VCommon
open VisualTest.VLog
open VisualTest.Visual
open VisualTest.VTest

open System
open System.Threading
open System.IO

let initialiseDP n c z v (regVals:uint32 list) : DataPath<Instr> =
    let flags =
        {N = n; C = c; Z = z; V = v;}

    let fillRegs (regVals:uint32 list) =
        let regsNoPC =
            match List.length regVals with
            | 15 ->
                regVals
                |> List.zip [0u..14u]
                |> List.map (fun (r,v) -> (consRegG r, v))
                |> Map.ofList
            | _ ->
                [0u..14u]
                |> List.zip [0u..14u]
                |> List.map (fun (r, _v) -> (consRegG r, 0u))
                |> Map.ofList

        Map.add R15 0u regsNoPC
            
    {
        Fl = flags; 
        Regs = fillRegs regVals; 
        MM = Map.empty<WAddr,MemLoc<DP.Instr>>
    }        

let printRegs (dp:DataPath<Instr>) =
    dp.Regs |> Map.toList |> List.map (fun (r, v) -> printfn "%A : %x" r v) |> ignore
    
let printFlags (dp:DataPath<Instr>) =
     dp.Fl |> qp |> ignore
         
let visToDP visOut = 
    let consDPReg (r:Out,value:int) =
        match r with
        | R n ->
            n
            |> string
            |> (+) "R"
            |> consReg, value|>uint32
    let flags = {
                    N = visOut.State.VFlags.FN; 
                    C = visOut.State.VFlags.FC;
                    Z = visOut.State.VFlags.FZ;
                    V = visOut.State.VFlags.FV;
                }
    let regs =
            visOut.Regs
            |> List.map consDPReg 
            |> Map.ofList
    // DP instructions don't care about memory!
    let mem = Map.empty<WAddr,MemLoc<DP.Instr>>
    {Fl = flags; Regs = regs; MM = mem}

let equalDP (dp1:DataPath<Instr>) (dp2:DataPath<Instr>) =
    let regs1 = Map.remove R15 dp1.Regs
    let regs2 = Map.remove R15 dp2.Regs
    {dp1 with Regs = regs1} = {dp2 with Regs = regs2}

let visCompare src regs n c z v =
    let flags = {FN=n; FC=c;FZ=z; FV=v}
    let regs' =
        match List.length regs with
        | 15 -> regs
        | _ -> List.map (fun _i -> 0u) [0..14]
    let param = {defaultParas with InitFlags = flags; InitRegs = regs'}
    let dp = initialiseDP n c z v regs'
    src
    |> parseLine None (WA 0u)
    |> function
    | Ok instr ->
        covertToDP instr
        |> executeDP dp
        |> function
        | Ok dp' ->
            RunVisualWithFlagsOut param src
            |> snd
            |> visToDP
            |> (equalDP dp')
        | Error e ->
            e |> qp
            false
    | Error e ->
        e |> qp
        false

let visUnitTest name src regs1 n c z v =
    let restOfRegsNumber = 15 - List.length regs1 |> uint32
    let regs2 = [1u..restOfRegsNumber]
    let regs' = List.append regs1 regs2
    testCase name <| fun() ->
        Expecto.Expect.equal (visCompare src regs' n c z v) true <|
            "Implementation doesn't agree with VisUAL"

let visTest3Params src (r0,r1,r2) n c z v =
    let regs = r0 :: r1 :: r2 :: [0u..11u]
    visCompare src regs n c z v

let visTest4Params src (r0,r1,r2,r3) n c z v =
    let regs = r0 :: r1 :: r2 :: r3 :: [0u..10u]
    visCompare src regs n c z v

let ADDSpropTest1 =
    let compare src (r0,r1,r2) c z v =
        (visTest3Params src (r0, r1, r2) false c z v)
    testProperty "ADDS, reg, N clear"  <| (compare "ADDS r0, r1, r2") 

let ADDSpropTest2 =
    let compare src (r0,r1,r2) n c v =
        (visTest3Params src (r0, r1, r2) n c false v)
    testProperty "ADDS, reg, Z clear"  <| (compare "ADDS r0, r1, r2")

let ADDSpropTest3 =
    let compare src n (shift:SInstr) (r0,r1,r2,r3) c z v =
        let src' = src + sInstrsStr.[shift] + " r3"
        (visTest4Params src' (r0, r1, r2, r3) n c z v)
    testProperty "ADDS, reg, shift, reg, N clear"  <| (compare "ADDS r0, r1, r2, " false) 

let ADDSpropTest4 =
    let compare src z (shift:SInstr) (r0,r1,r2,r3) n c v =
        let src' = src + sInstrsStr.[shift] + " r3"
        (visTest4Params src' (r0, r1, r2, r3) n c z v)
    testProperty "ADDS, reg, shift, reg, Z clear"  <| (compare "ADDS r0, r1, r2, " false) 


[<Tests>]
// N C Z V
let unitTests = 
    testList "Unit Tests"
        [
            visUnitTest "ADD" "ADD r0, r1, r2, LSL r3" [10u;11u;12u;13u] false false false false
            visUnitTest "ADDS" "ADDS r0, r1, r2" [10u;11u;12u] false false false false
        ]
  
[<Tests>]
// N C Z V
let propTests =
    testList "Property Based Testing"
    [
        ADDSpropTest1
        ADDSpropTest2
        ADDSpropTest3
        ADDSpropTest4
    ]