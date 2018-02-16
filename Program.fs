// Learn more about F# at http://fsharp.org
module Program

    open Expecto
    
    open CommonTop
    open CommonData

    [<EntryPoint>]
    let main argv =
        match argv with
        | [|"tests"|] -> runTestsInAssembly defaultConfig [||]
        | _ ->
            let test = parseLine None (WA 16u) "foo DCD 1324 - (1 + ((foo - 3) * 4))"
            printfn "%A" test
            0 // return an integer exit code