module DPExecution
    open CommonData
    open CommonLex
    open CommonTop
    open DP
    open Helpers
    open Errors
    open System.ComponentModel

    /// Bitwise shift operators defined to accept the shift value as first argument 
    ///  and the value to-be-shifted as a second argument. This allows the value
    ///  to-be-shifted to be piped in from previous processing.
    let inline (>>>>) shift num = (>>>) num shift    
    let inline (<<<<) shift num = (<<<) num shift
        
    /// A function that returns a new datapath with the specified register updated.
    let updateReg rX value dp =
        let updater reg old =
            match reg with
            | x when x = rX -> value
            | _ -> old
        {dp with Regs = Map.map updater dp.Regs}

    /// Error types for execution stage.
    type ErrExe =
        | ``Run time error`` of string

    /// Instruction to initiate execution of data processing instructions.
    let executeDP instr (dp: DataPath<CommonTop.Instr>) : (Result<DataPath<CommonTop.Instr>,ErrExe>) =

        /// A helper function to get the `n`th bit of `value`.
        let getBit n (value:uint32) =
            value
            |> (<<<<) (31-n)
            |> (>>>>) (31)
        
        /// A helper function to get `n`th bit of `value` as a bool. This is helpful
        ///  in finding the new value of the CPSR flags.
        let getBitBool n (value:uint32) =
            getBit n value
            |> System.Convert.ToBoolean

        /// A function an RRX and return the would-be values of the CPSR.
        let calcRRX reg (dp:DataPath<CommonTop.Instr>) =
            let c' = getBitBool 0 dp.Regs.[reg]
            let res =
                dp.Regs.[reg] >>> 1
                |> (|||) ((dp.Fl.C |> System.Convert.ToUInt32) <<< 31)
            let flags' = {dp.Fl with C = c'}
            (res, flags')
        
        /// A function to calculate a ROR.
        let doROR b r : uint32 =
            (b >>> r) ||| (b <<< (32-r))
        
        /// A function to calculate the values of literals from the underlying
        ///  byte and rotation.
        let calcLiteral lit =
            doROR (lit.b |> uint32) RotNums.[lit.r]

        /// A function to evaluate the shift value of flexible second operands
        ///  that are shifts.
        let calcShiftOperand sOp dp =
            match sOp with
            // This version is not
            | ConstShift litVal -> calcLiteral litVal
            | RegShift reg -> dp.Regs.[reg]
        
        /// A function to completelty evaluate flexible second operands that are shifts.
        let doShift shifter shift dp =
            let shiftBy = calcShiftOperand shift.sOp dp |> int32
            let res =
                shiftBy
                |> shifter dp.Regs.[shift.rOp2]
            (res, shiftBy)

        /// A function that completelty evaluate flexible second operands that
        ///  and returns the would-be CPSR.
        let shiftAndCarry shifter shift bitNum dp =
            let res, shiftBy = doShift shifter shift dp
            let C' = getBitBool (bitNum shiftBy) dp.Regs.[shift.rOp2]
            res, {dp.Fl with C = C'}

        /// A function that determines whcih shifting operation is to be done, and does this. It also parses a function
        ///  to determine the new C flag. 
        let calcShift (shift:FS2Form) dp =
            match shift.sInstr with
            | SInstr.LSL ->
                shiftAndCarry (<<<) shift (fun s -> 32-s) dp
            | SInstr.LSR ->
                shiftAndCarry (>>>) shift (fun s -> s-1) dp
            | SInstr.ASR ->
                shiftAndCarry (fun a b -> (int32 a) >>> b |> uint32) shift (fun s -> s-1) dp
            | SInstr.ROR ->
                shiftAndCarry (doROR) shift (fun s -> s-1) dp

        /// A function to determine the new value of the N flag.
        let negCheck (flags,op1,op2,value) =
            match value >>> 31 with
            | 1u    ->  {flags with N = true}, op1, op2, value
            | _     ->  {flags with N = false}, op2, op2, value

        /// A function to determine the new value of the Z flag.
        let zeroCheck (flags,op1,op2,value) =
            match value with
            | 0u    ->  {flags with Z = true}, op1, op2, value
            | _     ->  {flags with Z = false}, op1, op2, value
        
        /// A function to determine the new value of the C flag if an additive
        ///  instruction was executed.
        let additiveCarryCheck (flags,op1,op2,value) =
            let carry = 2.0 ** 32.0 |> uint64
            let value' = (op1 |> uint64) + (op2 |> uint64)
            match value' with
            | x when (x >= carry)   -> {flags with C = true}, op1, op2, value
            | _                     -> {flags with C = false}, op1, op2, value
        
        /// A function to determine the new value of the C flag if a subtractive
        ///  instruction was executed.
        let subtractiveCarryCheck (flags, op1, op2, value) =
            let value' = (op1 |> uint64) - (op2 |> uint64)
            value' |> printfn "Hello from the subtractiveCarryCheck: %x"
            value' |> uint32 |> printfn "Hello from the subtractiveCarryCheck: %x"
            value' |> uint32 |> getBit 31 |> printfn "Hello from the subtractiveCarryCheck: %x"
            match value' with
            | 0UL                                       ->
                "C IS HIGH" |> qp
                {flags with C = true}, op1, op2, value
            | v when ( v |> uint32 |> getBit 31 = 0u)   ->
                "C IS HIGH" |> qp
                {flags with C = true}, op1, op2, value
            | _                                         ->
                "C IS LOW" |> qp
                {flags with C = false}, op1, op2, value

        /// A function to determine the new value of the V flag if an additive
        ///  instruction was executed.
        let additiveOverflowCheck (flags,op1,op2,value) =
            match op1, op2 with
            | x, y when ((getBit 31 x = 0u) && (getBit 31 y = 0u)) ->
                match getBit 31 value with                
                | 1u -> {flags with V = true}, op1, op2, value
                | _  -> {flags with V = false}, op1, op2, value
            | x, y when ((getBit 31 x = 1u) && (getBit 31 y = 1u)) ->
                match getBit 31 value with                
                | 0u -> {flags with V = true}, op1, op2, value
                | _  -> {flags with V = false}, op1, op2, value
            | _ -> {flags with V = false}, op1, op2, value
        
        /// A function to determine the new value of the V flag if a subtractive
        ///  instruction was executed.  
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
            | _ -> {flags with V = false}, op1, op2, value
        
        /// A higher-order function for executing DP instructions.
        let execute dp func dest op1 op2 suffix flagTests : (Result<DataPath<CommonTop.Instr>,ErrExe>) =
            let result = func op1 op2
            let dp' =
                match dest with
                | Some destReg -> updateReg destReg result dp
                | None -> dp
            match suffix with
            | Some S ->
                flagTests
                |> List.collect id
                |> List.fold (fun flags test -> test flags) (dp'.Fl, op1, op2, result)
                |> fun (f, _op1, _op2, _res) -> f
                |> fun f -> {dp' with Fl = f}
                |> Ok
            | None ->
                dp'
                |> Ok

        /// An active pattern to match and unpack `DP3S` instructions.
        let (|DP3SMatch|_|) instr =
            match instr with
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

        /// An active pattern to match and unpack `DP2` instructions.   
        let (|DP2Match|_|) instr =
            match instr with
            | (DP2 instr') ->
                match instr' with      
                | (CMP ops) -> Some (instr', ops) 
                | (CMN ops) -> Some (instr', ops)
                | (TEQ ops) -> Some (instr', ops)
                | (TST ops) -> Some (instr', ops)
            | _ -> None
        
        /// An active pattern to match and unpack `DP2S` instructions.   
        let (|DP2SMatch|_|) instr =
            match instr with
            | (DP2S instr') ->
                match instr' with      
                | (MOV ops) -> Some (instr', ops) 
                | (MVN ops) -> Some (instr', ops)
            | _ -> None
        
        /// A function to completely evaluate the value of the flexible second operand.
        let calcOp2 fOp2 dp =
             match fOp2 with
                | Lit litVal    -> calcLiteral litVal, dp.Fl
                | Reg reg       -> dp.Regs.[reg], dp.Fl
                | Shift shift   -> calcShift shift dp
                | RRX reg       -> calcRRX reg dp

        /// A list of checks for the N and Z flags.
        let NZCheck = [negCheck; zeroCheck]

        /// A list of checks for the V and C flags if an additive instruction was executed.
        let CVCheckAdd = [additiveOverflowCheck; additiveCarryCheck;]

        /// A list of checks for the V and C flags if a subtractive instruction was executed.
        let CVCheckSub = [subtractiveOverFlowCheck; subtractiveCarryCheck]

        /// A function to determine which `DP3S` instruction is to be executed, 
        ///  execute it, and return the new datapath.
        let executeDP3S dp opcode (operands:DP3SForm) : (Result<DataPath<CommonTop.Instr>,ErrExe>) =
            let dest = Some operands.rDest
            let op1 = dp.Regs.[operands.rOp1]
            let Cb = dp.Fl.C
            let C = dp.Fl.C |> System.Convert.ToUInt32
            let op2, flags' = calcOp2 operands.fOp2 dp
            let dp' =
                match operands.suff with
                | Some S -> {dp with Fl = flags'}
                | None -> dp
            match opcode with
            | ADD _ -> execute dp' (fun op1 op2 -> op1 + op2) dest op1 op2 operands.suff [CVCheckAdd; NZCheck]
            | ADC _ -> execute dp' (fun op1 op2 -> op1 + op2) dest (op1+C) op2 operands.suff [CVCheckAdd; NZCheck]
            | SUB _ -> execute dp' (fun op1 op2 -> op1 - op2) dest op1 op2 operands.suff [CVCheckSub; NZCheck]
            | SBC _ -> execute dp' (fun op1 op2 -> op1 - op2) dest op1 (op2 + (Cb |> not |> System.Convert.ToUInt32)) operands.suff [CVCheckSub; NZCheck]
            | RSB _ -> execute dp' (fun op1 op2 -> op1 - op2) dest op2 op1 operands.suff [CVCheckSub; NZCheck]
            | RSC _ -> execute dp' (fun op1 op2 -> op1 - op2) dest op2 (op1 + (Cb |> not |> System.Convert.ToUInt32)) operands.suff [CVCheckSub; NZCheck]
            | AND _ -> execute dp' (fun op1 op2 -> op1 &&& op2) dest op1 op2 operands.suff [NZCheck]
            | ORR _ -> execute dp' (fun op1 op2 -> op1 ||| op2) dest op1 op2 operands.suff [NZCheck]
            | EOR _ -> execute dp' (fun op1 op2 -> op1 ^^^ op2) dest op1 op2 operands.suff [NZCheck]
            | BIC _ -> execute dp' (fun op1 op2 -> op1 &&& (~~~op2)) dest op1 op2 operands.suff [NZCheck]


        /// A function to determine which `DP2` instruction is to be executed, 
        ///  execute it, and return the new datapath.
        let executeDP2 dp opcode (operands:DP2Form) : (Result<DataPath<CommonTop.Instr>,ErrExe>) =
            let op1 = dp.Regs.[operands.rOp1]
            let C = dp.Fl.C |> System.Convert.ToUInt32
            let op2, flags' = calcOp2 operands.fOp2 dp
            // No suffix, but can effect CPSR
            let dp' = {dp with Fl = flags'}
            match opcode with
            | CMP _ -> execute dp' (fun op1 op2 -> op1 - op2) None op1 op2 (Some S) [CVCheckSub; NZCheck]
            | CMN _ -> execute dp' (fun op1 op2 -> op1 + op2) None op1 op2 (Some S) [CVCheckAdd; NZCheck]
            | TST _ -> execute dp' (fun op1 op2 -> op1 &&& op2) None op1 op2 (Some S) [NZCheck]
            | TEQ _ -> execute dp' (fun op1 op2 -> op1 ^^^ op2) None op1 op2 (Some S) [NZCheck]
        
        let executeDP2S dp opcode (operands:DP2SForm) : (Result<DataPath<CommonTop.Instr>,ErrExe>) =
            let dest = Some operands.rOp1
            let op1 = 0u
            let Cb = dp.Fl.C
            let C = dp.Fl.C |> System.Convert.ToUInt32
            let op2, flags' = calcOp2 operands.fOp2 dp
            let dp' =
                match operands.suff with
                | Some S -> {dp with Fl = flags'}
                | None -> dp
            match opcode with
            | MOV _ -> execute dp' (fun _op1 op2 -> op2) dest op1 op2 (Some S) [NZCheck]
            | MVN _ -> execute dp' (fun _op1 op2 -> ~~~op2) dest op1 op2 (Some S) [NZCheck]
    
        let dp' : Result<DataPath<CommonTop.Instr>,ErrExe> =
            match instr with            
            | DP3SMatch (instr', ops) -> executeDP3S dp instr' ops 
            | DP2Match (instr', ops) -> executeDP2 dp instr' ops
            | DP2SMatch (instr', ops) -> executeDP2S dp instr' ops
            | _ ->
                "Instruction has not been implemented"
                |> ``Run time error``
                |> Error

        Result.map (id) dp'

 
