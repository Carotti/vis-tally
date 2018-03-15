module Program
open CommonTop
open CommonData
open Helpers
open Execution
open ExecutionTop
open Symbols

/// A List of instructions to parse and then execute.
let instrLst = [
        "hello MOV r0, #1";
        "goodbye MOV r1, #2";
        " \t  ";
        "lsl r4, r1, #6";
        "aufwiedersehn MOV r2, #3";
        "\n   ";
        "tchus MOV r3, #4";
        "aurevoir MOV r4, #0x100";
        "   ";
    ]

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
                repl' (cpuData' |> Ok) 
        | Error e -> e |> qp
    repl' cpuData

/// Parses and executes items in a given list
let listExecute cpuData (lst: string list) = 
    prettyPrint cpuData
    let rec listExecute' cpuData' lst' = 
        match lst' with
        | head :: tail -> 
            match head with
            | Ok instr ->
                match cpuData' with
                | Ok cpuData' ->
                    execute instr cpuData'
                    |> function    
                    | cpuData'' ->
                          prettyPrint cpuData''
                          listExecute' cpuData'' tail
                | Error e -> e |> qp
            | Error err ->
                err |> qp
                listExecute' cpuData' tail
        | [] -> "Finished" |> qp
    let parsedList = List.map parseInstr lst
    let symTable = fillSymTable parsedList symMap
    listExecute' cpuData parsedList
    symTable |> qp

    
    
[<EntryPoint>]
let main argv =
    match argv with
        | [|"xrepl"|] ->
            "Doug's Remarkable REPL..." |> qp
            replExecute (initDataPath |> Ok) 
            0

        | [|"prepl"|] ->
            "Doug's Remarkable REPL..." |> qp
            replParse()

        | [|"xlist"|] ->
            "Executing list..." |> qp
            listExecute (initDataPath |> Ok) instrLst
            0
         | [|"plist"|] ->
            "Parsing list..." |> qp
            List.map parseInstr instrLst |> qpl |> ignore
            0

        | _ -> 
            "blah" |> qp
            |> ignore
            0