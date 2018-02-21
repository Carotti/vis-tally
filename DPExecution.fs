module DPExecution
    open CommonData
    open Helpers
    open DP
    open CommonTop
    open Execution

    let executeDP (instr: CommonLex.Parse<Instr>) (cpuData: DataPath<Instr>) : DataPath<Instr> =
        let rotate reg amt = 
            let binaryMask = uint32 (2.0 ** (float amt) - 1.0)
            let lsbs = reg &&& binaryMask
            let msbs = lsbs <<< (32 - amt)
            let shiftedNum = reg >>> amt
            msbs ||| shiftedNum

        let regContents r = cpuData.Regs.[r] // add 0 - 255

        let checkN (value: uint32, flags) =
            match (value >>> 31) with
            | 1u -> 
                value, {flags with N = true}
            | _ -> 
                value, {flags with N = false}
        
        let checkC (value: uint64, flags) =  // neeed to check
            let carry = ((0x10000000 |> uint64) <<< 1) // 2^32
            match value with
            | x when (x >= carry) -> 
                (value |> uint32), {flags with C = true}
            | _ -> 
                (value |> uint32), {flags with C = false}

        let checkZ (value: uint32, flags) =
            match value with
            | 0u -> 
                value, {flags with Z = true}
            | _ -> 
                value, {flags with Z = false}
        
        let checkV (value, flags) = 
            // not required for my instructions
            value, flags


        let checkAllFlags (value, flags) = 
            (value, flags)
            |> checkC
            |> checkN 
            |> checkZ
            |> checkV

                
        let getOp1 op1 = 
            match op1 with
            | Rs reg -> regContents reg
            | N num -> num

        let getOp2 op2 = 
            match op2 with
            | Some (Rs reg) -> regContents reg |> int32
            | Some (N num) -> num |> int32
            | None -> 0

        let executeInstr suffix rd (value: uint64) cpuData =
            let newCpuData = setReg rd (value |> uint32) cpuData
            match suffix with
            | Some S -> 
                let (_, newFlags) = checkAllFlags (value, newCpuData.Fl)
                {newCpuData with Fl = newFlags}
            | None -> newCpuData

        let executeLSL suffix rd rm shift cpuData =
            let value = (getOp1 rm |> uint64) <<< (getOp2 shift)
            executeInstr suffix rd value cpuData
        
        let executeASR suffix rd rm shift cpuData = 
            let value = (getOp1 rm |> int64) >>> (getOp2 shift) |> uint64
            executeInstr suffix rd value cpuData

        let executeLSR suffix rd rm shift cpuData = 
            let value = (getOp1 rm |> uint64) >>> (getOp2 shift)
            executeInstr suffix rd value cpuData

        let executeROR suffix rd rm shift cpuData = 
            let value = rotate (getOp1 rm) (getOp2 shift)
            executeInstr suffix rd (value |> uint64) cpuData

        let executeRRX suffix rd rm shift cpuData = 
            let value = (getOp1 rm) >>> shift
            let value' =
                match cpuData.Fl.C with
                | true -> 0x80000000u ||| value
                | false -> value
            executeInstr suffix rd (value' |> uint64) cpuData
        
        let executeMOV suffix rd rm cpuData = 
            let value = getOp1 rm
            executeInstr suffix rd (value |> uint64) cpuData

        let executeMVN suffix rd rm cpuData = 
            let value = 0xFFFFFFFFu ^^^ (getOp1 rm)
            executeInstr suffix rd (value |> uint64) cpuData

        let executeInstr (instr: ShiftInstr) (cpuData: DataPath<Instr>) =
            match instr with
            | LSL operands -> 
                executeLSL operands.suff operands.Rd operands.Op1 operands.Op2 cpuData
            | ASR operands -> 
                executeASR operands.suff operands.Rd operands.Op1 operands.Op2 cpuData
            | LSR operands -> 
                executeLSR operands.suff operands.Rd operands.Op1 operands.Op2 cpuData
            | ROR operands -> 
                executeROR operands.suff operands.Rd operands.Op1 operands.Op2 cpuData
            | RRX operands -> 
                executeRRX operands.suff operands.Rd operands.Op1 1 cpuData
            | MOV operands ->
                executeMOV operands.suff operands.Rd operands.Op1 cpuData
            | MVN operands ->
                executeMVN operands.suff operands.Rd operands.Op1 cpuData


        match condExecute instr cpuData with
        | true -> 
            match instr.PInstr with
            | CommonTop.IDP (Shift instr') ->
                let cpuData' = executeInstr instr' cpuData   
                updatePC instr cpuData'
            | _ -> failwithf "Not a valid instruction"
        | false -> 
            updatePC instr cpuData
            