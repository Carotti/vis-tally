module ExecutionTop
    open CommonData
    open DP
    open Memory
    open CommonTop
    open Execution
    open DPExecution
    open MemExecution

    let execute (instr: CommonLex.Parse<Instr>) (cpuData: DataPath<Instr>) : DataPath<Instr> =
        match condExecute instr cpuData with
            | true -> 
                match instr.PInstr with
                | CommonTop.IDP (Shift _) ->
                    executeDP instr cpuData
                | CommonTop.IMEM (Mem _) ->
                    executeMem instr cpuData        
                | _ -> failwithf "Not a valid instruction"
            | false -> 
                updatePC instr cpuData