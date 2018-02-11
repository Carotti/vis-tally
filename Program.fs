// Learn more about F# at http://fsharp.org
module Program
open System
open CommonTop
open CommonData

[<EntryPoint>]
let main argv =
    /// test the initProjectLexer code
    let test = parseLine None (WA 0u)
    printfn "%A" "Hi"
    0 // return an integer exit code
    // This is a test
