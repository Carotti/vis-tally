module Program

open Expecto

open CommonTop
open CommonData
open CommonLex
open DP
open DPExecution
open DPTests
open Tests

open VisualTest.Visual
open VisualTest.VTest

open System
open System.Threading
open System.IO

// Comments
let qp thing = thing |> printfn "%A"

let qpl lst = lst |> List.map (qp)

let parseREPL() =
    let rec repl'() =
        printf  "~> "
        System.Console.ReadLine().ToUpper()
        |> parseLine None (WA 0u) 
        |> qp
        repl'()
    repl'()

let exeREPL (dp:DataPath<Instr>) =
    printRegs dp
    printFlags dp

    let rec repl' (dp:DataPath<Instr>) =
        printf  "~> "
        System.Console.ReadLine().ToUpper()
        |> parseLine None (WA 0u)
        |> function
        | Ok instr ->
            covertToDP instr
            |> executeDP dp
            |> function
            | Ok dp' ->
              printRegs dp'
              printFlags dp'
              repl' dp'
            | Error e' ->
                e' |> qp
                repl' dp
         | Error e ->
            e |> qp
            repl' dp
    repl' dp


[<Tests>]


let expectoConfig = { Expecto.Tests.defaultConfig with 
                        parallel = testParas.Parallel
                        parallelWorkers = 6 // try increasing this if CPU use is less than 100%
                }

let qp item = printfn "%A" item
let qpl lst = List.map (qp) lst

[<EntryPoint>]
let main argv =

    let rec keyHandler() =
        printfn "Enter..."
        printfn "         'p' for a parsing REPL"
        printfn "         'e' for an execution REPL"
        printfn "      or 't' to run some cool tests"
        let key = System.Console.ReadKey()
        match key.KeyChar with
        | 'p'   ->
            "ready to REPL..." |> (printfn "%s")
            parseREPL()
            0
        | 'e'   ->
            "ready to REPL..." |> (printfn "%s")
            let dp = initialiseDP false false false false [0u]
            exeREPL dp
            0 
        | 't'   ->
            initCaches testParas
            let rc = runTestsInAssembly expectoConfig [||]
            finaliseCaches testParas
            System.Console.ReadKey() |> ignore
            0
        |  _    -> keyHandler()


    keyHandler()

    
  


