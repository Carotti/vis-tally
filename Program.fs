module Program

open Expecto
open FsCheck

open CommonTop
open CommonData
open CommonLex
open DP
open DPExecution
open DPTests
open Tests

open VisualTest.VCommon
open VisualTest.VLog
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

let visCompareREPL() =
    let rec repl'() =
        printf  "~> "
        let srcIn =
            System.Console.ReadLine().ToUpper()
        srcIn
        |> fun s -> visCompare s [] false false false false
        |> function
        | true ->
            "\n**************************"  |> (printfn "%s")
            srcIn                           |> qp
            "AGREES with VisUAL"            |> (printfn "%s")
            "**************************\n"  |> (printfn "%s")
        | false ->
            "\n**************************"  |> (printfn "%s")
            srcIn                           |> qp
            "DOES NOT AGREE with VisUAL"    |> (printfn "%s")
            "**************************"    |> (printfn "%s")
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


/// configuration for this testing framework      
/// configuration for expecto. Note that by default tests will be run in parallel
/// this is set by the fields oif testParas above



[<Tests>]


let expectoConfig = { Expecto.Tests.defaultConfig with 
                        parallel = testParas.Parallel
                        parallelWorkers = 6 // try increasing this if CPU use is less than 100%
                }

[<EntryPoint>]
let main argv =

    // "ready to REPL..." |> (printfn "%s")
    // parseREPL |> ignore

    // "ready to REPL..." |> (printfn "%s")
    // visCompareREPL()

    // "ready to REPL..." |> qp
    // let dp = initialiseDP false false false false [0u]
    // exeREPL dp |> ignore

    initCaches testParas
    let rc = runTestsInAssembly expectoConfig [||]
    finaliseCaches testParas
    System.Console.ReadKey() |> ignore                
    rc // return an integer exit code - 0 if all tests pass
    0
  


