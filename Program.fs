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
        "LSL R0, R1, #2";
        "LSL r0, r1, #0b101";
        "LSL r0, r1, #0x5";
        "LSL r0, r1, r114";
        "LSL R0, R1, R2"; 
    ]

    List.map (parseLine None (WA 0u)) instrLst
    |> qpl
    |> ignore
    0 // return an integer exit code
    // This is a test
