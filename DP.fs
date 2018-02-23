module DP

    open CommonData
    open CommonLex
    open Helpers
    // open Execution
    open Expecto
    open VisualTest


    type ShiftType = 
        | Rs of RName
        | N of uint32

    type Suffix = 
        | S

    [<Struct>]
    type ShiftInstrType =  {Rd: RName; Op1: ShiftType; Op2: Option<ShiftType>; suff: Option<Suffix>}
    
    type ShiftInstr = 
        | LSL of ShiftInstrType // 0-31
        | LSR of ShiftInstrType // 1-32
        | ASR of ShiftInstrType // 1-32
        | ROR of ShiftInstrType // 1-31
        | RRX of ShiftInstrType
        | MOV of ShiftInstrType
        | MVN of ShiftInstrType

    type Instr = 
        | Shift of ShiftInstr

    type ErrInstr = string


    let constructShift rd op1 sh sf =
        
        
        Result.map (fun _ -> 
            {
                Rd = regNames.[rd];
                Op1 = op1;
                Op2 = sh;
                suff = sf;
            })

    /// sample specification for set of instructions
    /// very incomplete!
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
<<<<<<< HEAD
<<<<<<< HEAD

    let execute (cpuData: DataPath<'INS>) (instr: Parse<Instr>) =
        let rotate reg amt = 
            let binaryMask = uint32 (2.0 ** (float amt) - 1.0)
            let lsbs = reg &&& binaryMask
            let msbs = lsbs <<< (32 - amt)
            let shiftedNum = reg >>> amt
            msbs ||| shiftedNum

        let PC = cpuData.Regs.[R15]
        let nextPC = PC + 4u
        let regContents r = cpuData.Regs.[r] // add 0 - 255
        let lessThan32 v = (fun x -> x % 32) v
        let lessThan31 v = (fun x -> x % 31) v

        let getRegOrNum sh = 
            match sh with
            | Some (Rs reg) -> regContents reg |> int32
            | Some (N num) -> num |> int32
            | None -> 0

        let afterInstr = 
            match instr.PInstr with
            | LSL operands -> 
                let value = (getRegOrNum (Some operands.Op1) |> uint32) <<< (getRegOrNum operands.shifter)
                setReg operands.Rd value cpuData
            | ASR operands -> 
                let value = (getRegOrNum (Some operands.Op1)) >>> (getRegOrNum operands.shifter) |> uint32
                setReg operands.Rd value cpuData
            | LSR operands -> 
                let value = (getRegOrNum (Some operands.Op1) |> uint32) >>> (getRegOrNum operands.shifter)
                setReg operands.Rd value cpuData
            | ROR operands -> 
                let value = rotate (getRegOrNum (Some operands.Op1) |> uint32) (getRegOrNum operands.shifter)
                setReg operands.Rd value cpuData
            | RRX operands when cpuData.Fl.C -> 
                // LSB needs to be put into C
                let value = (getRegOrNum (Some operands.Op1) |> uint32) >>> 1 |> (|||) (uint32 0x80000000)
                setReg operands.Rd value cpuData
            | RRX operands -> 
                // LSB needs to be put into C
                let value = (getRegOrNum (Some operands.Op1) |> uint32) >>> 1
                setReg operands.Rd value cpuData
            | MOV operands ->
                let value = (getRegOrNum (Some operands.Op1) |> uint32)
                setReg operands.Rd value cpuData
            | MVN operands ->
                let value = 0xFFFFFFFFu ^^^ (getRegOrNum (Some operands.Op1) |> uint32)
                setReg operands.Rd value cpuData
            | _ -> failwithf "Ain't an instruction bro"

        setReg R15 nextPC afterInstr
        


   
=======
  
>>>>>>> Refactored DP execution code
=======

          
>>>>>>> LMAO mov tests without flex op2 cos
    /// map of all possible opcodes recognised
    let opCodes = opCodeExpand dPSpec
    let parse (ls: LineData) : Result<Parse<Instr>,string> option =
        let (WA la) = ls.LoadAddr // address this instruction is loaded into memory

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

            let splitOps = splitAny ls.Operands ','

            let checkSuffix suff =
                match suff with 
                | "S" -> Some S
                | "" -> None
                | _ -> failwithf "Should never happen, not a suffix"

            let ops =
                match splitOps with
                | [dest; op1] when (regValid dest) ->
                    match op1 with
                    | LitMatch regOrNum ->
                        (Ok splitOps) |> constructShift dest regOrNum None (checkSuffix suffix) // RRX, MOV
                    | _ -> Error "LitMatch failed"
                | [dest; op1; op2] when (checkValid2 splitOps) ->
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

