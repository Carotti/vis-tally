module DPExecution
    open CommonData
    open CommonLex
    open DP


    let inline (>>>>) shift num = (>>>) num shift    
    let inline (<<<<) shift num = (<<<) num shift

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
            MM = Map.empty<WAddr,MemLoc<DP.Instr>>
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

    let executeDP (dp:DataPath<Instr>) (instr:CommonLex.Parse<Instr>) : (Result<DataPath<Instr>,ErrExe>) =

        let getBit n (value) =
            value
            |> (<<<<) (31-n)
            |> (>>>>) (31)
        
        let getBitBool n (value:uint32) =
            getBit n value
            |> System.Convert.ToBoolean

        let calcRRX reg (dp:DataPath<Instr>) =
            let c' = getBitBool 0 dp.Regs.[reg]
            let res =
                dp.Regs.[reg] >>> 1
                |> (|||) ((dp.Fl.C |> System.Convert.ToUInt32) <<< 31)
            let flags' = {dp.Fl with C = c'}
            (res, flags')
            
        let doROR b r : uint32 =
            (b >>> r) ||| (b <<< (32-r))
        
        let calcLiteral lit =
            doROR (lit.b |> uint32) RotNums.[lit.r]

        let calcShiftOperand sOp dp =
            match sOp with
            | ConstShift litVal -> calcLiteral litVal
            | RegShift reg -> dp.Regs.[reg]
        
        let doShift shifter shift dp =
            let shiftBy = calcShiftOperand shift.sOp dp |> int32
            let res =
                shiftBy
                |> int32
                |> shifter dp.Regs.[shift.rOp2]
            (res, shiftBy)
        
        let shiftAndCarry shifter shift bitNum dp =
            let res, shiftBy = doShift shifter shift dp
            let C' = getBitBool (bitNum shiftBy) dp.Regs.[shift.rOp2]
            res, {dp.Fl with C = C'}

        let calcShift (shift:FS2Form) dp =
            match shift.sInstr with
            | LSL ->
                shiftAndCarry (<<<) shift (fun s -> 32-s) dp
            | LSR ->
                shiftAndCarry (>>>) shift (fun s -> s-1) dp
            | ASR ->
                shiftAndCarry (fun a b -> (int32 a) >>> b |> uint32) shift (fun s -> s-1) dp
            | ROR ->
                shiftAndCarry (doROR) shift (fun s -> s-1) dp

        let negCheck (flags,op1,op2,value) =
            match value >>> 31 with
            | 1u    ->  {flags with N = true}, op1, op2, value
            | _     ->  {flags with N = false},op2, op2, value

        let zeroCheck (flags,op1,op2,value) =
            match value with
            | 0u    ->  {flags with Z = true}, op1, op2, value
            | _     ->  {flags with Z = false}, op1, op2, value
        
        let additiveCarryCheck (flags,op1,op2,value) =
            let carry = 2.0 ** 32.0 |> uint64
            let value' = (op1 |> uint64) + (op2 |> uint64)
            match value' with
            | x when (x >= carry)   -> {flags with C = true}, op1, op2, value
            | _                     -> {flags with C = false}, op1, op2, value

        let subtractiveCarryCheck (flags, op1, op2, value) =
            let value' = (op1 |> uint64) - (op2 |> uint64)
            match value' with
            | 0UL                                       -> {flags with C = true}, op1, op2, value
            | v when ( v |> uint32 |> getBit 31 = 0u)   -> {flags with C = true}, op1, op2, value
            | _                                         -> {flags with C = false}, op1, op2, value

        let additiveOverflowCheck (flags,op1,op2,value) =
            match op1, op2 with
            // | x, y when ((x >>> 31 = 0u) && (y >>> 31 = 0u)) ->
            | x, y when ((getBit 31 x = 0u) && (getBit 31 y = 0u)) ->
                // match value >>> 31 with
                match getBit 31 value with                
                | 1u -> {flags with V = true}, op1, op2, value
                | _  -> {flags with V = false}, op1, op2, value
            // | x, y when ((x >>> 31 = 1u) && (y >>> 31 = 1u)) ->
            | x, y when ((getBit 31 x = 1u) && (getBit 31 y = 1u)) ->
                // match value >>> 31 with
                match getBit 31 value with                
                | 0u -> {flags with V = true}, op1, op2, value
                | _  -> {flags with V = false}, op1, op2, value
            | _ -> flags, op1, op2, value
        
        let subtractiveOverFlowCheck (flags,op1,op2,value) =
            match op1, op2 with
            | x, y when ((getBit 31 x = 1u) && (getBit 31 y = 0u)) ->
                match getBit 31 value with                
                | 0u -> {flags with V = true}, op1, op2, value
                | _  -> {flags with V = false}, op1, op2, value
            | x, y when ((getBit 31 x = 0u) && (getBit 31 y = 1u)) ->
                match getBit 31 value with                
                | 1u -> {flags with V = true}, op1, op2, value
                | _  -> {flags with V = false}, op1, op2, value
            | _ -> flags, op1, op2, value
        
        let execute dp func dest op1 op2 suffix flagTests : (Result<DataPath<Instr>,ErrExe>) =
            let result = func op1 op2
            let dp' =
                match dest with
                | Some destReg -> updateReg destReg result dp
                | None -> dp
            match suffix with
            | Some S ->
                flagTests
                |> List.fold (fun flags test -> test flags) (dp'.Fl, op1, op2, result)
                |> fun (f, _op1, _op2, _res) -> f
                |> fun f -> {dp' with Fl = f}
                |> Ok
            | None ->
                dp'
                |> Ok

        let (|DP3SMatch|_|) instr =
            match instr.PInstr with
            | (DP3S instr') ->
                match instr' with      
                | (ADD ops) -> Some (instr', ops) 
                | (ADC ops) -> Some (instr', ops)
                | (SUB ops) -> Some (instr', ops) 
                | (SBC ops) -> Some (instr', ops)
                | (RSB ops) -> Some (instr', ops)
                | (RSC ops) -> Some (instr', ops)
                | (AND ops) -> Some (instr', ops)
                | (ORR ops) -> Some (instr', ops)
                | (EOR ops) -> Some (instr', ops)
                | (BIC ops) -> Some (instr', ops)
                | _ -> None
            | _ -> None
            
        let (|DP2Match|_|) instr =
            match instr.PInstr with
            | (DP2 instr') ->
                match instr' with      
                | (CMP ops) -> Some (instr', ops) 
                | (CMN ops) -> Some (instr', ops)
                | (TEQ ops) -> Some (instr', ops)
                | (TST ops) -> Some (instr', ops)
                | _ -> None
            | _ -> None

        let calcOp2 fOp2 dp =
             match fOp2 with
                | Lit litVal    -> calcLiteral litVal, dp.Fl
                | Reg reg       -> dp.Regs.[reg], dp.Fl
                | Shift shift   -> calcShift shift dp
                | RRX reg       -> calcRRX reg dp

        let NZCheck = [negCheck; zeroCheck]
        let CVCheckAdd = [additiveOverflowCheck; additiveCarryCheck;]
        let CVCheckSub = [subtractiveOverFlowCheck; subtractiveCarryCheck]

        let executeDP3S dp opcode (operands:DP3SForm) : (Result<DataPath<Instr>,ErrExe>) =
            let dest = Some operands.rDest
            let op1 = dp.Regs.[operands.rOp1]
            let Cb = dp.Fl.C
            let C = dp.Fl.C |> System.Convert.ToUInt32
            let op2, flags' = calcOp2 operands.fOp2 dp
            // dp' contains the CPSR updated by the barrel shifter
            // dp contains the CPSR that hasn't been updatted by the barrel shifter
            // if S is specified, pick the one that HAS, otherwise pick the original
            let dp' =
                match operands.suff with
                | Some S -> {dp with Fl = flags'}
                | None -> dp
            
            match opcode with
            | ADD _ -> execute dp' (fun op1 op2 -> op1 + op2) dest op1 op2 operands.suff (CVCheckAdd @ NZCheck)
            | ADC _ -> execute dp' (fun op1 op2 -> op1 + op2) dest (op1+C) op2 operands.suff (CVCheckAdd @ NZCheck)
            | SUB _ -> execute dp' (fun op1 op2 -> op1 - op2) dest op1 op2 operands.suff (CVCheckSub @ NZCheck)
            | SBC _ -> execute dp' (fun op1 op2 -> op1 - op2) dest op1 (op2 + (Cb |> not |> System.Convert.ToUInt32)) operands.suff (CVCheckSub @ NZCheck)
            | RSB _ -> execute dp' (fun op1 op2 -> op2 - op1) dest op1 op2 operands.suff (CVCheckSub @ NZCheck)
            | RSC _ -> execute dp' (fun op1 op2 -> op2 - op1) dest (op1 + (Cb |> not |> System.Convert.ToUInt32)) op2 operands.suff (CVCheckSub @ NZCheck)
            | AND _ -> execute dp' (fun op1 op2 -> op1 &&& op2) dest op1 op2 operands.suff NZCheck
            | ORR _ -> execute dp' (fun op1 op2 -> op1 ||| op2) dest op1 op2 operands.suff NZCheck
            | EOR _ -> execute dp' (fun op1 op2 -> op1 ^^^ op2) dest op1 op2 operands.suff NZCheck
            | BIC _ -> execute dp' (fun op1 op2 -> op1 &&& (~~~op2)) dest op1 op2 operands.suff NZCheck

        let executeDP2 dp opcode (operands:DP2Form) : (Result<DataPath<Instr>,ErrExe>) =
            let op1 = dp.Regs.[operands.rOp1]
            let C = dp.Fl.C |> System.Convert.ToUInt32
            let op2, flags' = calcOp2 operands.fOp2 dp
            // No suffix, but can effect CPSR
            let dp' = {dp with Fl = flags'}
            match opcode with
            | CMN _ -> execute dp' (fun op1 op2 -> op1 - op2) None op1 op2 (Some S) (CVCheckSub @ NZCheck)
            | CMP _ -> execute dp' (fun op1 op2 -> op1 + op2) None op1 op2 (Some S) (CVCheckAdd @ NZCheck)
            | TST _ -> execute dp' (fun op1 op2 -> op1 &&& op2) None op1 op2 (Some S) NZCheck
            | TEQ _ -> execute dp' (fun op1 op2 -> op1 ^^^ op2) None op1 op2 (Some S) NZCheck
        
        let dp' =
            match condExecute instr dp with
            | true ->
                match instr with            
                | DP3SMatch (instr', ops) -> executeDP3S dp instr' ops
                | DP2Match (instr', ops) -> executeDP2 dp instr' ops
                | _ ->
                    "Instruction has not been implemented"
                    |> ``Run time error``
                    |> Error
            | false ->
                updatePC instr dp
                |> Ok

        Result.map(updatePC instr) dp'
        
        
 