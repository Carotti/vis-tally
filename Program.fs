module Program
open CommonTop
open CommonData
open Helpers
open Execution
open ExecutionTop

/// A List of instructions to parse and then execute.
let parseInstr input =
    parseLine None (WA 0u) (uppercase input)

/// Prettyprinter for printing cpu regs, flags and memory
let prettyPrint dp =
    match dp with
    | Ok cpuData ->
        cpuData.Regs |> Map.toList |> List.map (fun (r, v) -> printfn "(%A : 0x%x)" r v) |> ignore
        cpuData.Fl |> qp |> ignore
        cpuData.MM |> Map.toList |> qpl |> ignore
    | Error e -> e |> qp
    


let replParse() =
    let rec repl'() =
        printf  "=> "
        System.Console.ReadLine().ToUpper()
        |> parseInstr
        |> qp
        repl'()
    repl'()

/// a REPL for parsing and executing instructions
/// have fun :)
let replExecute cpuData =
    prettyPrint cpuData
    let rec repl' cpuData' =
        match cpuData' with
        | Ok cpuData' ->
            printf  "=> "
            System.Console.ReadLine().ToUpper()
            |> parseInstr
            |> function
            | Ok instr ->
                execute instr cpuData'
                |> function
                | cpuData'' ->
                  prettyPrint cpuData''
                  repl' cpuData''
            | Error err -> 
                err |> qp
                repl' cpuData
        | Error e -> e |> qp
        
    repl' cpuData
    
[<EntryPoint>]
let main argv =
    match argv with
        | [|"xrepl"|] ->
            "Doug's Remarkable REPL..." |> qp
            replExecute (Ok initDataPath)
            0

        | [|"prepl"|] ->
            "Doug's Remarkable REPL..." |> qp
            replParse()
            
        | _ -> 
            "blah" |> qp
            |> ignore
            0