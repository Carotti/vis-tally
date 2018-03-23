module Integration

open Helpers
open CommonData
open CommonLex
open CommonTop
open ExecutionTop

open Errors
open FsCheck

let parseInstr input =
    parseLine None (WA 0u) (input)

let instrToParse err : ErrParse = 
    match err with
    | ERRIMEM x -> x
    | ERRIDP x -> x
    | ERRMISC x -> x
    | ERRBRANCH x -> x
    | ERRTOPLEVEL x -> x

let errUnpacker (_errName, err : ErrorBase) lineNo =
    printf "%s%s at line %d" err.errorTxt err.errorMessage lineNo

let highlightErrorParse (err, lineNo) = 
    let getErrNames = 
        match err with
        | ``Invalid literal`` x -> "Invalid literal", x
        | ``Invalid second operand`` x -> "Invalid second operand", x
        | ``Invalid flexible second operand`` x -> "Invalid flexible second operand", x
        | ``Invalid memory address`` x  -> "Invalid memory address", x
        | ``Invalid offset`` x  -> "Invalid offset", x
        | ``Invalid register`` x  -> "Invalid register", x
        | ``Invalid shift`` x  -> "Invalid shift", x
        | ``Invalid suffix`` x  -> "Invalid suffix", x
        | ``Invalid instruction`` x  -> "Invalid instruction", x
        | ``Invalid expression`` x  -> "Invalid expression", x
        | ``Invalid expression list`` x  -> "Invalid expression list", x
        | ``Invalid fill`` x  -> "Invalid fill", x
        | ``Label required`` x  -> "Label required", x
        | ``Unimplemented instruction`` x -> "Invalid instruction", x
    errUnpacker getErrNames lineNo

let highlightErrorResolve e =
    List.map (fun x -> errUnpacker ("resolve", x.error) x.lineNumber) e |> ignore

    
let handleRunTimeError e pInfo =
    match e with
    | EXIT -> 
        Some pInfo
    | NotInstrMem x -> 
        printf "Trying to access non-instruction memory with program counter"
        None
    | ``Run time error`` {errorTxt = txt ; errorMessage = msg} ->
        printf "Trying to access non-instruction memory with program counter"
        None

let rec pExecute pInfo =
    let newDp = dataPathStep pInfo.dp
    match newDp with
    | Result.Error e -> handleRunTimeError e pInfo
    | Result.Ok ndp ->
        pExecute {pInfo with dp = ndp}

let runCode code =
    let stringList = code |> (fun (x : string) -> x.Split [|'\n'|]) |> Array.toList

    // Parse each line of the file first
    let parsedList = List.map parseInstr stringList

    // Convert list of Results to a Result of lists
    // Also tuple with corresponding line numbers
    let parsedLst = 
        parsedList
        |> List.map (Result.mapError instrToParse)
        |> lineNumList
        |> List.fold listResToResList (Result.Ok [])
        |> Result.map List.rev
        |> Result.mapError List.rev
        |> Result.map (fun x -> x @ [(endInstruction, 0u)])

    // See if any errors exist, if they do display them
    match parsedLst with
    | Result.Ok insLst -> 
        match getInfoFromParsed insLst with
        | Result.Ok x -> 
            match pExecute x with
            | Some pInfo -> Some pInfo.dp
            | None -> None
        | Result.Error x -> 
            highlightErrorResolve x |> ignore
            None
    | Result.Error errLst -> 
        List.map highlightErrorParse errLst |> ignore
        None