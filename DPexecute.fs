module DPexecute
    open CommonData
    open CommonLex
    open DP
    open CommonTop
    open Execution

    type ErrExe =
        | ``Run time error`` of string


    let execute (dp:DataPath<Instr>) (instr:CommonLex.Parse<Instr>) : (Result<DataPath<Instr>,ErrExe>) =

        let executeADD (dp:DataPath<Instr>) (instr:CommonLex.Parse<Instr>) : (Result<DataPath<Instr>,ErrExe>) =
            let operands =
                match instr.PInstr with
                | CommonTop.IDP (ADD instr') -> instr'
                | _ -> failwithf "Only DP instructions have been implemented as of yet."
            
            match operands.fOp2 with
            | Reg (reg) ->
                let newValue = dp.Regs.[operands.rOp1] + dp.Regs.[reg]
                updateReg (operands.rDest) newValue dp
                |> Ok


        match condExecute instr dp with
        | true ->
            match instr.PInstr with
            | CommonTop.IDP (ADD _ ) ->
                executeADD dp instr
                |> Result.map(updatePC instr)
            | _ ->
                "Just a dummy error"
                |> ``Run time error``
                |> Error
        | false ->
            updatePC instr dp
            |> Ok


        





