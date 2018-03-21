﻿module Program
    open CommonTop
    open CommonLex
    open CommonData
    open Helpers
    open Execution
    open ExecutionTop
    open Symbols
    open Errors

    /// A List of instructions to parse and then execute.
    let instrLst = [
            "dcd1 DCD 123"; // 1, n/a
            "adr R0, dcd1";
            "END";
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
            // cpuData.MM |> Map.toList |> qpl |> ignore
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
    let replExecute (cpuData : Result<DataPath<Parse<CommonTop.Instr>>, Errors.ErrExe>) : unit =
        prettyPrint cpuData
        let rec repl' (cpuData' : Result<DataPath<Parse<CommonTop.Instr>>, Errors.ErrExe>) (symTable : SymbolTable) =
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
        repl' cpuData emptySymMap
   
    let fetchInstr (cpuData: DataPath<Parse<CommonTop.Instr>>) addr : Result<Parse<CommonTop.Instr>, ErrExe> =
        match validateWA addr with
        | true -> 
            let instrAddr = WA addr
            match locExists instrAddr cpuData with
            | true -> 
                match getMemLoc instrAddr cpuData with
                | Code c -> c |> Ok
                | DataLoc _ -> failwith "Fetching data from a supposed instruction memory address."
            | false -> 
                (addr |> string, " does not contain an instruction.")
                ||> makeError
                |> ``Run time error``
                |> Error
        | false ->
            (addr |> string, "Instructions were not stored at aligned addresses!")
            ||> makeError
            |> ``Run time error``
            |> Error

    let exeFromMem (cpuData: Result<DataPath<Parse<CommonTop.Instr>>, ErrExe>) (lst: string list) =
        let rec executer (cpuData: Result<DataPath<Parse<CommonTop.Instr>>, ErrExe>) pcToLine (symTable: SymbolTable) =
            match cpuData with
            | Ok cpuData' ->
                let pc = getPC cpuData'
                match Map.containsKey pc pcToLine with
                | true ->
                    let instr : Result<Parse<CommonTop.Instr>,ErrExe> =
                        cpuData'
                        |> getPC
                        |> fetchInstr cpuData'
                    match instr with
                    | Ok instr' ->
                        execute instr' cpuData' symTable
                        |> function
                        | cpuData'' ->
                            prettyPrint cpuData''
                            executer cpuData'' pcToLine symTable
                        // | Error e ->
                        //     e |> qp
                        //     failwith "The DataPath has an error after execution."
                    | Error e ->
                        e |> qp
                        failwith "Instruction has an error."   
                | false ->
                    prettyPrint (cpuData' |> Ok)
            | Error EXIT ->
                prettyPrint cpuData      
            | Error e ->
                e |> qp
                failwith "The DataPath has an error."                    
        
        let parsedList = List.map parseInstr lst
        let errorList = 
            parsedList
            |> lineNumList
            |> generateErrorList
        errorList
        |> List.length
        |> function
        | 0 ->
            "NO PARSE ERRORS - EXECUTE!" |> qp |> ignore
            match cpuData with
            | Ok cpuData' ->
                let symTable', cpuData'' = fillSymTable parsedList emptySymMap cpuData'
                let pcLineMap = 
                    parsedList
                    |> makePcToLineNum
                symTable' |> qp
                prettyPrint (cpuData'' |> Ok) 
                executer (cpuData'' |> Ok) pcLineMap symTable'
                
            | Error e ->
                e |> qp
        | n ->
            // n errors, send to Nippy's shit code to highight
            // dummy return
            errorList |> qpl |> ignore
            "ERROR: HIGHLIGHT IT SHIPPY" |> qp

    /// Parses and executes items in a given list
    let listExecute (cpuData : Result<DataPath<Parse<CommonTop.Instr>>, Errors.ErrExe>) (lst: string list) = 
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
            |> lineNumList
            |> generateErrorList
        errorList
        |> List.length
        |> function
        | 0 ->
            "NO PARSE ERRORS - EXECUTE!" |> qp |> ignore
            match cpuData with
            | Ok cpuData' ->
                let symTable', cpuData'' = fillSymTable parsedList emptySymMap cpuData'
                // CHRIS WILL KILL 10 PUPPIES IF WE DO NOT CHANGE THIS
                listExecute' (cpuData'' |> Ok)  symTable' parsedList
                // prettyPrint (Ok cpuData'')
                parsedList
                |> makePcToLineNum
                |> Map.toList
                |> qpl 
                |> ignore 

                symTable' |> qp
            | Error e ->
                e |> qp
        | n ->
            // n errors, send to Nippy's shit code to highight
            // dummy return
            errorList |> qpl |> ignore
            "ERROR: HIGHLIGHT IT SHIPPY" |> qp
            
        
         
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

            | [|"xmem"|] ->
                "Executing from memory..." |> qp
                exeFromMem (initDataPath |> Ok) instrLst
                0

            | _ -> 
                "blah" |> qp
                |> ignore
                0