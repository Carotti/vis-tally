// Learn more about F# at http://fsharp.org
module Program
open System
open CommonTop
open CommonData

[<EntryPoint>]
let main argv =
    /// test the initProjectLexer code
    let test = parseLine None (WA 16u) "FOO DCD"
    printfn "%A" test
    0 // return an integer exit code