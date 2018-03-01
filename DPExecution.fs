module DPExecution
    open CommonData
    open Helpers
    open DP
    open CommonTop
    open Execution

    /// Data Processing Execution function
    let executeDP (instr: CommonLex.Parse<Instr>) (cpuData: DataPath<Instr>) : DataPath<Instr> =
        /// Rotate for ROR 
        /// reg is value in register
        /// amt the amount you want to rotate by
        let rotate reg amt = 
            let binaryMask = uint32 (2.0 ** (float amt) - 1.0)
            let lsbs = reg &&& binaryMask
            let msbs = lsbs <<< (32 - amt)
            let shiftedNum = reg >>> amt
            msbs ||| shiftedNum

        let regContents r = cpuData.Regs.[r] // add 0 - 255

        /// Check if Negative flag should be set
        let checkN (value: uint32, flags) =
            match (value >>> 31) with
            | 1u -> value, {flags with N = true}
            | _ -> value, {flags with N = false}

        /// Check if Carry flag should be set
        /// Requires 64bit uint to check if there
        /// was a carry. In execute functions the output
        /// is a 64bit uint32 which is converted to 32 here
        let checkC (value: uint64, flags) =  // neeed to check
            let carry = ((0x80000000 |> uint64) <<< 1) // 2^32
            match value with
            | x when (x >= carry) -> (value |> uint32), {flags with C = true}
            | _ -> (value |> uint32), {flags with C = false}

        /// Check if Zero flag should be set
        let checkZ (value: uint32, flags) =
            match value with
            | 0u -> value, {flags with Z = true}
            | _ -> value, {flags with Z = false}
        
        /// Check if Overflow flag should be set
        /// Not required for my instructions
        /// Chris has an implementation
        let checkV (value, flags) = 
            value, flags

        /// Check all of the flags
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

        /// Shift global execute function
        /// Sets the registers and updats flags
        /// returns a new data path
        let executeInstr suffix rd (value: uint64) cpuData =
            let newCpuData = setReg rd (value |> uint32) cpuData
            match suffix with
            | Some S -> 
                let (_, newFlags) = checkAllFlags (value, newCpuData.Fl)
                {newCpuData with Fl = newFlags}
            | None -> newCpuData

        /// Logical Shift Left Execute
        let executeLSL suffix rd rm shift cpuData =
            let value = (getOp1 rm |> uint64) <<< (getOp2 shift)
            executeInstr suffix rd value cpuData
        
        /// Arithmetic Shift Right Execute
        let executeASR suffix rd rm shift cpuData = 
            let value = (getOp1 rm |> int64) >>> (getOp2 shift) |> uint64
            executeInstr suffix rd value cpuData

        /// Logical Shift Right Execute
        let executeLSR suffix rd rm shift cpuData = 
            let value = (getOp1 rm |> uint64) >>> (getOp2 shift)
            executeInstr suffix rd value cpuData

        /// Rotate Right Execute
        let executeROR suffix rd rm shift cpuData = 
            let value = rotate (getOp1 rm) (getOp2 shift)
            executeInstr suffix rd (value |> uint64) cpuData

        /// Rotate Right Extend (by 1) Execute
        let executeRRX suffix rd rm shift cpuData = 
            let value = (getOp1 rm) >>> shift
            let value' =
                match cpuData.Fl.C with
                | true -> 0x80000000u ||| value
                | false -> value
            executeInstr suffix rd (value' |> uint64) cpuData
        
        /// Move Execute
        let executeMOV suffix rd rm cpuData = 
            let value = getOp1 rm
            executeInstr suffix rd (value |> uint64) cpuData

        /// Move Not Execute
        let executeMVN suffix rd rm cpuData = 
            let value = ~~~ (getOp1 rm)
            executeInstr suffix rd (value |> uint64) cpuData

        /// All the shift instructions
        let executeShiftInstr (instr: ShiftInstr) (cpuData: DataPath<Instr>) =
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

        /// Check flags and conditionals, using Tom's condExecute (as he dealt with them first)
        /// Match the instruction, execute then update program counter by 4
        match condExecute instr cpuData with
        | true -> 
            match instr.PInstr with
            | CommonTop.IDP (Shift instr') ->
                let cpuData' = executeShiftInstr instr' cpuData   
                updatePC instr cpuData'
            | _ -> failwithf "Not a valid instruction"
        | false -> 
            updatePC instr cpuData
            
