﻿// Learn more about F# at http://fsharp.org
module Program

    open Expecto
    
    open CommonTop
    open CommonData

    [<EntryPoint>]
    let rec main argv =
        match argv with
        | [|"tests"|] -> runTestsInAssembly defaultConfig [||]
        | _ ->
            printfn "########################"
            let test = parseLine None (WA 16u) <| System.Console.ReadLine()
            printfn "%A" test
            main argv |> ignore
            0 // return an integer exit code