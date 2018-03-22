module Integration

open Tabs
open Update
open Helpers
open CommonData
open CommonLex
open CommonTop
open ExecutionTop

open Errors
open Ref

open Fable.Core.JsInterop
open Fable.Import
open Fable.Import.Browser
open Fable.Core

open Fable.Import.Electron
open Node.Exports
open System.IO

let parseInstr input =
    parseLine fNone (WA 0u) (input)

let instrToParse err = 
    match err with
    | ERRIMEM x -> x
    | ERRIDP x -> x
    | ERRMISC x -> x
    | ERRBRANCH x -> x
    | ERRTOPLEVEL x -> x

let errUnpacker (_errName, err : ErrorBase) tId lineNo =
    makeErrorInEditor tId lineNo (err.errorTxt + err.errorMessage) 

let highlightErrorParse (err, lineNo) tId = 
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
    errUnpacker getErrNames tId lineNo

let highlightErrorResolve e tId =
    List.map (fun x -> errUnpacker ("resolve", x.error) tId x.lineNumber) e

let makeMemoryMap mm =
    Map.toList mm
    |> List.map (fun (wAddr, value) ->
        let addr = match wAddr with
                    | WA x -> x 
        match value with
        | DataLoc x -> Some (addr, x)
        | _ -> fNone
    )
    |> List.choose id
    |> Map.ofList

let setRegs regs =
    Map.map (fun r value ->
        setRegister regNums.[r] value
    ) regs |> ignore
    
let setFlags flags =
    setFlag "N" flags.N
    setFlag "C" flags.C
    setFlag "Z" flags.Z
    setFlag "V" flags.V

let showInfo pInfo =
    symbolMap <- pInfo.syms
    updateSymTable()
    memoryMap <- makeMemoryMap pInfo.dp.MM
    updateMemory()
    setRegs pInfo.dp.Regs
    setFlags pInfo.dp.Fl

let handleRunTimeError e pInfo =
    match e with
    | EXIT -> showInfo pInfo
    | NotInstrMem x -> 
        Browser.window.alert(sprintf "Trying to access non-instruction memory 0x%x" x)
    | ``Run time error`` {errorTxt = txt ; errorMessage = msg} ->
        Browser.window.alert(txt + " " + msg)
            
let rec pExecute pInfo =
    let newDp = dataPathStep pInfo.dp
    match newDp with
    | Result.Error e -> handleRunTimeError e pInfo
    | Result.Ok ndp ->
        pExecute {pInfo with dp = ndp}

let tryParseCode tId =
    let stringList = getCode tId |> (fun (x : string) -> x.Split [|'\n'|]) |> Array.toList

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
        Browser.console.log(sprintf "%A" (getInfoFromParsed insLst))
        match getInfoFromParsed insLst with
        | Result.Ok x -> Some x
        | Result.Error x -> 
            highlightErrorResolve x tId |> ignore
            fNone
    | Result.Error errLst -> 
        List.map (fun x -> highlightErrorParse x tId) errLst |> ignore
        fNone
let runCode () =
    let tId = currentFileTabId
    removeEditorDecorations tId
    match tryParseCode tId with
    | Some p -> 
        disableEditors()
        pExecute p
    | _ -> ()

let mutable currentPInfo : ParsedInfo option = fNone

let rec stepCode () =
    let tId = currentFileTabId
    match currentPInfo with
    | Some pInfo ->
        let newDp = dataPathStep pInfo.dp
        match newDp with
        | Result.Error e ->
            handleRunTimeError e pInfo
        | Result.Ok ndp ->
            let newP = {pInfo with dp = ndp}
            currentPInfo <- Some newP
            showInfo newP
    | _ ->
        currentPInfo <- tryParseCode tId
        match currentPInfo with
        | Some _ -> 
            disableEditors()
            stepCode ()
        | _ -> ()

let resetEmulator () =
    enableEditors()   
    memoryMap <- initialMemoryMap
    symbolMap <- initialSymbolMap
    updateMemory()
    updateSymTable()
    resetRegs()
    resetFlags()
    currentPInfo <- fNone