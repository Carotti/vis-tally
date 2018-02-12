// Learn more about F# at http://fsharp.org
module Program
open CommonTop
open CommonData
open CommonLex

[<EntryPoint>]
let main _argv =
    /// test the initProjectLexer code
    let symTable : SymbolTable = Map.ofList [
                                    "foo", 10u;
                                ]
    let test = parseLine (Some symTable) (WA 16u) "FOO DCD foo, foo"
    printfn "%A" test
    0 // return an integer exit code