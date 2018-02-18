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
            let test = parseLine None (WA 16u) "FILL 1, #, 7"
            printfn "%A" test
            0 // return an integer exit code