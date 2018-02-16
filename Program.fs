// Learn more about F# at http://fsharp.org
module Program
open System
open CommonTop
open CommonData
open DP


////////////////////////////////////////////////////////////////////////////////
// maccth helper functions, to delete 
let qp thing = thing |> printfn "%A"
let qpl lst = lst |> List.map (qp)
// maccth helper functions, to delete 
////////////////////////////////////////////////////////////////////////////////

[<EntryPoint>]
let main argv =
    /// test the initProjectLexer code
    let instrLst = [
        "hello: ADD R1, R2, #0xf000000f";
        "ADD R2, R4, #0xf8000007";
        "ADD R20, R4, #0xf8000007";
        "ADD R1, R2, R3, R4";
        "ADD R0, R1, R2 ";
    ]

    instrLst
    |> List.map (fun instr -> (instr + "\n", parseLine None (WA 0u) instr))
    |> qpl
    |> ignore
    // let test = parseLine None (WA 0u) "ADD R1, R2,  #0xf000000f"
    // test |> qp


    

    0 // return an integer exit code
