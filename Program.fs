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
    let test = parseLine None (WA 0u)
    opCodes |> qp

    

    0 // return an integer exit code
