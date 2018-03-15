module BranchExecution
    open Branch
    open Expressions
    open CommonData
    open Execution
    open Errors

    /// Execute a Branch instruction
    let executeBranch ins cpuData =
        let nxt = cpuData.Regs.[R15] + 4u // Address of the next instruction
        match ins with
        | B (ExpUnresolved _)
        | BL (ExpUnresolved _) ->
            ("BL", " Trying to execute an unresolved label.")
            ||> makeError 
            |> ``Run time error``
            |> Error
        | B (ExpResolvedByte _) 
        | BL (ExpResolvedByte _) -> 
            ("BL", " Trying to execute branch to byte.")
            ||> makeError 
            |> ``Run time error``
            |> Error
        | B (ExpResolved addr) -> 
            cpuData 
            |> updateReg addr R15
            |> Ok
        | BL (ExpResolved addr) ->
            cpuData
            |> updateReg addr R15
            |> updateReg nxt R14
            |> Ok
        | END ->
            EXIT |> Error
            