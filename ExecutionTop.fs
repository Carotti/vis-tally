module ExecutionTop
    open CommonData
    open DP
    open Memory
    open Branch
    open Misc
    open CommonTop
    open Execution
    open DPExecution
    open MemExecution
    open BranchExecution
    open MiscExecution

    /// The Top level execute instruction taking any Parse<Instr>
    /// and downcasting it to the revelvant memory or data processing
    /// instructions, then calling their executes.
    let execute (instr: CommonLex.Parse<Instr>) (cpuData: DataPath<Instr>) =
        match condExecute instr cpuData with
            | true -> 
                match instr.PInstr with
                | CommonTop.IDP (DPTop instr') ->
                    executeDP instr' cpuData
                | CommonTop.IMEM (Mem instr') ->
                    executeMem instr' cpuData
                // | CommonTop.IBRANCH (Branch instr') ->
                //     executeBranch instr' cpuData
                | CommonTop.IMISC (Misc _instr') ->
                    failwithf "Trying to execute a MISC instruction"
            | false -> 
                updatePC instr cpuData |> Ok
        |> Result.map (updatePC instr)