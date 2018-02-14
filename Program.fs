// Learn more about F# at http://fsharp.org
module Program

    open Expecto
    
    open CommonTop
    open CommonData
    open CommonLex

    [<EntryPoint>]
    let main argv =
        match argv with
        | [|"tests"|] -> runTestsInAssembly defaultConfig [||]
        | _ ->
            /// test the initProjectLexer code
            let symTable : SymbolTable = Map.ofList [
                                            "foo", 10u;
                                        ]
            let test = parseLine (Some symTable) (WA 16u) "FOO DCD 0x5 + 0xC"
            printfn "%A" test
            0 // return an integer exit code