module Program

open Expecto
open FsCheck

open CommonTop
open CommonData
open CommonLex
open DP
open DPExecution


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

let covertToDP (ins : Parse<CommonTop.Instr>) : Parse<DP.Instr> =
    match ins.PInstr with
    | IDP dpIns -> 
        {
            PInstr = dpIns
            PLabel = ins.PLabel
            PSize = ins.PSize
            PCond = ins.PCond
        }
    | _ -> failwithf "Invalid downcast to DP"

let parseREPL() =
    let rec repl'() =
        printf  "~> "
        System.Console.ReadLine().ToUpper()
        |> parseLine None (WA 0u) 
        |> qp
        repl'()
    repl'()

// (dp:DataPath<Instr>)
let exeREPL (dp:DataPath<Instr>) =
    let printRegs (dp:DataPath<Instr>) =
        dp.Regs |> Map.toList |> List.map (fun (r, v) -> printfn "%A : %x" r v) |> ignore
    let printFlags (dp:DataPath<Instr>) =
         dp.Fl |> qp |> ignore
    
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
let expectoConfig = { Expecto.Tests.defaultConfig with 
                        parallel = testParas.Parallel
                        parallelWorkers = 6 // try increasing this if CPU use is less than 100%
                }

[<EntryPoint>]
let main argv =
    initCaches testParas
    let rc = runTestsInAssembly expectoConfig [||]
    finaliseCaches testParas
    System.Console.ReadKey() |> ignore                
    rc // return an integer exit code - 0 if all tests pass



    "ready to REPL..." |> (printfn "%s")
    parseREPL |> ignore

    "ready to REPL..." |> qp
    let dp = initialiseDP false false false false [0u]
    exeREPL dp |> ignore

    0


