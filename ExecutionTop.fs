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

    let generateErrorList (instrLst: Result<CommonLex.Parse<CommonTop.Instr>, CommonTop.ErrInstr> list) =
        [1u..(List.length instrLst) |> uint32]
        |> List.zip instrLst
        |> List.filter (function | Error _, _ -> true | Ok _, _ -> false)

    let fillSymTable (instrLst: Result<CommonLex.Parse<CommonTop.Instr>,CommonTop.ErrInstr> list) (symTable: SymbolTable) (cpuData : DataPath<CommonTop.Instr>) =
        let rec fillSymTable' (instrLst': Result<CommonLex.Parse<CommonTop.Instr>,CommonTop.ErrInstr> list) (symTable': SymbolTable) (cpuData' : DataPath<CommonTop.Instr>) loc =
            match instrLst' with
            | head :: tail ->
                match head with
                | Ok instr' ->
                    match instr'.PInstr with
                    | CommonTop.IMISC (Misc instr'') ->
                        let resInstr = miscResolve instr'' symTable' 
                        executeMisc resInstr minAddress cpuData'
                        |> function
                        | Ok cpuData''' ->  fillSymTable' tail symTable' cpuData''' loc     
                        | Error _ -> failwithf "Woaaaaaaaaaaaaaah we need to sort this"         
                    | _ ->
                        match instr'.PLabel with
                        | Some label ->
                            let symTableNew = symTable'.Add((label |> fst), (loc))
                            fillSymTable' tail symTableNew cpuData' (loc + instr'.PSize)
                        | None ->
                            fillSymTable' tail symTable' cpuData' (loc + instr'.PSize)
                | Error _ -> symTable', cpuData'
            | [] -> symTable', cpuData'
        fillSymTable' instrLst symTable cpuData 0u

    /// The Top level execute instruction taking any Parse<Instr>
    /// and downcasting it to the revelvant memory or data processing
    /// instructions, then calling their executes.
    let execute (instr: CommonLex.Parse<CommonTop.Instr>) (cpuData: DataPath<CommonTop.Instr>) (symTable: SymbolTable) =
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
                    let resInstr = miscResolve instr' symTable
                    executeMisc resInstr minAddress cpuData
                | CommonTop.EMPTY _ ->
                    cpuData |> Ok
            | false -> 
                updatePC instr cpuData |> Ok
        |> Result.map (updatePC instr)