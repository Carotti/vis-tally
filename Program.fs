// Learn more about F# at http://fsharp.org
module Program
open CommonTop
open CommonData
open Expecto
open Helpers

[<EntryPoint>]
let main argv =
    /// test the initProjectLexer code
    let instrLst = [
        // "LSL R0, R1, #2";
        // "LSL r0, r1, #0b101";
        // "LSL r0, r1, #0xe";
        // "LSL r0, r1, #&f";
        // "LSL R0, R1, R2";
        // "LSR R0, R1, #2";
        // "LSR r0, r1, #0b101";
        // "LSR r0, r1, #0xe";
        // "LSR r0, r1, #&f";
        // "LSR R0, R1, R2";
        // "ASR R0, R1, #2";
        // "ASR r0, r1, #0b101";
        // "ASR r0, r1, #0xe";
        // "ASR r0, r1, #&f";
        // "ASR R0, R1, R2";
        // "ROR R0, R1, #2";
        // "ROR r0, r1, #0b101";
        // "ROR r0, r1, #0xe";
        // "ROR r0, r1, #&f";
        // "ROR R0, R1, R2"; 
        // "RRX R0, R1";
        // "RRX R12, R12";
        "LDR r0, [r1, r2]!"
        // "LDR r0, [r1]";
        // "LDR r0, [r1], #4"
        // "LDR r0, [r1, #4]!"
        // "LDR r0, [r1, #4]";
        // "LDR r0, [r1, #0x7]";
        // "LDR r0, [r1, #&8]";
        // "LDR r0, [r1, #0b10110]";
        // "STR r0, [r1, r2]";
        // "STR r0, [r1]";
        // "STR r0, [r1, #4]";
        // "STR r0, [r1, #0x7]";
        // "STR r0, [r1, #&8]";
        // "STR r0, [r1, #0b10110]"
        // "LDM r0, {r1, r2}";
        // "LDM r0, {r1, r2, r3, r4, r5}";
        // "STM r0, {r1-r4}";
        // "STM r0, {r1-r3, r7, r8}";
        // "LDM r0!, {r0-r15}";
    ]
    "Enter \"tests\" to run the test suite, for execution anything else" |> qp
    match argv with
        | [|"tests"|] -> runTestsInAssembly defaultConfig [||]
        | _ -> 
            List.map (parseLine None (WA 0u)) instrLst
            |> qpl
            |> ignore
            0 // return an integer exit code
