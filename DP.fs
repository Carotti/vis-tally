//////////////////////////////////////////////////////////////////////////////////////////
//                   Sample (skeleton) instruction implementation modules
//////////////////////////////////////////////////////////////////////////////////////////

module DP
    open CommonData
    open CommonLex

    /// Enum type for all possible rotation values.
    /// TODO: Turn into type-safe DU + Map
    type RotVal =
        | Rot0 = 0      | Rot2 = 2      | Rot4 = 4      | Rot6 = 6
        | Rot8 = 8      | Rot10 = 10    | Rot12 = 12    | Rot14 = 14  
        | Rot16 = 16    | Rot18 = 18    | Rot20 = 20    | Rot22 = 22 
        | Rot24 = 24    | Rot26 = 26    | Rot28 = 28    | Rot30 = 30 

    /// Literal type for allowed literals.
    ///  `K` is the underlying byte.
    ///  `R` is the rotation that is applied to `K`
    type Literal =
        {
            K: byte;
            R: RotVal;
        }
        
    /// Shift operands for the shift instructions `SIns`.
    type SOp =
        | Const 
        | Regs 
   
    /// Shift instructions that require operands and are compatible with the
    ///  flexiable second operand. `RRX` is not included since it does not
    ///  require any further operands.
    type SInstr =
        | LSL
        | LSR
        | ASR
        | ROR

    type FelxOp2 =
        | Lit       of Literal
        | Reg       of RName
        | Shifted   of RName * SInstr * SOp
        | RRX       of RName 

    type Instr =
        Add of rDest:RName * rOp1:RName * op2:FelxOp2
       
    /// parse error (dummy, but will do)
    type ErrInstr = string

    /// **sample** specification for set of instructions
    /// very incomplete!
    let dPSpec = {
        InstrC = DP
        Roots = ["ADD";"SUB"]
        Suffixes = [""; "S"]
    }

    /// map of all possible opcodes recognised
    let opCodes = opCodeExpand dPSpec

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
                PInstr={DPDummy=()}; 


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
