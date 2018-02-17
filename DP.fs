//////////////////////////////////////////////////////////////////////////////////////////
//                   Sample (skeleton) instruction implementation modules
//////////////////////////////////////////////////////////////////////////////////////////

module DP
    open CommonData
    open CommonLex
    open Helpers
    open Expecto

    type ShiftType = 
        | Rs of RName
        | N of uint32
        | Empty       // None type here for RRX

    /// op{S}{cond} Rd, Rm, Rs
    /// op{S}{cond} Rd, Rm, #N
    /// RRX{S}{cond} Rd, Rm
    [<Struct>]
    type InstrShift =  {Rd: RName; Rm: RName; shifter: ShiftType}

    type Instr = 
        | LSL of InstrShift // 0-31
        | LSR of InstrShift // 1-32
        | ASR of InstrShift // 1-32
        | ROR of InstrShift // 1-31
        | RRX of InstrShift

    /// parse error (dummy, but will do)
    type ErrInstr = string

    let constructShift rd rm sh = 
        Result.map (fun x -> 
            {
                Rd = regNames.[rd];
                Rm = regNames.[rm];
                shifter = sh
            })

    /// sample specification for set of instructions
    /// very incomplete!
    let dPSpec = {
        InstrC = DP
        Roots = ["LSL";"LSR";"ASR";"ROR";"RRX"]
        Suffixes = ["";"S"]
    }

    let shiftTypeMap = 
        Map.ofList [
            "LSL", LSL;
            "LSR", LSR;
            "ASR", ASR;
            "ROR", ROR;
            "RRX", RRX
        ]

    let execute (cpuData: DataPath<'INS>) (instr: Parse<Instr>) =
        // let nextPC = cpuData.Regs[] + 4u
        qp instr
   
    /// map of all possible opcodes recognised
    let opCodes = opCodeExpand dPSpec
    let parse (ls: LineData) : Result<Parse<Instr>,string> option =
        let (WA la) = ls.LoadAddr // address this instruction is loaded into memory

        let (|Op2Match|_|) str =
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
        let parseShift root suffix pCond : Result<Parse<Instr>,string> = 

            let splitOps = splitAny ls.Operands ','

            let ops =
                match splitOps with
                | [dest; op1] when (checkValid2 splitOps) ->
                    (Ok splitOps) |> constructShift dest op1 Empty // RRX
                | [dest; op1; op2] when (checkValid2 splitOps) ->
                    match op2 with
                    | Op2Match regOrNum -> 
                        (Ok splitOps) |> constructShift dest op1 regOrNum // ASR, LSL, LSR ROR
                    | _ -> Error "Error - op2 did not match"
                | _ -> Error "Error - splitting operands"

            let make ops =
                Ok { 
                    PInstr = shiftTypeMap.[root] ops
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
    
    [<Tests>]
    let dataProcessingTests =
        
        testList "DP Tests" [
            
            ]

