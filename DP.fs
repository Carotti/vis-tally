//////////////////////////////////////////////////////////////////////////////////////////
//                   Sample (skeleton) instruction implementation modules
//////////////////////////////////////////////////////////////////////////////////////////

module DP
    open CommonData
    open CommonLex
    open System.Text.RegularExpressions

    type RotAmount =
        | RotAmt0 = 0   | RotAmt2 = 2   | RotAmt4 = 4   | RotAmt6 = 6 
        | RotAmt8 = 8   | RotAmt10 = 10 | RotAmt12 = 12 | RotAmt14 = 14 
        | RotAmt16 = 16 | RotAmt18 = 18 | RotAmt20 = 20 | RotAmt22 = 22 
        | RotAmt24 = 24 | RotAmt26 = 26 | RotAmt28 = 28 | RotAmt30 = 30

    [<Struct>]
    type LiteralValue = {value: byte; rot: RotAmount}

    type ShiftType = 
        | Rs of RName
        | N of uint32
        | Empty       // None type here for RRX

    /// op{S}{cond} Rd, Rm, Rs
    /// op{S}{cond} Rd, Rm, #N
    /// RRX{S}{cond} Rd, Rm
    type InstrShift =  {Rd: RName; Rm: RName; shifter: ShiftType}

    type Instr = 
        | LSL of InstrShift // 0-31
        | LSR of InstrShift // 1-32
        | ASR of InstrShift // 1-32
        | ROR of InstrShift // 1-31
        | RRX of InstrShift

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

    let parse (ls: LineData) : Result<Parse<Instr>,string> option =
        let (WA la) = ls.LoadAddr // address this instruction is loaded into memory
        
        let (|ParseRegex|_|) regex str =
           let m = Regex(regex).Match(str)
           if m.Success
           then Some (m.Groups.[1].Value)
           else None

        let (|LiteralMatch|_|) str =
            let uintOption = uint32 >> Some
            match str with 
            | ParseRegex "^#(0[xX][0-9a-fA-F]+)$" hex -> hex |> uintOption
            | ParseRegex "^#(0&[0-9a-fA-F]+)$" hex -> hex |> uintOption
            | ParseRegex "^#(0[bB][0-1]+)$" bin -> bin |> uintOption
            | ParseRegex "^#([0-9]+)$" dec -> dec |> uintOption
            | _ -> None
        
        // this does the real work of parsing
        let parseShift suffix pCond : Result<Parse<Instr>,string> = 
            // test here
            let regregreg : InstrShift = {Rd = R1; Rm = R2; shifter = (Rs R3)}
            let regregn : InstrShift = {Rd = R1; Rm = R2; shifter = (N 5u)}
            let regreg : InstrShift = {Rd = R1; Rm = R2; shifter = Empty}
            Ok { 
                PInstr = LSL regregreg
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
