﻿module Program
    open CommonTop
    open CommonLex
    open CommonData
    open Helpers
    open Execution
    open ExecutionTop
    open Symbols

    /// A List of instructions to parse and then execute.
    let instrLst = [
            "fish DCD 123";
            "hello MOV r0, #1";
            "goodbye MOV r1, #2";
            " \t  ";
            "lsl r4, r1, #6";
            "lsr r3, r4, r2";
            "aufwiedersehn MOV r2, #3";
            "\n   ";
            "tchus MOV r3, #4";
            "add r1, r2, r3, lsl r2";
            "aurevoir MOV r4, #0x100";
            "   ";
            "B tchus";
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
    let replExecute (cpuData : Result<DataPath<Instr>, Errors.ErrExe>) : unit =
        prettyPrint cpuData
        let rec repl' (cpuData' : Result<DataPath<Instr>, Errors.ErrExe>) (symTable : SymbolTable) =
            match cpuData' with
            | Ok cpuData' ->
                printf  "=> "
                System.Console.ReadLine().ToUpper()
                |> parseInstr
                |> function
                | Ok instr ->
                    symTable |> qp |> ignore
                    execute instr cpuData' symTable
                    |> function
                    | cpuData'' ->
                      prettyPrint cpuData''
                      repl' cpuData'' symTable
                | Error err -> 
                    err |> qp
                    repl' (cpuData' |> Ok) symTable
            | Error e -> e |> qp
        repl' cpuData symMap

    /// Parses and executes items in a given list
    let listExecute (cpuData : Result<DataPath<Instr>, Errors.ErrExe>) (lst: string list) = 
        prettyPrint cpuData
        let rec listExecute' cpuData' (symTable : SymbolTable) lst' = 
            match lst' with
            | head :: tail -> 
                match head with
                | Ok instr ->
                    match cpuData' with
                    | Ok cpuData' ->
                        execute instr cpuData' symTable
                        |> function    
                        | cpuData'' ->
                              prettyPrint cpuData''
                              listExecute' cpuData'' symTable tail
                    | Error e -> e |> qp
                | Error err ->
                    err |> qp
                    listExecute' cpuData' symTable tail
            | [] -> "Finished" |> qp

        let parsedList = List.map parseInstr lst
        let errorList = 
            parsedList
            |> generateErrorList
        errorList
        |> List.length
        |> function
        | 0 ->
            "NO ERRORS - EXECUTE!" |> qp |> ignore

            // no errors, can execute
            let symTable' = fillSymTable parsedList symMap
            listExecute' cpuData  symTable' parsedList
            symTable' |> qp
            // dummy return
        | n ->
            // n errors, send to Nippy's shit code to highight
            // dummy return
            errorList |> qpl |> ignore
            "HIGHLIGHT IT SHIPPY" |> qp
            
        
        
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