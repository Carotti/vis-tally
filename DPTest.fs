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


    //  VisOutput 

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

let exeREPL (dp:DataPath<Instr>) =
    printRegs dp
    printFlags dp

let zeroTest src =
    let flags = {FN=false;FZ=false; FC=false;FV=false}
    let regs = List.map (fun _i -> 0u) [0..14]
    let param = {defaultParas with InitFlags = flags; InitRegs = regs}
    let dp = initialiseDP false false false false regs
    src
    |> parseLine None (WA 0u)
    |> function
    | Ok instr ->
        covertToDP instr
        |> executeDP dp
        |> function
        | Ok dp' ->
            initCaches param
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
        




    