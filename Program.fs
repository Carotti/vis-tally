// Learn more about F# at http://fsharp.org
module Program

    open Expecto
    
    open CommonTop
    open CommonData

    let ts = Map.ofList [
                    "a", 192u
                    "moo", 17123u
                    "J", 173u
                    "fOO", 402u
                    "Bar", 19721u
                    "z1", 139216u
                    "rock74", 16u
                    "Nice1", 0xF0F0F0F0u
                    "Nice2", 0x0F0F0F0Fu
                    "bigNum", 0xFFFFFFFFu
                    "n0thing", 0u
                ] |> Some

    [<EntryPoint>]
    let main argv =
        match argv with
        | [|"tests"|] -> runTestsInAssembly defaultConfig [||]
        | _ ->
            let test = parseLine ts (WA 16u) "FOO DCD a - J"
            printfn "%A" test
            0 // return an integer exit code