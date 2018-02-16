// Learn more about F# at http://fsharp.org
module Program
open System
open CommonTop
open CommonData

let qp item = printfn "%A" item
let qpl lst = List.map (qp) lst

[<EntryPoint>]
let main argv =
    /// test the initProjectLexer code
    let instrLst = [
        // "LSL R0, R1, #2";
        // "LSR r0, r1, #0b101";
        // "ASR r0, r1, #0xe";
        // "ROR r0, r1, #&f";
        // "LSL R0, R1, R2"; 
        // "RRX R0, R1";
        "LDR r0, [r1, r2]";
        "LDR r0, [r1]";
        "LDR r0, [r1, #4]";
        "LDR r0, [r1, #0x7]";
        "LDR r0, [r1, #&8]";
        "LDR r0, [r1, #0b10110]";
        "STR r0, [r1, r2]";
        "STR r0, [r1]";
        "STR r0, [r1, #4]";
        "STR r0, [r1, #0x7]";
        "STR r0, [r1, #&8]";
        "STR r0, [r1, #0b10110]"
    ]

    List.map (parseLine None (WA 0u)) instrLst
    |> qpl
    |> ignore
    0 // return an integer exit code
    // This is a test
