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
    let parse (ls: LineData) : Result<Parse<Instr>,string> option =
        let parse' (instrC, (root,suffix,pCond)) =

            let (WA la) = ls.LoadAddr // address this instruction is loaded into memory
            // this does the real work of parsing
            // dummy return for now
            Ok { 
                // Normal (non-error) return from result monad
                // This is the instruction determined from opcode, suffix and parsing
                // the operands. Not done in the sample.
                // Note the record type returned must be written by the module author.
                PInstr={}


                // This is normally the line label as contained in
                // ls together with the label's value which is normally
                // ls.LoadAddr. Some type conversion is needed since the
                // label value is a number and not necessarily a word address
                // it does not have to be div by 4, though it usually is
                PLabel = ls.Label |> Option.map (fun lab -> lab, la) ; 


                // this is the number of bytes taken by the instruction
                // word loaded into memory. For arm instructions it is always 4 bytes. 
                // For data definition DCD etc it is variable.
                //  For EQU (which does not affect memory) it is 0
                PSize = 4u; 

                // the instruction condition is detected in the opcode and opCodeExpand                 
                // has already calculated condition already in the opcode map.
                // this part never changes
                PCond = pCond 
                }
        Map.tryFind ls.OpCode opCodes // lookup opcode to see if it is known
        |> Option.map parse' // if unknown keep none, if known parse it.


    /// Parse Active Pattern used by top-level code
    let (|IMatch|_|) = parse
