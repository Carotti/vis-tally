module DP
    open CommonData
    open CommonLex
    open Helpers

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
    let FlexOpShifts =
        Map.ofList [
            "LSL", LSL;
            "LSR", LSR;
            "ASR", ASR;
            "ROR", ROR;     
        ]
    
    /// Flexible shift instruction format within the flexible second operand.
    type FlexShiftForm =
        {
            rOp2: RName;
            sInstr: SInstr;
            sOp: SOp;
        }
    
    /// Flexible second operand format.
    type FlexOp2 =
        | Lit       of Literal
        | Reg       of RName
        | Shift     of FlexShiftForm
        | RRX of RName
    
    /// Only S is suffix for shift instr
    type Suffix = 
        | S
    
    /// Can either be register or number
    type ShiftType = 
        | Rs of RName
        | N of uint32

    type FOps2Instr =
        {   
            suff: Option<Suffix>;
            rOp1: RName;
            fOp2: FlexOp2;     
        }
    
    type FOps3Instr = 
        {   
            suff: Option<Suffix>;
            rDest: RName;
            rOp1: RName;
            fOp2: FlexOp2; 
        }
    
    type Ops3Instr =
        {
            suff:Option<Suffix>;
            rDest: RName;
            rOp1: ShiftType;
        }


    /// For shifts and moves
    /// op{S}{cond} Rd, Rm, Rs
    /// op{S}{cond} Rd, Rm, #n
    /// op{S}{cond} Rd, Rm
    [<Struct>]
    type ShiftInstrType =  
        {Rd: RName; Op1: ShiftType; Op2: Option<ShiftType>; suff: Option<Suffix>}
    
    type ShiftInstr = 
        | LSL of ShiftInstrType // 0-31 not implemented as I believe visual does not?!?
        | LSR of ShiftInstrType // 1-32 not implemented as I believe visual does not?!?
        | ASR of ShiftInstrType // 1-32 not implemented as I believe visual does not?!?
        | ROR of ShiftInstrType // 1-31 not implemented as I believe visual does not?!?
        | RRX of ShiftInstrType
        | MOV of ShiftInstrType // limited functionality with no FlexOp2 as Chris was Flex man.
        | MVN of ShiftInstrType

    type Instr = 
        | Shift of ShiftInstr

    type ErrInstr = string

    /// Contructs an Instruction of type shift
    let constructShift rd op1 sh sf =
        Result.map (fun _ -> 
            {
                Rd = regNames.[rd];
                Op1 = op1;
                Op2 = sh;
                suff = sf;
            })

    let dPSpec = {
        InstrC = DP
        Roots = ["LSL";"LSR";"ASR";"ROR";"RRX";"MOV";"MVN";]
        Suffixes = ["";"S"]
    }

    let shiftTypeMap = 
        Map.ofList [
            "LSL", LSL;
            "LSR", LSR;
            "ASR", ASR;
            "ROR", ROR;
            "RRX", RRX;
            "MOV", MOV;
            "MVN", MVN;
        ]

          
    /// map of all possible opcodes recognised
    let opCodes = opCodeExpand dPSpec
    let parse (ls: LineData) : Result<Parse<Instr>,string> option =
        let (WA la) = ls.LoadAddr // address this instruction is loaded into memory

        /// Partial Active pattern for
        /// matching numbers and registers
        /// either hex, bin, dec or reg.
        /// Also contructs the desired type
        let (|LitMatch|_|) str =
            let optionN n = N (uint32 n) |> Some
            let optionRs = function
                | a when (regValid a) -> Rs (regNames.[a]) |> Some
                | _ -> None
            match str with 
            | ParseRegex "#(0[xX][0-9a-fA-F]+)" hex -> hex |> optionN
            | ParseRegex "#&([0-9a-fA-F]+)" hex -> ("0x" + hex) |> optionN
            | ParseRegex "#(0[bB][0-1]+)" bin -> bin |> optionN
            | ParseRegex "#([0-9]+)" dec -> dec |> optionN
            | ParseRegex "([rR][0-9]{1,2})" reg -> reg |> optionRs
            | _ -> None // Literal was not valid

        // this does the real work of parsing
        let parseShift root suffix pCond = 

            /// Split input at ','
            let splitOps = splitAny ls.Operands ','

            let checkSuffix suff =
                match suff with 
                | "S" -> Some S
                | "" -> None
                | _ -> failwithf "Should never happen, not a suffix"

            /// Operands for instruction
            let ops =
                match splitOps with
                | [dest; op1] when (regValid dest) -> // matches mov, mvn, rrx
                    match op1 with
                    | LitMatch regOrNum ->
                        (Ok splitOps) |> constructShift dest regOrNum None (checkSuffix suffix) // RRX, MOV
                    | _ -> Error "LitMatch failed"
                | [dest; op1; op2] when (checkValid2 splitOps) -> // matches lsl, lsr, asr, ror
                    match op1 with
                    | LitMatch regOrNum1 -> 
                        match op2 with
                        | LitMatch regOrNum2 ->
                            (Ok splitOps) |> constructShift dest regOrNum1 (Some regOrNum2) (checkSuffix suffix)// ASR, LSL, LSR ROR
                        | _ -> Error "LitMatch failed"
                    | _ -> Error "LitMatch failed"
                | _ -> Error "splitOps did not match with \'op1, op2\' or \'op1, op2, op3\'"
            
            let make ops =
                Ok { 
                    PInstr = shiftTypeMap.[root] ops |> Shift
                    PLabel = ls.Label |> Option.map (fun lab -> lab, la) ; 
                    PSize = 4u; 
                    PCond = pCond 
                }

            Result.bind make ops
            
        let parse' (_instrC, (root,suffix,pCond)) =
            parseShift root suffix pCond
           
        Map.tryFind ls.OpCode opCodes // lookup opcode to see if it is known
        |> Option.map parse' // if unknown keep none, if known parse it.

    /// Parse Active Pattern used by top-level code
    let (|IMatch|_|) = parse
