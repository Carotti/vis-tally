module BranchExecution
    open Branch
    open Expressions
    open CommonData
    open CommonLex
    open Execution
    open Errors

    /// Execute a Branch instruction
    let executeBranch ins dp =
        let nxt = dp.Regs.[R15] + 4u // Address of the next instruction
        match ins with
        | B (ExpUnresolved _)
        | BL (ExpUnresolved _) ->
            failwithf "Trying to execute an unresolved label"
        | B (ExpResolvedByte _) 
        | BL (ExpResolvedByte _) -> 
            failwithf "Trying to execute branch to byte"
        | B (ExpResolved addr) -> 
            dp 
            |> updateReg addr R15
            |> Ok
        | BL (ExpResolved addr) ->
            dp
            |> updateReg addr R15
            |> updateReg nxt R14
            |> Ok
        | END ->
            EXIT |> Error
            