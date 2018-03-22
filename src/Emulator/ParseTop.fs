module ParseTop

open CommonLex
open CommonTop
open CommonData

open ExecutionTop
open Execution

open Errors

open Misc
open Branch
open System.Linq.Expressions

// Convert list of Results to a Result of lists
// An Error list of errors if any errors occured,
// An Ok list of instructions if no errors occured
let listResToResList state (nxt, lineNo) =
    match nxt with
    | Ok ins -> 
        match state with
        | Ok lst -> (ins, lineNo) :: lst |> Ok
        | x -> x
    | Error err -> 
        match state with
        | Error lst -> (err, lineNo) :: lst |> Error
        | _ -> [err, lineNo] |> Error
        
// let rec resolveSymbols lst syms =
//     let resolver syms' (ins, lineNo) = 
//         match ins.PInstr with
//         | IMISC ins ->
//             let tryResolve = resolve syms' ins
//             match tryResolve with
//             | Ok misc ->

//             | Error e -> // Couldn't resolve the symbol
//         | _ ->

//     let newSyms = List.fold resolver syms lst
//     newSyms

// All the parsedInfo
type parsedInfo = {
        dp : DataPath<Parse<CommonTop.Instr>>
        lineNo : Map<uint32, uint32>
        syms : Map<string, uint32>
        pc : uint32
    }

let nearestHexHundred x = (x / 0x100u + 1u) * 0x100u

// Adds symbol x to the symbol table in pInfo if there is a label
let addSymbol x pInfo =
    match x with
        | Some (label, _) -> 
            match Map.containsKey label pInfo.syms with
            | true -> 
                (label, "is already used")
                ||> makeError
                |>  ``Symbol already defined``
                |> Error
            | false -> Map.add label pInfo.pc pInfo.syms |> Ok
        | None -> pInfo.syms |> Ok

let pInfoPlaceIns pInfo ins ln insSize addLineNo =
    let newSyms = addSymbol ins.PLabel pInfo
    Result.map (fun x ->
        {
            dp = (setMemInstr ins pInfo.pc pInfo.dp)
            lineNo =
                match addLineNo with
                | true -> Map.add pInfo.pc ln pInfo.lineNo
                | false -> pInfo.lineNo
            syms = x
            pc = pInfo.pc + insSize
    }) newSyms

// Place regular instructions in memory locations
let placeInstructions lst parseInfo = 
    // Attempt to place ins from line number ln in the datapath in pInfo
    let insPlacer pInfo (ins, ln) =
        let resPInfo pi =
            match ins.PInstr with
            | IMISC ins' -> 
                match ins' with
                | Misc (ADR _) -> pInfoPlaceIns pi ins ln 4u true
                | _ -> pInfo
            | EMPTY -> Result.map (fun x -> {pi with syms = x}) (addSymbol ins.PLabel pi)
            | _ -> pInfoPlaceIns pi ins ln 4u true
        Result.bind resPInfo pInfo

    List.fold insPlacer (Ok parseInfo) lst

// Place directive instructions in memory
let placeDirectives lst parseInfo = 
    let dirPlacer pInfo (ins, ln) =
        let resPInfo pi =
            match ins.PInstr with
            | IMISC ins' ->
                match ins' with
                | Misc (ADR _) -> pInfo
                | _ -> pInfoPlaceIns pi ins ln ins.PSize false
            | _ -> pInfo
        Result.bind resPInfo pInfo

    List.fold dirPlacer (Ok parseInfo) lst

// Given a list of correctly parsed instructions, return
// a the datapath with the instructions placed in memory,
// a symbol table with corresponding symbols
// and a mapping from program counters to line numbers
let getInfoFromParsed (lst : (Parse<CommonTop.Instr> * uint32) list) =
    let initialParsedInfo = {dp = initDataPath ; lineNo = Map.ofList [] ; syms = Map.ofList []; pc = 0u}

    placeInstructions lst initialParsedInfo
    |> Result.map (fun x -> {x with pc = nearestHexHundred x.pc})
    |> Result.map (placeDirectives lst)