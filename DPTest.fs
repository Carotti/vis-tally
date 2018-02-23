module DPTests

open Expecto

open CommonTop
open CommonData
open DP
open DPExecution

open VisualTest.VCommon
open VisualTest.Visual
open VisualTest.VTest


/// A map to convert between `RName` and a numerical register name. This is used
///  to interface with the `Out` type.
let regNums =
    Map.ofList [ 
        R0,0 ; R1,1 ; R2,2 ; R3,3 ; R4,4 ; R5,5
        R6,6 ; R7,7 ; R8,8 ; R9,9 ; R10,10 ; R11,11 ; 
        R12,12 ; R13,13 ; R14,14 ; R15,15 ;
    ] 

/// A function to initialise data paths based on register and CPSR values.
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
                [0u..15u]
                |> List.zip [0u..15u]
                |> List.map (fun (r, _v) -> (consRegG r, 0u))
                |> Map.ofList

        Map.add R15 0u regsNoPC
            
    {
        Fl = flags; 
        Regs = fillRegs regVals; 
        MM = Map.empty<WAddr,MemLoc<DP.Instr>>
    }        

/// A function to construct a register-value tuple from the VisUAL framework format.
let consDPReg (r:Out,value:int) =
        match r with
        | R n ->
            n
            |> string
            |> (+) "R"
            |> consReg, value|>uint32

/// A function to print the hexadecimal values of registers in a datapath.
let printRegs (dp:DataPath<Instr>) =
    dp.Regs |> Map.toList |> List.map (fun (r, v) -> printfn "%A : %x" r v) |> ignore
    
/// A function to print the flags in a datapath.
let printFlags (dp:DataPath<Instr>) =
     dp.Fl |> qp |> ignore
         
/// A function to convert between the `VisOuput` and `DataPath` formats.
///  This only looks at registers and flags.
let visOutToDP visOut = 
    let flags =
        {
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

/// A function to convert between the `VisOutput` and `Params` formats.
///  This only looks at registers and flags.
let visOutToVisParams inParams visOut =
    let flags =
        {
            FN = visOut.State.VFlags.FN; 
            FC = visOut.State.VFlags.FC;
            FZ = visOut.State.VFlags.FZ;
            FV = visOut.State.VFlags.FV;
        }

    let regs =
        visOut.Regs
        |> List.map (consDPReg)
        |> List.filter (fun (r,_v) -> (r <> R15))
        |> List.map snd
    
    {inParams with InitFlags = flags; InitRegs = regs} 

/// A function to convert between the `Params` and `VisOut` formats.
///  This only looks at registers and flags.
let visParamsToVisOut (visParams:Params) =
    let flags =
        {
            FN = visParams.InitFlags.FN; 
            FC = visParams.InitFlags.FC;
            FZ = visParams.InitFlags.FZ;
            FV = visParams.InitFlags.FV;
        }
    let regs =
        let size = List.length visParams.InitRegs
        List.zip [0..(size-1)] visParams.InitRegs
        |> List.map (fun (r,v) -> R r, v |> int)
    {
        Regs = regs
        RegsAfterPostlude = []
        State = {VFlags = flags; VMemData = []}
    }

/// A function to convert between the `DataPath` and `VisOut` formats.
///  This only looks at registers and flags.
let DPToVisOut (visDP:DataPath<Instr>) =
    let flags =
        {
            FN = visDP.Fl.N; 
            FC = visDP.Fl.C;
            FZ = visDP.Fl.Z;
            FV = visDP.Fl.V;
        }

    let regs =
        visDP.Regs
        |> Map.toList
        |> List.map (fun (r,v) -> R regNums.[r], v |> int)
    {
        Regs = regs
        RegsAfterPostlude = []
        State = {VFlags = flags; VMemData = []}
    }
    
/// A function to determine if two data paths are equal. This does not take into
///  account the value of the program counter.  
let equalDP (dp1:DataPath<Instr>) (dp2:DataPath<Instr>) =
    let regs1 = Map.remove R15 dp1.Regs
    let regs2 = Map.remove R15 dp2.Regs
    {dp1 with Regs = regs1} = {dp2 with Regs = regs2}

/// A function that, given a line of assembly, register and CPSR values, parses
///  executes and compares to VisUAL in an error safe way.
let visCompare src regs n c z v =
    let flags = {FN=n; FC=c;FZ=z; FV=v}
    let regs' =
        match List.length regs with
        | 15 -> regs
        | _ -> List.map (fun _i -> 0u) [0..14]
    let param = {testParas with InitFlags = flags; InitRegs = regs'}
    let dp = initialiseDP n c z v regs'
    src
    |> parseLine None (WA 0u)
    |> function
    | Ok instr ->
        covertToDP instr
        |> executeDP dp
        |> function
        | Ok localDP ->
            let visDP =
                RunVisualWithFlagsOut param src
                |> snd
                |> visOutToDP
            // Commented-out code below is used to debug differences between local and VisUAL executions
            // printRegs visDP
            // printFlags visDP
            // printRegs localDP
            // printFlags localDP
            equalDP localDP visDP
        | Error e ->
            e |> qp
            false
    | Error e ->
        e |> qp
        false

/// A function that executes an instruction locally and on VisUAL and then outputs
///  the local data path and the VisUAL output.
let processDP src (visDP:VisOutput) (localDP:DataPath<Instr>) =
    src
    |> parseLine None (WA 0u)
    |> function
    | Ok instr ->
        covertToDP instr
        |> executeDP localDP
        |> function
        | Ok dp' ->
            let visOut = (RunVisualWithFlagsOut (visDP |> (visOutToVisParams testParas)) src) |> snd
            visOut, dp'
        | Error e ->
            e |> qp
            (visDP, localDP)
    | Error e ->
        e |> qp
        (visDP, localDP)

/// A function that executes a sequence of assembly instructions locally and on
/// VisUAL and comapre the two data paths.
let visCompareSequence srcLst regs n c z v =
    let srcLst' = List.map (fun (i:string) -> i.ToUpper()) srcLst
    let flags = {FN=n; FC=c;FZ=z; FV=v}
    let regs' =
        match List.length regs with
        | 15 -> regs
        | _ -> List.map (fun _i -> 0u) [0..14]
    let visDP = {testParas with InitFlags = flags; InitRegs = regs'}
    let visIn = visParamsToVisOut visDP
    let localDP = initialiseDP n c z v regs'

    srcLst'
    |> List.fold (fun (vDP,lDP) src -> processDP src vDP lDP) (visIn,localDP)
    |> fun (vOut, lOut) -> ((visOutToDP) vOut, lOut)
    ||> equalDP

/// A more expressive version of `visCompareSequence` used to debug tests.
let visCompareSequenceShow srcLst regs n c z v =
    let srcLst' = List.map (fun (i:string) -> i.ToUpper()) srcLst
    let flags = {FN=n; FC=c;FZ=z; FV=v}
    let regs' =
        match List.length regs with
        | 15 -> regs
        | _ -> List.map (fun _i -> 0u) [0..14]
    let visDP = {testParas with InitFlags = flags; InitRegs = regs'}
    let visIn = visParamsToVisOut visDP
    let localDP = initialiseDP n c z v regs'

    srcLst'
    |> List.fold (fun (vDP,lDP) src -> processDP src vDP lDP) (visIn,localDP)
    |> fun (vOut, lOut) -> ((visOutToDP) vOut, lOut)
    |> fun (vDP, lDP) ->
        "******************* Visual *******************" |> qp
        printRegs vDP
        printFlags vDP
        "******************* Local *******************" |> qp
        printRegs lDP
        printFlags lDP

/// A function for running an Expecto unit test and comparing to VisUAL.
let visUnitTest name src regs1 n c z v =
    let restOfRegsNumber = 15 - List.length regs1 |> uint32
    let regs2 = [1u..restOfRegsNumber]
    let regs' = List.append regs1 regs2
    testCase name <| fun() ->
        Expecto.Expect.equal (visCompare src regs' n c z v) true <|
            "Implementation doesn't agree with VisUAL"

/// A function for running an Expecto unit test on a sequence of instructions
///  and comparing to VisUAL.
let visSequenceTest name srcLst regs1 n c z v =
    let restOfRegsNumber = 16 - List.length regs1 |> uint32
    let regs2 = [1u..restOfRegsNumber]
    let regs' = List.append regs1 regs2
    testCase name <| fun() ->
        Expecto.Expect.equal (visCompareSequence srcLst regs' n c z v) true <|
            "Implementation doesn't agree with VisUAL"

/// A function to allow for property based testing of three-register-operand instructions.
let visTest3Params src (r0,r1,r2) n c z v =
    let regs = r0 :: r1 :: r2 :: [0u..11u]
    visCompare src regs n c z v

/// A function to allow for property based testing of four-register-operand instructions.
let visTest4Params src (r0,r1,r2,r3) n c z v =
    let regs = r0 :: r1 :: r2 :: r3 :: [0u..10u]
    visCompare src regs n c z v
