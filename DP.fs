//////////////////////////////////////////////////////////////////////////////////////////
//                   Sample (skeleton) instruction implementation modules
//////////////////////////////////////////////////////////////////////////////////////////

module DP
    open CommonData
    open CommonLex

    type RotAmount =
        | RotAmt0 = 0   | RotAmt2 = 2   | RotAmt4 = 4   | RotAmt6 = 6 
        | RotAmt8 = 8   | RotAmt10 = 10 | RotAmt12 = 12 | RotAmt14 = 14 
        | RotAmt16 = 16 | RotAmt18 = 18 | RotAmt20 = 20 | RotAmt22 = 22 
        | RotAmt24 = 24 | RotAmt26 = 26 | RotAmt28 = 28 | RotAmt30 = 30

    [<Struct>]
    type LiteralValue = {value: byte; rot: RotAmount}

    /// ***Rs*** is the register containing the shift value for register-controlled shifts. 
    /// Rm must be in the range r0-r7.
    [<Struct>]
    type RegShift = {rs: RName}
    /// ***Rm*** is the source register for immediate shifts. 
    /// Rm must be in the range r0-r7.
    /// ***expr*** is the immediate shift value. It is an expression evaluating (at assembly time) to an integer in the range:
    /// note: 0-31 if op is LSL
    ///       1-32 otherwise. 
    [<Struct>]
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
        | ASR of InstrShift

    /// parse error (dummy, but will do)
    type ErrInstr = string

    /// sample specification for set of instructions
    /// very incomplete!
    let dPSpec = {
        InstrC = DP
        Roots = ["LSL";"LSR";"ASR";"RRX";"ROR"]
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

        let (|ParseRegex|_|) regex str =
           let m = Regex(regex).Match(str)
           if m.Success
           then Some (List.tail [ for x in m.Groups -> x.Value ])
           else None

        // this does the real work of parsing
        let parseShift suffix pCond : Result<Parse<Instr>,string> = 
            // test here
            let test : InstrShift = {rd = R1; shifter = (Reg {rs = R2})}
            let test2 : InstrShift = {rd = R1; shifter = (RegExp {rm = R2; exp = 8u})}
            Ok { 

                PInstr = LSL test
                PLabel = ls.Label |> Option.map (fun lab -> lab, la) ; 
                PSize = 4u; 
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
