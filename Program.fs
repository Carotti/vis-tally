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
    /// test the initProjectLexer code
    let instrLst = [
        // "LSL R0, R1, #2";
        // "LSL r0, r1, #0b101";
        // "LSL r0, r1, #0xe";
        // "LSL r0, r1, #&f";
        // "LSL R0, R1, R2"; 
        // "RRX R0, R1";
        "LDR r0, [r1, r2]";
    ]

    List.map (parseLine None (WA 0u)) instrLst
    |> qpl
    |> ignore
    0 // return an integer exit code
    // This is a test
