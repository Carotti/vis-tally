module DPExecution
    open CommonData
    open CommonLex
    open DP
    open CommonTop


    let inline (||||>) (a,b,c,d) f = f a b c d

    let initialiseDP n c z v (regVals:uint32 list) : DataPath<Instr> =
        let flags =
            {N = n; C = c; Z = z; V = v;}

        let fillRegs (regVals:uint32 list) =
            match List.length regVals with
            | 16 ->
                regVals
                |> List.zip [0u..15u]
                |> List.map (fun (r,v) -> (consRegG r, v))
                |> Map.ofList
            | _ ->
                [0u..15u]
                |> List.zip [0u..15u]
                |> List.map (fun (r, _v) -> (consRegG r, 0u))
                |> Map.ofList
                
        {
            Fl = flags; 
            Regs = fillRegs regVals; 
            MM = Map.empty<WAddr,MemLoc<Instr>>
        }                

    /// Return a new datapath with reg rX set to value
    let updateReg rX value dp =
        let updater reg old =
            match reg with
            | x when x = rX -> value
            | _ -> old
        {dp with Regs = Map.map updater dp.Regs}

    let updatePC (instr:CommonLex.Parse<Instr>) (dp:DataPath<Instr>) : DataPath<Instr> =
        let pc = dp.Regs.[R15]
        let size = instr.PSize
        updateReg R15 (pc+size) dp
 
    /// Return whether or not an instruction should be executed
    let condExecute (ins:CommonLex.Parse<Instr>) (data : DataPath<Instr>) =
        let (n, c, z, v) = (data.Fl.N, data.Fl.C, data.Fl.Z, data.Fl.V)
        match ins.PCond with
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

    type ErrExe =
        | ``Run time error`` of string

    let execute (dp:DataPath<Instr>) (instr:CommonLex.Parse<Instr>) : (Result<DataPath<Instr>,ErrExe>) =

        let calcRRX reg (dp:DataPath<Instr>) =
            let c' = dp.Regs.[reg] % 2u |> System.Convert.ToBoolean
            let res =
                dp.Regs.[reg] >>> 1
                |> (|||) ((dp.Fl.C |> System.Convert.ToUInt32) <<< 31)
            let flags' = {dp.Fl with C = c'}
            
            let dp' = {dp with Fl = flags'}
            (res, dp')
            
        let doROR b r : uint32 =
            (b >>> r) ||| (b <<< (32-r))
        
        let calcLiteral lit =
            doROR (lit.b |> uint32) RotNums.[lit.r]

        let calcShiftOperand sOp dp =
            match sOp with
            | ConstShift litVal -> calcLiteral litVal
            | RegShift reg -> dp.Regs.[reg]
        
        let doShift shifter shift dp =
            calcShiftOperand shift.sOp dp
            |> int32
            |> shifter dp.Regs.[shift.rOp2]
  
        let calcShift (shift:FS2Form) dp =
            match shift.sInstr with
            | LSL -> doShift (<<<) shift dp
            | LSR -> doShift (>>>) shift dp
            | ASR -> doShift (fun a b -> (int a) >>> b |> uint32) shift dp
            | ROR -> doShift (doROR) shift dp

        let negCheck flags value =
            match value >>> 31 with
            | 1u    ->  {flags with N = true}, value
            | _     ->  {flags with N = false}, value

        let zeroCheck flags value =
            match value with
            | 0u    ->  {flags with Z = true}, value
            | _     ->  {flags with Z = false}, value
        
        let carryCheckAdd flags (op1:uint32) (op2:uint32) value =
            let carry = 2.0 ** 32.0 |> uint64
            let value' = (op1 |> uint64) + (op2 |> uint64)
            match value' with
            | x when (x >= carry)   -> {flags with C = true}, value
            | _                     -> {flags with C = false}, value

        let overflowCheckAdd flags (op1:uint32) (op2:uint32) value =
            match op1, op2 with
            | x, y when ((x >>> 31 = 0u) && (y >>> 31 = 0u)) ->
                match value >>> 31 with
                | 1u -> {flags with V = true}, op1, op2, value
                | _  -> {flags with V = false}, op1, op2, value
            | x, y when ((x >>> 31 = 1u) && (y >>> 31 = 1u)) ->
                match value >>> 31 with
                | 0u -> {flags with V = true}, op1, op2, value
                | _  -> {flags with V = false}, op1, op2, value
            | _ -> flags, op1, op2, value
        
        let flagChecksAdd flags (op1:uint32) (op2:uint32) value =
            (flags, op1, op2, value)
            ||||> overflowCheckAdd 
            ||||> carryCheckAdd
            ||> negCheck
            ||> zeroCheck
            |> fst

        let executeADD dp dest (op1:uint32) (op2:uint32) suffix : (Result<DataPath<Instr>,ErrExe>) =
            let result = op1 + op2
            let dp' = updateReg dest result dp
            match suffix with
            | Some S ->
                let flags' = flagChecksAdd dp.Fl op1 op2 result
                {dp' with Fl = flags'} |> Ok
            | None ->
                dp' |> Ok 
    
        let executeLOGIC dp dest logic (op1:uint32) (op2:uint32) suffix : (Result<DataPath<Instr>,ErrExe>) =
            let result = (logic) op1 op2
            let dp' = updateReg dest result dp
            match suffix with
            | Some S ->
                let flags' = (dp.Fl, result) ||> negCheck ||> zeroCheck |> fst 
                {dp' with Fl = flags'} |> Ok
            | None ->
                dp' |> Ok 

        let unpackOperands instr =
            match instr with
                | ADD ops -> ops
                | ADC ops -> ops
                | AND ops -> ops
                | ORR ops -> ops
                | EOR ops -> ops
                | BIC ops -> ops
                | _ -> failwithf "Only DP instructions have been implemented as of yet."
            
        let executeDP3S (dp:DataPath<Instr>) (instr:DP3SInstr) : (Result<DataPath<Instr>,ErrExe>) =
            let operands = unpackOperands instr
            let dest = operands.rDest
            let op1 = dp.Regs.[operands.rOp1]
            let C = dp.Fl.C |> System.Convert.ToUInt32
            // Must obtain another a DataPath since RRX can change CPSR if suffix S is used
            // TODO: check others!
            let op2, dp' =
                match operands.fOp2 with
                | Lit litVal    -> calcLiteral litVal, dp
                | Reg reg       -> dp.Regs.[reg], dp
                | Shift shift   -> calcShift shift dp, dp
                | RRX reg       -> calcRRX reg dp

            match instr with
            | ADD _ -> executeADD dp' dest op1 op2 operands.suff
            | ADC _ -> executeADD dp' dest (op1+C) op2 operands.suff
            | AND _ -> executeLOGIC dp' dest (&&&) op1 op2 operands.suff
            | ORR _ -> executeLOGIC dp' dest (|||) op1 op2 operands.suff
            | EOR _ -> executeLOGIC dp' dest (^^^) op1 op2 operands.suff
            | BIC _ -> executeLOGIC dp' dest (&&&) op1 (~~~op2) operands.suff
             
        match condExecute instr dp with
        | true ->
            match instr.PInstr with
            | CommonTop.IDP (DP3S instr') ->
                executeDP3S dp instr'
                |> Result.map(updatePC instr)
            | _ ->
                "Just a dummy error"
                |> ``Run time error``
                |> Error
        | false ->
            updatePC instr dp
            |> Ok