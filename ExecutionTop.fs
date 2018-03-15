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

    let fillSymTable (instrLst: Result<CommonLex.Parse<Instr>,ErrInstr> list) (symTable: SymbolTable) =
        let rec fillSymTable' (instrLst': Result<CommonLex.Parse<Instr>,ErrInstr> list) (symTable': SymbolTable) loc =
            match instrLst' with
            | head :: tail ->
                match head with
                | Ok instr' ->
                    match instr'.PLabel with
                    | Some label ->
                        let symTableNew = symTable'.Add((label |> fst), (loc))
                        fillSymTable' tail symTableNew (loc + instr'.PSize)
                    | None ->
                        fillSymTable' tail symTable' (loc + instr'.PSize)
                | Error _ -> symTable'
            | [] ->
                symTable'
        fillSymTable' instrLst symTable 0u

    /// The Top level execute instruction taking any Parse<Instr>
    /// and downcasting it to the revelvant memory or data processing
    /// instructions, then calling their executes.
    let execute (instr: CommonLex.Parse<Instr>) (symTable: SymbolTable) (cpuData: DataPath<Instr>) =
        match condExecute instr cpuData with
            | true -> 
                match instr.PInstr with
                | CommonTop.IDP (DPTop instr') ->
                    executeDP instr' cpuData
                | CommonTop.IMEM (Mem instr') ->
                    executeMem instr' cpuData
                | CommonTop.IBRANCH (Branch instr') ->
                    executeBranch instr' cpuData
                | CommonTop.IMISC (Misc instr') ->
                    let resolved = resolve symTable instr'
                    executeMisc (resolve instr') minAddress cpuData
            | false -> 
                updatePC instr cpuData |> Ok
        |> Result.map (updatePC instr)