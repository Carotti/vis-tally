module Memory
    open CommonData
    open CommonLex
    open Expecto

    type OffsetType =
        | ImmOffset of uint32
        | Empty

    [<Struct>]
    type Address = {addrReg: RName; offset: OffsetType}
    
    type PostIndex =
        | N of uint32
        | Empty

    [<Struct>]
    type InstrMem = {valReg: RName; addr: Address; postOffset: PostIndex}

    type Instr = 
        | LDR of InstrMem

    /// parse error (dummy, but will do)
    type ErrInstr = string

    let memSpec = {
        InstrC = MEM
        Roots = ["LDR";"STR";"STM";"LDM"]
        Suffixes = [""; "B"]
    }

    /// map of all possible opcodes recognised
    let opCodes = opCodeExpand memSpec

    /// main function to parse a line of assembler
    /// ls contains the line input
    /// and other state needed to generate output
    /// the result is None if the opcode does not match
    /// otherwise it is Ok Parse or Error (parse error string)
    let parse (ls: LineData) : Result<Parse<Instr>,string> option =

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


        let parseLoad suffix pCond : Result<Parse<Instr>,string> = 

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

            let makeLDR ops = 
                Ok { 
                    PInstr= LDR (ops);
                    PLabel = None ; 
                    PSize = 4u; 
                    PCond = pCond 
                }
            Result.bind makeLDR ops

    
        let listOfInstr = 
            Map.ofList [
                "LDR", parseLoad;
            ]

        let parse' (instrC, (root,suffix,pCond)) =
            listOfInstr.[root] suffix pCond

        Map.tryFind ls.OpCode opCodes
        |> Option.map parse'



    /// Parse Active Pattern used by top-level code
    let (|IMatch|_|)  = parse


