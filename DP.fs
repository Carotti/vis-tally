//////////////////////////////////////////////////////////////////////////////////////////
//                   Sample (skeleton) instruction implementation modules
//////////////////////////////////////////////////////////////////////////////////////////

module DP
    open CommonData
    open CommonLex
    open Helpers
    // open Execution
    open Expecto

    type ShiftType = 
        | Rs of RName
        | N of uint32

    type Suffix = 
        | S

    [<Struct>]
    type InstrShift =  {Rd: RName; Rm: RName; shifter: Option<ShiftType>; suff: Option<Suffix>}

    type Instr = 
        | LSL of InstrShift // 0-31
        | LSR of InstrShift // 1-32
        | ASR of InstrShift // 1-32
        | ROR of InstrShift // 1-31
        | RRX of InstrShift
        | MOV of InstrShift

    type ErrInstr = string

    let constructShift rd rm sh sf = 
        Result.map (fun _ -> 
            {
                Rd = regNames.[rd];
                Rm = regNames.[rm];
                shifter = sh;
                suff = sf;
            })

    /// sample specification for set of instructions
    /// very incomplete!
    let dPSpec = {
        InstrC = DP
        Roots = ["LSL";"LSR";"ASR";"ROR";"RRX";"MOV"]
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
        ]

    let execute (cpuData: DataPath<'INS>) (instr: Parse<Instr>) =
        let rotate reg amt = 
            let binaryMask = uint32 (2.0 ** (float amt) - 1.0)
            let lsbs = reg &&& binaryMask
            let msbs = lsbs <<< (32 - amt)
            let shiftedNum = reg >>> amt
            msbs ||| shiftedNum

        let pc = cpuData.Regs.[R15]
        let pcNext = pc + 4u
        let regContents r = cpuData.Regs.[r] // add 0 - 255

        let getShifter sh = 
            match sh with
            | Some (Rs reg) -> regContents reg |> int32
            | Some (N num) -> num |> int32
            | None -> 0

        let afterInstr = 
            match instr.PInstr with
            | LSL operands -> 
                let value = (regContents operands.Rm) <<< (getShifter operands.shifter)
                setReg operands.Rd value cpuData
            | ASR operands -> 
                let value = ((regContents operands.Rm) |> int32) >>> (getShifter operands.shifter) |> uint32
                setReg operands.Rd value cpuData
            | LSR operands -> 
                let value = (regContents operands.Rm) >>> (getShifter operands.shifter)
                setReg operands.Rd value cpuData
            | ROR operands -> 
                let value = rotate (regContents operands.Rm) (getShifter operands.shifter)
                setReg operands.Rd value cpuData
            | RRX operands when cpuData.Fl.C -> 
                // LSB needs to be put into C
                let value = (regContents operands.Rm) >>> 1 |> (|||) (uint32 0x80000000)
                setReg operands.Rd value cpuData
            | RRX operands -> 
                // LSB needs to be put into C
                let value = (regContents operands.Rm) >>> 1
                setReg operands.Rd value cpuData

        setReg R15 pcNext afterInstr


   
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

            let checkSuffix suff =
                match suff with 
                | "S" -> Some S
                | "" -> None
                | _ -> failwithf "Should never happen, not a suffix"

            let ops =
                match splitOps with
                | [dest; op1] when (checkValid2 splitOps) ->
                    (Ok splitOps) |> constructShift dest op1 None (checkSuffix suffix) // RRX, MOV
                | [dest; op1; op2] when (checkValid2 splitOps) ->
                    match op2 with
                    | Op2Match regOrNum -> 
                        (Ok splitOps) |> constructShift dest op1 (Some regOrNum) (checkSuffix suffix)// ASR, LSL, LSR ROR
                    | _ -> Error "Op2Match failed"
                | _ -> Error "splitOps did not match with \'op1, op2\' or \'op1, op2, op3\'"
            

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

