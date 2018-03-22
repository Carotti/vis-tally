module ExecutionTop
open Errors
open CommonData
open DP
open Memory
open Branch
open Misc
open CommonTop
open CommonLex
open Execution
open DPExecution
open MemExecution
open BranchExecution
open MiscExecution
open Helpers
open Branch
open ErrorMessages
open Expressions
open MiscExecution

type ErrResolveBase = {lineNumber : uint32 ; error : ErrorBase}

let setMemInstr contents (addr: uint32) (cpuData: DataPath<Parse<CommonTop.Instr>>) = 
// let updateMem contents addr (cpuData: DataPath<CommonTop.Instr>) =
    match addr % 4u with
    | 0u -> {cpuData with MM = Map.add (WA addr) (Code contents) cpuData.MM}
    | _ -> failwithf "Not aligned, but should have been checked already."
    // match contents.PInstr with
    // | CommonTop.IDP (DPTop instr') ->
    //     updateMem instr' addr cpuData
    // | CommonTop.IMEM (Mem instr') ->
    //     updateMem instr' addr cpuData
    // | CommonTop.IBRANCH (Branch instr') ->
    //     updateMem instr' addr cpuData
    // | _ -> failwithf "Shouldnt be in mem."
    
    
let miscResolve instr symTable =
    let resolved = Misc.resolve symTable instr
    match resolved with
    | Ok resInstr -> resInstr
    | Error _ -> failwithf "Invalid Symbol"

let branchResolve instr symTable =
    let resolved = Branch.resolvePInstr symTable instr
    match resolved with
    | Ok resInstr -> resInstr
    | Error _ -> failwithf "Invalid Symbol"

let lineNumList instrLst =
    [1u..(List.length instrLst) |> uint32]
    |> List.zip instrLst

let generateErrorList lineNumLst =
    lineNumLst
    |> List.filter (function | Error _, _ -> true | Ok _, _ -> false)
    |> List.map (function | Error e, l -> e,l | _ -> failwith alwaysMatchesFM)

let makePcToLineNum (instrLst: Result<CommonLex.Parse<CommonTop.Instr>, ErrParse> list) =
    let getPcInc (instr: Result<CommonLex.Parse<CommonTop.Instr>, ErrParse>) =
        match instr with
        | Ok instr' ->
            match isMisc instr' with
            | true ->
                0u
            | false ->
                instr'.PSize
        | Error _ -> failwith noErrorsFM

    let pairLinePC ((lineMap:Map<uint32,uint32>), pc) (instr, lineNum) =
        let lineMap' = lineMap.Add(pc, lineNum)
        let pc' = pc + (getPcInc instr)
        (lineMap', pc')
    
    instrLst
    |> lineNumList
    |> List.fold (pairLinePC) (Map.empty, 0u)
    |> fst

let fillSymTable (instrLst: Result<CommonLex.Parse<CommonTop.Instr>,ErrParse> list) (symTable: SymbolTable) (cpuData : DataPath<Parse<CommonTop.Instr>>) =
    let rec fillSymTable' (instrLst': Result<CommonLex.Parse<CommonTop.Instr>,ErrParse> list) (symTable': SymbolTable) (cpuData' : DataPath<Parse<CommonTop.Instr>>) instrAddr dataAddr  =
        match instrLst' with
        | head :: tail ->
            match head with
            | Ok instr' ->
                match instr'.PInstr with
                | CommonTop.IMISC (Misc instr'') ->
                    let symTableNew = 
                        match instr'.PLabel with
                        | Some s -> symTable'.Add((s |> fst), (dataAddr))
                        | None -> symTable'
                        
                    let resInstr = miscResolve instr'' symTableNew
                    
                    executeMisc resInstr dataAddr cpuData'
                    |> function
                    | Ok (cpuData''', nextAddr) -> 
                        "value of nextAddr" + string(nextAddr) |> qp
                        fillSymTable' tail symTableNew cpuData''' instrAddr nextAddr      
                    | Error _ -> failwithf "Woaaaaaaaaaaaaaah we need to sort this"

                | _ ->
                    let cpuData'' = setMemInstr (instr') instrAddr cpuData'
                    match instr'.PLabel with
                    | Some label ->
                        let symTableNew = symTable'.Add((label |> fst), (instrAddr))
                        "strange number we don't know = " + (label |> snd |> string) |> qp
                        fillSymTable' tail symTableNew cpuData'' (instrAddr + instr'.PSize) dataAddr
                    | None ->
                        fillSymTable' tail symTable' cpuData'' (instrAddr + instr'.PSize) dataAddr
            | Error _ -> symTable', cpuData'
        | [] -> symTable', cpuData'
    fillSymTable' instrLst symTable cpuData 0u minAddress

/// The Top level execute instruction taking any Parse<Instr>
/// and downcasting it to the revelvant memory or data processing
/// instructions, then calling their executes.
let execute (instr: Parse<CommonTop.Instr>) (cpuData: DataPath<Parse<CommonTop.Instr>>) (symTable: SymbolTable) =
    match condExecute instr cpuData with
        | true -> 
            match instr.PInstr with
            | CommonTop.IDP (DPTop instr') ->
                executeDP instr' cpuData
            | CommonTop.IMEM (Mem instr') ->
                executeMem instr' cpuData
            | CommonTop.IBRANCH (Branch instr') ->
                let resInstr = branchResolve instr' symTable
                executeBranch resInstr cpuData
            | CommonTop.IMISC (Misc instr') ->
                    cpuData |> Ok
            //     let resInstr = miscResolve instr' symTable
            //     executeMisc resInstr minAddress cpuData
            | CommonTop.EMPTY _ ->
                cpuData |> Ok
        | false -> 
            updatePC instr cpuData |> Ok
    |> Result.map (updatePC instr)
    
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
        
// All the parsedInfo
type ParsedInfo = {
        dp : DataPath<Parse<CommonTop.Instr>>
        lineNo : Map<uint32, uint32>
        syms : Map<string, uint32>
        pc : uint32
    }

let nearestHexHundred x = (x / 0x100u + 1u) * 0x100u

// Adds symbol x to the symbol table in pInfo if there is a label
let addSymbol x pInfo ln =
    match x with
        | Some (label, _) -> 
            match Map.containsKey label pInfo.syms with
            | true -> 
                let err = (label, " is already used")
                            ||> makeError
                [{lineNumber = ln ; error = err}] 
                |> Error
            | false -> Map.add label pInfo.pc pInfo.syms |> Ok
        | None -> pInfo.syms |> Ok

let pInfoPlaceIns pInfo ins ln insSize isIns =
    let newSyms = addSymbol ins.PLabel pInfo ln
    Result.map (fun x ->
        {
            dp = 
                match isIns with
                | true -> (setMemInstr ins pInfo.pc pInfo.dp)
                | false -> pInfo.dp
            lineNo =
                match isIns with
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
            | EMPTY -> Result.map (fun x -> {pi with syms = x}) (addSymbol ins.PLabel pi ln)
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
                | Misc (ADR _)
                | Misc (EQU _) -> Ok pi
                | _ -> pInfoPlaceIns pi ins ln ins.PSize false
            | _ -> pInfo
        Result.bind resPInfo pInfo

    List.fold dirPlacer (Ok parseInfo) lst

let evalErrListToString x =
    let unpackSymUndeclared a = 
        match a with
        | SymbolUndeclared a' -> a'
    List.map unpackSymUndeclared x
    |> List.reduce (fun a b -> a + ", " + b)

let makeSymError (res, ln) =
    match res with
    | Error x -> evalErrListToString x
    | _ -> ""
    |> (fun x -> (x, ln))

let rec resolveEqus equs syms = 
    let resolveEqu x (label, exp, ln)=
        match x with
        | Error x -> Error x
        | Ok (oldSyms, unresolved) ->
            match evalSymExp oldSyms exp with
            | Ok x ->
                match x with
                | ExpResolved value -> 
                    match Map.containsKey label oldSyms with
                    | true -> 
                        let err = (label, " is already used")
                                    ||> makeError
                        [{lineNumber = ln ; error = err}] 
                        |> Error
                    | false -> (Map.add label value oldSyms, unresolved) |> Ok
                | _ -> failwithf "Resolution should have been successful"
            | Error _ -> (oldSyms, (label, exp, ln) :: unresolved) |> Ok

    let x = List.fold resolveEqu (Ok (syms, [])) equs

    Result.bind (fun (newSyms, unresolved) ->
        match newSyms = syms with
        | true -> // No new symbols were found, so we're done
            match List.length unresolved with
            | 0 -> Ok newSyms
            | _ -> 
                let makeSingleError (s, ln : uint32) = 
                    let e = (s, " not defined") ||> makeError
                    {
                        lineNumber = ln
                        error = e
                    }
                List.map (((fun (_, b, c) -> (evalSymExp newSyms b, c)) >> makeSymError) >> makeSingleError) unresolved
                |> Error

        | false -> // Keep trying to expand symbol table
            resolveEqus unresolved newSyms
    ) x


let getLabel ins = 
    match ins.PLabel with
    | Some (l, _) -> l
    | _ -> failwithf "Instruction should have label by now" 

let resolveSymbols lst parseInfo =
    // Only EQU at this point can create new symbols
    let getEqu (ins, ln) =
        match ins.PInstr with
        | IMISC ins' ->
            match ins' with
            | Misc (EQU x) ->
                let label = getLabel ins
                Some (label, x, ln)
            | _ -> None
        | _ -> None

    let equs =
        List.map getEqu lst
        |> List.choose id
    let resolvedEquSyms = resolveEqus equs parseInfo.syms
    Result.map (fun x -> {parseInfo with syms = x}) resolvedEquSyms

let ErrMiscResolveToErrResolve x ln =
    match x with
    | InvalidByteExp x -> (sprintf "%d" x, " cannot fit inside a byte") ||> makeError
    | SymbolErrors lst -> (evalErrListToString lst, " not declared") ||> makeError
    | InvalidFillMultiple -> ("", "Fill size must be a multiple of the value size.") ||> makeError
    |> (fun x -> {error = x ; lineNumber = ln})
    
let resolveDirectives lst parseInfo = 
    let resolveDirective pInfo (ins, ln) =
        Result.bind (fun pi ->
            match ins.PInstr with
            | IMISC (Misc miscIns) ->
                match miscIns with
                | ADR _ -> pi |> Ok
                | EQU _ -> pi |> Ok
                | _ ->
                    match Misc.resolve pi.syms miscIns with
                    | Ok resolved -> 
                        match executeMisc resolved pi.syms.[getLabel ins] pi.dp with
                        | Ok x -> {pi with dp = x |> fst} |> Ok
                        | Error _ -> failwithf "Shouldn't error executing misc"
                    | Error err -> 
                        ErrMiscResolveToErrResolve err ln 
                        |> (fun x -> [x])
                        |> Error
            | _ -> pi |> Ok
        ) pInfo

    List.fold (resolveDirective) (Ok parseInfo) lst

// Go through instructions already in the datapath and 
// resolve the ADR and branch instructions against the 
// now fully built symbol table
let resolveAdrBranchs parseInfo =
    let memMapper wAddr data =
        let addr = 
            match wAddr with
            | WA x -> x
        match data with
        | DataLoc x -> DataLoc x |> Ok
        | Code ins ->
            match ins.PInstr with
            | IMISC (Misc ins') ->
                match ins' with
                | ADR _ -> 
                    let codify x = Code {ins with PInstr = x |> Misc |> IMISC} 
                    match Misc.resolve parseInfo.syms ins' with
                    | Ok x -> codify x |> Ok
                    | Error x -> ErrMiscResolveToErrResolve x parseInfo.lineNo.[addr] |> Error
                | _ -> failwithf "Other MISC instructions shouldn't be in memory"
            | IBRANCH (Branch ins') -> 
                let codify x = Code {ins with PInstr = x |> Branch |> IBRANCH}
                match Branch.resolve parseInfo.syms ins' with
                | Ok x -> codify x |> Ok
                | Error x -> 
                    (evalErrListToString x, " not declared") ||> makeError
                    |> (fun x -> {lineNumber = parseInfo.lineNo.[addr] ; error = x})
                    |> Error
            | _ -> Code ins |> Ok
    let mapFolder res wAddr data = 
        match res with
        | Ok okMap ->
            match data with
            | Ok okData -> Map.add wAddr okData okMap |> Ok
            | Error e -> [e] |> Error
        | Error errLst ->
            match data with
            | Ok _ -> Error errLst
            | Error e -> e :: errLst |> Error
    let newMemory = 
        Map.map memMapper parseInfo.dp.MM
        |> Map.fold mapFolder (Map.ofList [] |> Ok)

    Result.map (fun x -> {parseInfo with dp = {parseInfo.dp with MM = x}}) newMemory

// Given a list of correctly parsed instructions, return
// a the datapath with the instructions placed in memory,
// a symbol table with corresponding symbols
// and a mapping from program counters to line numbers
let getInfoFromParsed (lst : (Parse<CommonTop.Instr> * uint32) list) =
    let initialParsedInfo = {dp = initDataPath ; lineNo = Map.ofList [] ; syms = Map.ofList []; pc = 0u}

    placeInstructions lst initialParsedInfo
    |> Result.bind (fun x -> Ok {x with pc = nearestHexHundred x.pc})
    |> Result.bind (placeDirectives lst)
    |> Result.bind (resolveSymbols lst)
    |> Result.bind (resolveDirectives lst)
    |> Result.bind (resolveAdrBranchs)

let dataPathStep (dp : DataPath<Parse<CommonTop.Instr>>) =
    let nextIns = getMemLoc (WA dp.Regs.[R15]) dp
    match nextIns with
    | Code instr ->
        match condExecute instr dp with
        | true -> 
            match instr.PInstr with
            | CommonTop.IDP (DPTop instr') ->
                executeDP instr' dp
            | CommonTop.IMEM (Mem instr') ->
                executeMem instr' dp
            | CommonTop.IBRANCH (Branch instr') ->
                executeBranch instr' dp
            | CommonTop.IMISC (Misc instr') ->
                executeMisc instr' 0u dp
                |> Result.map fst
            | CommonTop.EMPTY _ -> failwithf "Shouldn't be executing empty instruction"
        | false -> 
            updatePC instr dp |> Ok
        |> Result.map (updatePC instr)
    | DataLoc _ ->
        NotInstrMem dp.Regs.[R15] |> Error