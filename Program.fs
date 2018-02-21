// Learn more about F# at http://fsharp.org
module Program

    open Expecto
    
    open CommonTop
    open CommonData
    open CommonLex

    open VisualTest
    open Visual
    open VTest
    open TestTop

    open MiscTest

    open Execution

    open TestFormats

    [<EntryPoint>]
    let rec main argv =
        match argv with
        | [|"tests"|] -> runVisualTests ()
        | [|"repl"|] ->
            printfn "########################"
            let test = parseLine None (WA 16u) <| System.Console.ReadLine()
            printfn "%A" test
            main argv |> ignore
            0 // return an integer exit code
        | _ ->
            runMisc "FILL 17, 0xAB" |> printfn "%A"
            // initCaches testParas
            // testPropertyVis "DCD" sameAsVisualDCD
            // |> runTests defaultConfig |> ignore
            // finaliseCaches testParas
            0            
