module ExecutionTop
    open CommonData
    open DP
    open Memory
    open CommonTop
    open Execution
    open DPExecution
    open MemExecution

    /// The Top level execute instruction taking any Parse<Instr>
    /// and downcasting it to the revelvant memory or data processing
    /// instructions, then calling their executes.
    let execute (instr: CommonLex.Parse<Instr>) (cpuData: DataPath<Instr>) : DataPath<Instr> =
        match condExecute instr cpuData with
            | true -> 
                match instr.PInstr with
                | CommonTop.IDP (Shift _) ->
                    executeDP instr cpuData
                | CommonTop.IMEM (Mem _) ->
                    executeMem instr cpuData        
            | false -> 
                updatePC instr cpuData