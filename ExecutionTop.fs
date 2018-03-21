module ExecutionTop
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
    open FsCheck


    let setMemInstr (contents: Parse<CommonTop.Instr>) (addr: uint32) (cpuData: DataPath<Parse<CommonTop.Instr>>) = 
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

    let lineNumList (instrLst: Result<CommonLex.Parse<CommonTop.Instr>, CommonTop.ErrInstr> list) =
        [1u..(List.length instrLst) |> uint32]
        |> List.zip instrLst

    let generateErrorList lineNumLst =
        lineNumLst
        |> List.filter (function | Error _, _ -> true | Ok _, _ -> false)
    
    let makePcToLineNum (instrLst: Result<CommonLex.Parse<CommonTop.Instr>, CommonTop.ErrInstr> list) =
        let getPcInc (instr: Result<CommonLex.Parse<CommonTop.Instr>, CommonTop.ErrInstr>) =
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

    let fillSymTable (instrLst: Result<CommonLex.Parse<CommonTop.Instr>,CommonTop.ErrInstr> list) (symTable: SymbolTable) (cpuData : DataPath<Parse<CommonTop.Instr>>) =
        let rec fillSymTable' (instrLst': Result<CommonLex.Parse<CommonTop.Instr>,CommonTop.ErrInstr> list) (symTable': SymbolTable) (cpuData' : DataPath<Parse<CommonTop.Instr>>) instrAddr dataAddr  =
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