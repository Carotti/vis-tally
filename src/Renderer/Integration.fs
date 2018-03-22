module Integration

open Tabs
open Update
open Helpers
open CommonData
open CommonLex
open CommonTop
open ExecutionTop
open ParseTop

open Errors

open Fable.Core.JsInterop
open Fable.Import
open Fable.Import.Browser
open Fable.Core

open Fable.Import.Electron
open Node.Exports
open System.IO

let fNone = Microsoft.FSharp.Core.option.None

let parseInstr input =
    parseLine fNone (WA 0u) (uppercase input)

let instrToParse err = 
    match err with
    | ERRIMEM x -> x
    | ERRIDP x -> x
    | ERRMISC x -> x
    | ERRBRANCH x -> x
    | ERRTOPLEVEL x -> x

let highlightError (err, lineNo) tId = 
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
    let errUnpacker (_errName, err : ErrorBase) =
        makeErrorInEditor tId lineNo (err.errorTxt + err.errorMessage) 
    errUnpacker getErrNames
let tryParseCode tId =
    match isTabUnsaved tId with
    | true -> Browser.window.alert("File is unsaved, please save and try again") 
    | false ->
        // Remove the old editor decorations first
        removeEditorDecorations tId
        let onceFileRead (fileData : Node.Buffer.Buffer) =
            let stringList = fileData.toString("utf8") |> (fun (x : string) -> x.Split [|'\n'|]) |> Array.toList

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

            // See if any errors exist, if they do display them
            match parsedLst with
            | Result.Ok insLst -> 
                Browser.console.log(sprintf "%A" (getInfoFromParsed insLst))
            | Result.Error errLst -> 
                List.map (fun x -> highlightError x tId) errLst |> ignore

            Browser.console.log("Working")

        fs.readFile(getTabFilePath tId, (fun err data -> // TODO: find out what this error does
            onceFileRead data
        ))

