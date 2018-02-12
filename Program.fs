// Learn more about F# at http://fsharp.org
module Program
open CommonTop
open CommonData

[<EntryPoint>]
let main _argv =
    /// test the initProjectLexer code
    let test = parseLine None (WA 16u) "FOO DCD 16, 17, 18"
    printfn "%A" test
    0 // return an integer exit code