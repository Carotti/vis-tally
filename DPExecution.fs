module DPExecution
    open CommonData
    open CommonLex
    open Helpers
    open DP

    let initDP n c z v (regVals: uint32 list) : DataPath<Instr> =
        let flags =
            {N = n; C = c; Z = z; V = v;}

        let fillRegs (regVals: uint32 list) =
            match List.length regVals with
            | 16 ->
                "Input Register Values" |> qp
                regVals
                |> List.zip [0u..15u]
                |> List.map (fun (r,v) -> (makeRegFromNum r, v))
                |> Map.ofList
            | _ ->
                "All Zeros" |> qp
                [0u..15u]
                |> List.zip [0u..15u]
                |> List.map (fun (r, _v) -> (makeRegFromNum r, 0u))
                |> Map.ofList
                
        {
            Fl = flags; 
            Regs = fillRegs regVals; 
            MM = Map.empty<WAddr,MemLoc<Instr>>
        }                
    
    let updatePC (instr: Parse<Instr>) (cpuData: DataPath<Instr>) : DataPath<Instr> =
        let pc = cpuData.Regs.[R15]
        let size = instr.PSize
        setReg R15 (pc + size) cpuData

    let condExecute (instr: Parse<Instr>) (cpuData: DataPath<Instr>) =
        let n, c, z, v = (cpuData.Fl.N, cpuData.Fl.C, cpuData.Fl.Z, cpuData.Fl.V)
        match instr.PCond with
        | Cal -> true
        | Cnv -> false
        | Ceq -> z
        | Cne -> (not z)
        | Chs -> c
        | Clo -> (not c)
        | Cmi -> n
        | Cpl -> (not n)
        | Cvs -> v
        | Cvc -> (not v)
        | Chi -> (c && not z)
        | Cls -> (not c || z)
        | Cge -> (n = v)
        | Clt -> (n <> v)
        | Cgt -> (not z && (n = v))
        | Cle -> (z || (n <> v))
    
    let execute (instr: ShiftInstr) (cpuData: DataPath<Instr>) : DataPath<Instr> =
        let rotate reg amt = 
            let binaryMask = uint32 (2.0 ** (float amt) - 1.0)
            let lsbs = reg &&& binaryMask
            let msbs = lsbs <<< (32 - amt)
            let shiftedNum = reg >>> amt
            msbs ||| shiftedNum

        let regContents r = cpuData.Regs.[r] // add 0 - 255

        let checkN (value: uint32, flags) =
            match (value >>> 31) with
            | 1u -> value, {flags with N = true}
            | _ -> value, {flags with N = false}
        
        let checkC (value: uint64, flags) =  // neeed to check
            let carry = ((0x10000000 |> uint64) <<< 1) // 2^32
            match value with
            | x when (x >= carry) -> (value |> uint32), {flags with C = true}
            | _ -> (value |> uint32), {flags with C = false}

        let checkZ (value: uint32, flags) =
            match value with
            | 0u -> value, {flags with Z = true}
            | _ -> value, {flags with Z = false}
        
        let checkV (value, flags) = 
            // not required for my instructions
            value, flags


        let checkAllFlags value flags = 
            value, flags
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
                let value, newFlags = checkAllFlags value, cpuData.Fl
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

    