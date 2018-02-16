module DP

    open CommonData
    open CommonLex
    open System.Text.RegularExpressions
    open Expecto

    let qp item = printfn "%A" item
    let qpl lst = List.map (qp) lst

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
           let m = Regex("^" + regex + "[\\s]*" + "$").Match(str)
           if m.Success
           then Some (m.Groups.[1].Value)
           else None

        let (|Op2Match|_|) str =
            let optionN n = N (uint32 n) |> Some
            let optionRs r = 
                match r with
                | a when (Map.containsKey a regNames) -> Rs (regNames.[a]) |> Some
                | _ -> None
            match str with 
            | ParseRegex "#(0[xX][0-9a-fA-F]+)" hex -> hex |> optionN
            | ParseRegex "#&([0-9a-fA-F]+)" hex -> ("0x" + hex) |> optionN
            | ParseRegex "#(0[bB][0-1]+)" bin -> bin |> optionN
            | ParseRegex "#([0-9]+)" dec -> dec |> optionN
            | ParseRegex "([rR][0-9]+)" reg -> reg |> optionRs
            | _ -> None // Literal was not valid
        
        // this does the real work of parsing
        let parseShift suffix pCond : Result<Parse<Instr>,string> = 
            let regsExist rLst = 
                rLst 
                |> List.fold (fun b r -> b && (Map.containsKey r regNames)) true
            
            let checkValid opList =
                match opList with
                | [dest; op1; _] when (regsExist [dest; op1]) -> true // ASR, LSL, LSR ROR
                | [dest; op1] when (regsExist [dest; op1]) -> true // RRX
                | _ -> false

            let splitOps =                          
                let nospace = ls.Operands.Replace(" ", "")                                    
                nospace.Split([|','|])              
                |> Array.map (fun r -> r.ToUpper())    
                |> List.ofArray

            let ops =
                match splitOps with
                | [dest; op1] when (checkValid splitOps) ->
                    Result.map (fun x -> 
                        {
                            Rd = regNames.[dest];
                            Rm = regNames.[op1];
                            shifter = Empty
                        }) (Ok Empty) // RRX
                | [dest; op1; op2] when (checkValid splitOps) ->
                    match op2 with
                    | Op2Match regOrNum -> 
                        Result.map (fun regOrNum -> 
                            {
                                Rd = regNames.[dest]; 
                                Rm = regNames.[op1]; 
                                shifter = regOrNum
                            }) (Ok regOrNum) // ASR, LSL, LSR ROR
                    | _ -> Error "Did not match"
                | _ ->
                    qp splitOps
                    Error "Split bollocked"

            let makeLSL ops = 
                Ok { 
                    PInstr = LSL ops
                    PLabel = ls.Label |> Option.map (fun lab -> lab, la) ; 
                    PSize = 4u; 
                    PCond = pCond 
                }
            let makeASR ops = 
                Ok { 
                    PInstr = LSL ops
                    PLabel = ls.Label |> Option.map (fun lab -> lab, la) ; 
                    PSize = 4u; 
                    PCond = pCond 
                }
            let makeRRX ops = 
                Ok { 
                    PInstr = RRX ops
                    PLabel = ls.Label |> Option.map (fun lab -> lab, la) ; 
                    PSize = 4u; 
                    PCond = pCond 
                }

            Result.bind makeRRX ops
        let listOfInstr = 
            Map.ofList [
                "LSL", parseShift;
                "LSR", parseShift;
                "ASR", parseShift;
                "ROR", parseShift;
                "RRX", parseShift;
            ]
        let parse' (_instrC, (root,suffix,pCond)) =
           listOfInstr.[root] suffix pCond

        Map.tryFind ls.OpCode opCodes // lookup opcode to see if it is known
        |> Option.map parse' // if unknown keep none, if known parse it.


    /// Parse Active Pattern used by top-level code
    let (|IMatch|_|) = parse

    // [<Tests>]
    // let shiftPropertyTests =
    //     let parseCheck inp ans 
