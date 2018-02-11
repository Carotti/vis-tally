//////////////////////////////////////////////////////////////////////////////////////////
//                   Sample (skeleton) instruction implementation modules
//////////////////////////////////////////////////////////////////////////////////////////

module DP
    open CommonData
    open CommonLex

    /// ***Rs*** is the register containing the shift value for register-controlled shifts. 
    /// Rm must be in the range r0-r7.
    type RegShift = {rs: RName}
    /// ***Rm*** is the source register for immediate shifts. 
    /// Rm must be in the range r0-r7.
    /// ***expr*** is the immediate shift value. It is an expression evaluating (at assembly time) to an integer in the range:
    /// note: 0-31 if op is LSL
    ///       1-32 otherwise. 
    type RegExpShift = {rm: RName; exp: uint32}
    /// Both types of shift 
    /// op, rd, rs
    /// op, rd, rm, #expr
    type ShiftType = 
        | Reg of RegShift
        | RegExp of RegExpShift


    /// ***Rd*** is the destination register. It is also the source register for register-controlled shifts.
    /// Rd must be in the range r0-r7.
    /// ***Rs*** is the register containing the shift value for register-controlled shifts. 
    /// Rm must be in the range r0-r7.
    /// ***Rm*** is the source register for immediate shifts. 
    /// Rm must be in the range r0-r7.
    /// ***expr*** is the immediate shift value. It is an expression evaluating (at assembly time) to an integer in the range:
    /// note: 0-31 if op is LSL
    ///       1-32 otherwise. 
    type InstrShift =  {rd: RName; shifter: ShiftType}

    type Instr = 
        | LSL of InstrShift
        | LSR of InstrShift

    /// parse error (dummy, but will do)
    type ErrInstr = string

    /// sample specification for set of instructions
    /// very incomplete!
    let dPSpec = {
        InstrC = DP
        Roots = ["LSL";"LSR";"ASR";"ROR";"RRX"]
        Suffixes = ["";"S"]
    }

    /// map of all possible opcodes recognised
    let opCodes = opCodeExpand dPSpec

    /// main function to parse a line of assembler
    /// ls contains the line input
    /// and other state needed to generate output
    /// the result is None if the opcode does not match
    /// otherwise it is Ok Parse or Error (parse error string)
    let parse (ls: LineData) : Result<Parse<Instr>,string> option =
        let (WA la) = ls.LoadAddr // address this instruction is loaded into memory

        // this does the real work of parsing
        let parseShift suffix pCond : Result<Parse<Instr>,string> = 
            // test here
            let test : InstrShift = {rd = R1; shifter = (Reg {rs = R2})}
            let test2 : InstrShift = {rd = R1; shifter = (RegExp {rm = R2; exp = 8u})}
            Ok { 
                // Normal (non-error) return from result monad
                // This is the instruction determined from opcode, suffix and parsing
                // the operands. Not done in the sample.
                // Note the record type returned must be written by the module author.
                PInstr = LSL test


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
        let listOfInstr = 
            Map.ofList [
                "LSL", parseShift;
            ]
        let parse' (_instrC, (root,suffix,pCond)) =
           listOfInstr.[root] suffix pCond

        Map.tryFind ls.OpCode opCodes // lookup opcode to see if it is known
        |> Option.map parse' // if unknown keep none, if known parse it.


    /// Parse Active Pattern used by top-level code
    let (|IMatch|_|) = parse
