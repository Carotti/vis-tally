module DP

    open CommonData
    open CommonLex
    open System.Text.RegularExpressions
    open FsCheck

    /// Quick print helper function.
    let qp thing = thing |> printfn "%A"

    /// Quick print helper function for lists with nicer formatting.
    let qpl lst = lst |> List.map (qp)

    /// Rotation values for the `Literal` type in the flexible second operand.
    [<Struct>]
    type RotVal =
        | Rot0     | Rot2     | Rot4     | Rot6
        | Rot8     | Rot10    | Rot12    | Rot14  
        | Rot16    | Rot18    | Rot20    | Rot22 
        | Rot24    | Rot26    | Rot28    | Rot30

    /// Map for converting from the `RVal` type to the underlying numerical
    ///  value used for the rotation in the `Literal` of the flexible second
    ///  operand.
    let RotNums =
        Map.ofList [
            (Rot0, 0);   (Rot2, 2);     (Rot4, 4);   (Rot6, 6);
            (Rot8, 8);   (Rot10, 10);   (Rot12, 12); (Rot14, 14);  
            (Rot16, 16); (Rot18, 18);   (Rot20, 20); (Rot22, 22); 
            (Rot24, 24); (Rot26, 26);   (Rot28, 28); (Rot30, 30);
        ]
    
    /// Map for converting from a numerical value to the `RVal` used to
    ///  represent the rotation in the `Literal` of the flexible second
    ///  operand.
    let RotVals =
        Map.ofList [
            (0, Rot0);   (2, Rot2);     (4, Rot4);   (6, Rot6);
            (8, Rot8);   (10, Rot10);   (12, Rot12); (14, Rot14);  
            (16, Rot16); (18, Rot18);   (20, Rot20); (22, Rot22); 
            (24, Rot24); (26, Rot26);   (28, Rot28); (30, Rot30);
        ]

    /// Literal type for allowed literals.
    ///  `b` is the underlying byte.
    ///  `r` is the rotation that is applied to `b`.
    type Literal =
        {
            b: byte;
            r: RotVal;
        }
        
    /// Possible shift operands for the shift instructions `SIns`.
    type SOp =
        | ConstShift    of Literal
        | RegShift      of RName
   
    /// Shift instructions that require operands and are compatible with the
    ///  flexible second operand. `RRX` is not included since it does not
    ///  require any further operands.
    type SInstr =
        | LSL
        | LSR
        | ASR
        | ROR
    
    /// Map for converting `strings` containing shift instructions (used in
    ///  the flexible second operand) to shift instructions of type `SInstr`.
    let sInstrs =
        Map.ofList [
            ("LSL", LSL);
            ("LSR", LSR);
            ("ASR", ASR);
            ("ROR", ROR);     
        ]
    
    // This is mainly used in property based testing to allow shift instructions
    //  to be picked by Expecto.
    /// Map for converting SInstr containing shift instructions (used in
    ///  the flexible second operand) to their string representation.
    let sInstrsStr =
        Map.ofList [
            (LSL, "LSL");
            (LSR, "LSR");
            (ASR, "ASR");
            (ROR, "ROR");     
        ]
    
    /// Type representation of the optional suffix `S`.
    type Suffix =
        | S
     
    /// Flexible shift instruction format within the flexible second operand.
    type FS2Form =
        {
            rOp2:RName;
            sInstr:SInstr;
            sOp:SOp
        }

    /// Flexible second operand format.
    type FlexOp2 =
        | Lit       of Literal
        | Reg       of RName
        | Shift     of FS2Form
        | RRX       of RName

    /// Operand format for three-operand data processing instructions. 
    type DP3Form =
        {
            rDest:RName;
            rOp1:RName;
            fOp2:FlexOp2
        }

    /// Operand format including an optional suffix for three-operand data
    ///  processing instructions. 
    type DP3SForm =
        {
            rDest:RName;
            rOp1:RName;
            fOp2:FlexOp2;
            suff:Option<Suffix>;
        }

    /// Operand format for two-operand data processing instructions. 
    type DP2Form =
        {
            rOp1:RName;
            fOp2:FlexOp2;
        }

    /// Operand format including an optional suffix for two-operand data
    ///  processing instructions. 
    type DP2SForm =
        {
            rOp1:RName;
            fOp2:FlexOp2;
            suff:Option<Suffix>;
        }
     
    /// Operand format for the first two operands of the three-operand data
    ///  processing instructions. 
    type DP32Form =
        {
            rDest:RName;
            rOp1: RName
        }
    
    /// All DP3S instructions, that is data processing instructions that have
    ///  three operands (with a flexible second operand) and an optional suffix.
    type DP3SInstr =
        | ADD of DP3SForm
        | ADC of DP3SForm
        | SUB of DP3SForm
        | SBC of DP3SForm
        | RSB of DP3SForm
        | RSC of DP3SForm
        | AND of DP3SForm
        | ORR of DP3SForm
        | EOR of DP3SForm
        | BIC of DP3SForm

    /// All DP2 instructions, that is data processing instructions that have
    ///  two operands (with a flexible second operand) and no optional suffix.
    type DP2Instr =
        | CMP of DP2Form   
        | CMN of DP2Form
        | TEQ of DP2Form    
        | TST of DP2Form
    
    /// Top level instruction type for data processing instructions.
    type Instr =
        | DP3S of DP3SInstr
        | DP2 of DP2Instr
  
    /// Error types for parsing.
    type ErrInstr =
        | ``Invalid literal``       of string
        | ``Invalid register``      of string
        | ``Invalid shift``         of string
        | ``Invalid flexible second operand``  of string
        | ``Invalid suffix``        of string
        | ``Invalid instruction``   of string
        | ``Syntax error``          of string

    /// Constructs a `DP2S` from an optional suffix and a `DP2`
    let consDP2S suffix (dp2:DP2Form) =
        {
            rOp1 = dp2.rOp1
            fOp2 = dp2.fOp2
            suff = suffix
        }
    
     /// Constructs a `DP3S` from an optional suffix and a `DP3`
    let consDP3S suffix (dp3:DP3Form) =
        {
            rDest = dp3.rDest
            rOp1 = dp3.rOp1
            fOp2 = dp3.fOp2
            suff = suffix
        }

    let DPSpec =
        {
            InstrC = DP
            Roots = [   "ADD"; "ADC"; "SUB";
                        "SBC"; "RSB"; "RSC";
                        "AND"; "ORR"; "EOR";    
                        "BIC"; "CMP"; "CMN";
                        "TST"; "TEQ"
                    ]
            Suffixes = [""; "S"]
        }

    /// Constructs a register name of type `RName` from a register specified as a `string`.
    let consReg reg =
        regNames.[reg]

    /// A general version of `consReg` that constructs a register name of type `RName`.
    let consRegG reg =
        reg |> string |> (+) "R" |> consReg

    /// Constructs an operand record of type `DP3Form` from registers specified as strings.
    let consDP3 rDest' rOp1' fOp2' =
        {
            rDest = regNames.[rDest'];
            rOp1 = regNames.[rOp1'];
            fOp2 = fOp2'
        }
    
    /// Constructs an operand record of type `DP3Form` from registers specified as type `RName`.
    let consDP3R rDest' rOp1' fOp2' =
        {
            rDest = rDest';
            rOp1 = rOp1';
            fOp2 = fOp2'
        }

    /// Constructs an operand record of type `DP2Form` from registers specified as strings.
    let consDP2 rOp1' fOp2' =
        {
            rOp1 = regNames.[rOp1'];
            fOp2 = fOp2'
        }

    /// Constructs an operand record of type `DP2Form` from registers specified as type `RName`.    
    let consDP2R rOp1' fOp2' =
        {
            rOp1 = rOp1';
            fOp2 = fOp2'
        }

    /// Constructs an operand record of type `DP32Form` from registers specified as strings.
    let consDP32 rDest' rOp1' =
        {
            rDest = regNames.[rDest'];
            rOp1 = regNames.[rOp1']
        }

    /// Joins a `DP32Form` and a `FlexOp2` to create a `DP3Form`.
    let joinDP dp2 fOp2' =
        {
            rDest = dp2.rDest;
            rOp1 = dp2.rOp1;
            fOp2 = fOp2'
        }

    /// Constructs an operand record of type `FS2Form` from registers specified as strings.
    let consFS2 rOp2' sInstr' sOp'=
        {
            rOp2 = consReg(rOp2');
            sInstr = sInstr';
            sOp = sOp';
        }
  
    /// Constructs an operand record of type `FS2Form` from registers specified as `RName`.
    let consFS2R rOp2' sInstr' sOp'=
        {
            rOp2 = rOp2';
            sInstr = sInstr';
            sOp = sOp';
        }
    
    /// Constructs a shift sub-instruction for the flexible second operand of
    ///  type `SInstr` from a shift instruction specified as a `string`
    let consSInstr instr =
        sInstrs.[instr]

    /// Constructs a literal record of type `Literal` from a rotation values
    ////  specified as an `int`.
    let consLit (b', r') =
        {
            b = b';
            r = RotVals.[r']
        }

    /// Constructs a literal record of type `FlexOp2` from a rotation values
    ///  specified as an `int`.
    let consLitOp (b', r') =
        Lit (consLit (b', r'))
    
    /// map of all possible opcodes recognised
    let opCodes = opCodeExpand DPSpec

    /// A function to combine results or forward errors.
    let combineError (res1:Result<'T1,'E>) (res2:Result<'T2,'E>) : Result<'T1 * 'T2, 'E> =
        match res1, res2 with
        | Error e1, _ -> Error e1
        | _, Error e2 -> Error e2
        | Ok rt1, Ok rt2 -> Ok (rt1, rt2)

    /// A function that combines two results by applying a function on them as a pair, or forwards errors.
    let combineErrorMapResult (res1:Result<'T1,'E>) (res2:Result<'T2,'E>) (mapf:'T1 -> 'T2 -> 'T3) : Result<'T3,'E> =
        combineError res1 res2
        |> Result.map (fun (r1,r2) -> mapf r1 r2)
    
    /// A function that applies a possibly erroneous function to a possibly erroneous argument, or forwards errors.
    let applyResultMapError (res:Result<'T1->'T2,'E>) (arg:Result<'T1,'E>) =
        match arg, res with
        | Ok arg', Ok res' -> res' arg' |> Ok
        | _, Error e -> e |> Error
        | Error e, _ -> e |> Error
   

    /// main function to parse a line of assembler
    /// ls contains the line input
    /// and other state needed to generate output
    /// the result is None if the opcode does not match
    /// otherwise it is Ok Parse or Error (parse error string)
    let parse (ld: LineData) : Result<Parse<Instr>,ErrInstr> option =
      
        /// A function to check the validity of literals according to the ARM spec.
        let checkLiteral lit =
            let rotMask n = (0xFFu >>> n) ||| (0xFFu <<< 32 - n)
            [0..2..30] 
            |> List.map (fun r -> rotMask r, r)
            |> List.filter (fun (mask, _r) -> (mask &&& lit) = lit)
            |> function
            | hd :: _tl ->
                let rotB = fst hd |> (&&&) lit
                let B = (rotB <<< snd hd) ||| (rotB >>> 32 - snd hd) |> byte
                Ok (B, snd hd)
            | [] ->
                (lit |> string) + " is not a valid literal."
                |> ``Invalid literal``
                |> Error

        /// A partially active pattern to parse regexes, and return the matched group.
        let (|ParseRegex|_|) regex txt =
            let m = Regex.Match(txt, "^[\\s]*" + regex + "[\\s]*" + "$")
            match m.Success with
            | true -> Some (m.Groups.[1].Value)
            | false -> None

        /// A partially active pattern to match literals according to the ARM spec,
        ///  and return a numerical representation of the literal if it is valid.
        let (|LitMatch|_|) txt =
            match txt with
            | ParseRegex "#&([0-9a-fA-F]+)" num -> 
                (uint32 ("0x" + num)) |> checkLiteral |> Some
            | ParseRegex "#(0B[0-1]+)" num
            | ParseRegex "#(0X[0-9a-fA-F]+)" num
            | ParseRegex "#([0-9]+)" num ->
                num |> uint32 |> checkLiteral |> Some
            | _ ->
                None

        /// A partially active pattern to check validity of a register passed as a string,
        /// and return an `RName` if it is valid.
        let (|RegMatch|_|) txt =
            match Map.tryFind txt regNames with
            | Some reg ->
                reg |> Ok |> Some
            | _ ->
                None

        /// A partially active pattern that returns an error if a register argument is not valid.
        let (|RegCheck|_|) txt =
            match Map.tryFind txt regNames with
            | Some reg ->
                reg |> Ok |> Some
            | _ ->
                txt + " is not a valid register."
                |> ``Invalid register``
                |> Error
                |> Some
        
        /// A partially active pattern to match RRXs and, if valid, to return the
        ///  `RName` of the  register upon which the rotation will be done
        let (|RrxMatch|_|) reg txt =
            match txt with
            | ParseRegex "(^RRX)" _ ->
                match reg with
                | RegCheck reg' ->
                    reg' |> Some
                | _ ->
                    failwith "Should never happen! Match statement always matches."
            | _ ->
                None

        /// A partially active pattern to match shift sub-instructions and, if valid,
        ///  to return the instruction in `SInstr` form.
        let (|ShiftInstr|_|) txt =
            match txt with
            | ParseRegex "(LSL)" _ 
            | ParseRegex "(LSR)" _ 
            | ParseRegex "(ASR)" _ 
            | ParseRegex "(ROR)" _ -> txt |> consSInstr |> Some
            | _ -> None

        /// A partially active pattern to match shift flexible second operands
        ///  and return a flexible second operand if it is valid.
        let (|ShiftMatch|_|) (reg:string) (txt:string) =
            match String.length txt with
            | x when x < 5 ->
                None
            | _ ->
                let instr = txt.[0..2]
                let oprnds = txt.[3..]
                match instr with
                | ShiftInstr sInstr ->
                    match reg with
                    | RegCheck reg' ->
                        match reg' with
                        | Ok reg'' ->
                            let partialFS2 = consFS2R reg'' sInstr
                            match oprnds with
                            | LitMatch litVal ->
                                litVal
                                |> Result.map(consLit)
                                |> Result.map(ConstShift)
                                |> Result.map(partialFS2)
                                |> Some
                            | RegMatch rOp3 ->
                                rOp3
                                |> Result.map(RegShift)
                                |> Result.map(partialFS2)
                                |> Some
                            | _ ->
                                oprnds + " is not a valid literal or register."
                                |> ``Invalid shift``
                                |> Error
                                |> Some
                        | Error e ->
                            e
                            |> Error
                            |> Some
                    | _ ->
                        failwith "Should never happen! Match statement always matches."
                | _ ->
                    None  

        /// A function to parse the flexible second operand if it has no extension.
        ///  An extention is a fourth comma-seperated operand.
        let parseFOp2NoExtn op2 createOp =
            match op2 with
                | LitMatch litVal ->
                    let litVal' = Result.map (consLitOp) litVal
                    applyResultMapError createOp litVal'
                | RegMatch reg ->
                    let reg' = Result.map (Reg) reg
                    applyResultMapError createOp reg'
                | _ ->
                    op2 + " is an invalid flexible second operand"
                    |> ``Invalid flexible second operand``
                    |> Error
                    |> applyResultMapError createOp

        /// A function to parse the flexible second operand if it has an extension.
        ///  An extention is a fourth comma-seperated operand.
        let parseFOp2Extn rOp2 extn createOp =
            match extn with
            | RrxMatch rOp2 reg ->
                let reg' = Result.map (RRX) reg
                applyResultMapError createOp reg'
            | ShiftMatch rOp2 shift ->
                let shift' = Result.map (Shift) shift
                applyResultMapError createOp shift'
            | _ ->
                rOp2 + ", " + extn + " is an invalid flexible second operand"
                |> ``Invalid flexible second operand``
                |> Error
                |> applyResultMapError createOp

        /// A function to parse instructions with three comma-seperated operands.
        let parse3Ops rDest rOp1 op2 =
            match rDest, rOp1 with
            | RegCheck rDest', RegCheck rOp1' ->
                let dp32 = combineErrorMapResult rDest' rOp1' consDP3R
                parseFOp2NoExtn op2 dp32
            | _ ->
                failwith "Should never happen! Match statement always matches."

        /// A function to parse instructions with four comma-seperated operands.
        let parse4Ops rDest rOp1 rOp2 extn =
            match rDest, rOp1 with
            | RegCheck rDest', RegCheck rOp1' ->
                let dp32 = combineErrorMapResult rDest' rOp1' consDP3R
                parseFOp2Extn rOp2 extn dp32
            | _ ->
                failwith "Should never happen! Match statement always matches."

        /// Lazy data representing the operands for `DP2` instructions.    
        let operandsDP2 = 
            lazy (
                ld.Operands.Split([|','|])
                |> Array.toList
                |> List.map (fun op -> op.ToUpper())
                |> function
                | [rOp1; op2] ->
                    match rOp1 with
                    | RegCheck rOp1' ->
                        let dp2 = Result.map(consDP2R) rOp1'
                        parseFOp2NoExtn op2 dp2
                    | _ ->
                        failwith "Should never happen! Match statement always matches."
                | [rOp1; op2; extn] ->
                    match rOp1 with
                    | RegCheck rOp1' ->
                        let dp2 = Result.map(consDP2R) rOp1'
                        parseFOp2Extn op2 extn dp2
                    | _ ->
                        failwith "Should never happen! Match statement always matches."  
                | _ ->
                    "Syntax error. Instruction format is incorrect."
                    |> ``Invalid instruction``
                    |> Error
            ) 

        /// Lazy data representing the operands for `DP3` instructions.    
        let operandsDP3 = 
            lazy (
                ld.Operands.Split([|','|])
                |> Array.toList
                |> List.map (fun op -> op.ToUpper())
                |> function
                | [rDest; rOp1; op2] ->
                    parse3Ops rDest rOp1 op2
                | [rDest; rOp1; rOp2; extn] ->
                    parse4Ops rDest rOp1 rOp2 extn
                | _ ->
                    "Syntax error. Instruction format is incorrect."
                    |> ``Invalid instruction``
                    |> Error
            )  
            
        let (WA la) = ld.LoadAddr

        /// A helper function for quick construction of a complete top-level instruction.
        let makeInstr cond instr =
            {
                PInstr  = instr; 
                PLabel  = ld.Label |> Option.map (fun lab -> lab, la);
                PSize   = 4u;
                PCond   = cond
            }

        let opcodesDP3 =
            Map.ofList [
                "ADD", ADD;
                "ADC", ADC;
                "SUB", SUB;
                "SBC", SBC;
                "RSB", RSB;
                "RSC", RSC;
                "AND", AND;
                "ORR", ORR;
                "EOR", EOR;
                "BIC", BIC;
            ]

        let opcodesDP2 =
            Map.ofList [
                "CMP", CMP;
                "CMN", CMN;
                "TST", TST;
                "TEQ", TEQ;
            ]
        
        /// A function to initiate parsing of an instruction, and return a result
        ///  based on this parsing.
        let parse' (_instrC, (root, suffix, cond)) =
            let suff = match suffix with "S" -> Some S | _ -> None
            let instr =
                match Map.containsKey root opcodesDP2, suff with
                | true, None ->
                    let opcode = opcodesDP2.[root]
                    operandsDP2.Force()
                    // The commened-out code below will be required for DP2S instructions such as MOV
                    // I have kept it here to allow for faster integration with group
                    // |> Result.map (consDP2S suff)
                    |> Result.map (opcode) 
                    |> Result.map (DP2)
                | true, Some _suff' ->
                    "This instruction cannot have a suffix."
                    |> ``Invalid suffix``
                    |> Error
                | false, _ ->
                    let opcode = opcodesDP3.[root]
                    operandsDP3.Force()
                    |> Result.map (consDP3S suff)
                    |> Result.map (opcode) 
                    |> Result.map (DP3S)
            Result.map(makeInstr cond) instr
                    
        Map.tryFind ld.OpCode opCodes
        |> Option.map parse' 

    let (|IMatch|_|) = parse