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

    [<EntryPoint>]
    let rec main argv =
        match argv with
        | [|"tests"|] -> VProgram.runVisualTests ()
        | [|"repl"|] ->
            printfn "########################"
            let test = parseLine None (WA 16u) <| System.Console.ReadLine()
            printfn "%A" test
            main argv |> ignore
            0 // return an integer exit code
        | _ ->
            initCaches testParas
            let testStr = "foo DCD 19, 11, 12, 13, 14, 16, 17"
            compareDpDataMem (runMisc testStr) (runVisualGetMem testStr) |> printfn "%A"
            finaliseCaches testParas
            0
