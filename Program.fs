// Learn more about F# at http://fsharp.org
module Program
open CommonTop
open CommonData
open Expecto
open Helpers
open VisualTest
open DPExecution

let instrLst = [
        "LSL R0, R1, #2";
        "LSLS r0, r1, #0b101";
        "LSL r0, r1, #0xe";
        "LSLS r0, r1, #&f";
        "LSL R0, R1, R2";
        "LSR R0, R1, #2";
        "LSRS r0, r1, #0b101";
        "LSR r0, r1, #0xe";
        "LSRS r0, r1, #&f";
        "LSR R0, R1, R2";
        "ASRS R0, R1, #2";
        "ASR r0, r1, #0b101";
        "ASRS r0, r1, #0xe";
        "ASR r0, r1, #&f";
        "ASR R0, R1, R2";
        "RORS R0, R1, #2";
        "ROR r0, r1, #0b101";
        "ROR r0, r1, #0xe";
        "RORS r0, r1, #&f";
        "ROR R0, R1, R2"; 
        "RRXS R0, R1";
        "RRXS R12, R12";
        "LDRB r0, [r1, r2]!"
        "LDR r0, [r1]";
        "LDRB r0, [r1], #4"
        "LDR r0, [r1, #4]!"
        "LDRB r0, [r1, #4]";
        "LDR r0, [r1, #0x7]";
        "LDRB r0, [r1, #&8]";
        "LDR r0, [r1, #0b10110]";
        "STRB r0, [r1, r2]";
        "STR r0, [r1]";
        "STRB r0, [r1, #4]";
        "STR r0, [r1, #0x7]";
        "STRB r0, [r1, #&8]";
        "STR r0, [r1, #0b10110]"
        "LDMIA r0, {r1, r2}";
        "LDMIB r0, {r1, r2, r3, r4, r5}";
        "STMDA r0, {r1-r4}";
        "STMDB r0, {r1-r3, r7, r8}";
        "LDM r0!, {r0-r15}";
        "MOV r0, r1";
        "MOVS r1, r1";
        "MOV r3, #4";
        "MVNS r4, #0x56";
        "MVN r6, r7";
    ]

let parseREPL cpuData =
    let rec repl'() =
        printf  "~> "
        System.Console.ReadLine().ToUpper()
        |> parseLine None (WA 0u) 
        |> qp
        repl'()
    repl'()

let exeREPL cpuData =
    let printRegs cpuData =
        cpuData.Regs |> Map.toList |> qpl |> ignore
    let printFlags cpuData =
         cpuData.Fl |> qp |> ignore
    
    printRegs cpuData
    printFlags cpuData

    let rec repl' cpuData =
        printf  "~> "
        System.Console.ReadLine().ToUpper()
        |> parseLine None (WA 0u)
        |> function
        | Error e ->
            e |> qp
            repl' cpuData
        | Ok instr ->
            execute instr cpuData
            |> function
            | cpuData' ->
              printRegs cpuData'
              printFlags cpuData'
              repl' cpuData'
            | _ ->
                "Nope" |> qp
                repl' cpuData
    repl' cpuData
    


[<EntryPoint>]
let main argv =
    match argv with
<<<<<<< HEAD
        | [|"tests"|] -> runTestsInAssembly defaultConfig [||]
=======
        | [|"tests"|] -> 
            "Running all Expecto tests..." |> qp
            runTestsInAssembly defaultConfig [||]
        | [|"vtests"|] -> 
            "Running visUAL based tests..." |> qp
            VProgram.runVisualTests ()
        | [|"repl"|] ->
            "Doug's Remarkable REPL..." |> qp
            let cpuData = initDP false false false false [0u]
            exeREPL cpuData
>>>>>>> Work
        | _ -> 
            List.map (parseLine None (WA 0u)) instrLst
            |> qpl
            |> ignore
            0 // return an integer exit code
