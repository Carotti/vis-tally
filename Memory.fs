module Memory
    open CommonData
    open CommonLex
    open Expecto
    open System.Text.RegularExpressions

    let qp item = printfn "%A" item
    let qpl lst = List.map (qp) lst

    type OffsetType =
        | ImmOffset of uint32
        | RegOffset of RName
        | NoPre
    
    [<Struct>]
    type Address = {addrReg: RName; offset: OffsetType}
    
    type PostIndex =
        | N of uint32
        | NoPost

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

    let memTypeMap = 
        Map.ofList [
            "LDR", LDR
        ]

    /// map of all possible opcodes recognised
    let opCodes = opCodeExpand memSpec

    let constructMem reg mem preoffset postoffset = 
        Result.map (fun a -> 
            {
                valReg = regNames.[reg]; 
                addr = {addrReg = regNames.[mem]; offset = preoffset};
                postOffset = postoffset
            })

    let regValid r =
        Map.containsKey r regNames

    let regsValid rLst = 
        rLst 
        |> List.fold (fun b r -> b && (regValid r)) true

    let parse (ls: LineData) : Result<Parse<Instr>,string> option =

        let (|ParseRegex|_|) regex str =
           let m = Regex("^" + regex + "[\\s]*" + "$").Match(str)
           if m.Success
           then Some (m.Groups.[1].Value)
           else None

        let (|MemMatch|_|) str =
            // let optionAddr m = 
            //     match m with
            //     | a when (regValid a) -> RegOffset (regNames.[a]) |> Some
            //     | _ -> None
            match str with 
            | ParseRegex "\[([rR][0-9]{1,2})\]" address -> address |> Some
            | ParseRegex "\[([rR][0-9]{1,2})" address -> address |> Some
            | _ -> "mem fail" |> Some
        
        let (|OffsetMatch|_|) str =
            let optionBang b = 
                ImmOffset (uint32 b) |> Some // Somehow also construct postIndex with b
            let optionN n = 
                ImmOffset (uint32 n) |> Some
            let optionR = function
                | a when (regValid a) -> RegOffset (regNames.[a]) |> Some
                | _ -> None
            match str with 
            | ParseRegex "([rR][0-9]{1,2})\]" preOffReg -> preOffReg |> optionR
            | ParseRegex "#(0[xX][0-9a-fA-F]+)\]" preOffHex -> preOffHex |> optionN
            | ParseRegex "#([0-9]+)\]" preOffDec -> preOffDec |> optionN
            | ParseRegex "#&([0-9a-fA-F]+)\]" preOffHex -> ("0x" + preOffHex) |> optionN
            | ParseRegex "#(0[bB][0-1]+)\]" preOffBin -> preOffBin |> optionN
            | _ -> 
                qp "offset match fail"
                None

        let parseLoad opcode suffix pCond : Result<Parse<Instr>,string> = 
            
            let checkValid opList =
                match opList with
                | [dest; op1; _] when (regsValid [dest; op1]) -> true // e.g. LDR R0, [R1], #4
                | [dest; op1] when (regsValid [dest; op1]) -> true // e.g. LDR R0, [R1]
                | _ -> false

            let splitOps =
                let nospace = ls.Operands.Replace(" ", "")                                    
                nospace.Split([|','|])              
                |> Array.map (fun r -> r.ToUpper())    
                |> List.ofArray

            let ops =
                match splitOps with
                | [dest; op1] ->
                    match op1 with
                    | MemMatch mem -> 
                        match [dest; mem] with
                        | [dest; mem] when (checkValid [dest; mem]) ->
                            (Ok NoPost)
                            |> constructMem dest mem NoPre NoPost
                        | _ -> Error "Balls"
                    | _ -> Error "Bollocks"
                | [dest; op1; op2] ->
                    match op1 with
                    | MemMatch mem ->
                        match [dest; mem] with
                        | [dest; mem] when (checkValid [dest; mem]) ->
                            match op2 with
                            | OffsetMatch op2 -> 
                                (Ok NoPost)
                                |> constructMem dest mem op2 NoPost
                            | _ -> Error "Cobblers"
                        | _ -> Error "Goolies"
                    | _ -> Error "Gonads"

                | _ -> 
                    qp splitOps
                    Error "Split bollocked"

            let make ops = 
                Ok { 
                    PInstr= memTypeMap.[opcode] ops;
                    PLabel = None ; 
                    PSize = 4u; 
                    PCond = pCond 
                }
            Result.bind make ops

    
        // let listOfInstr = 
        //     Map.ofList [
        //         "LDR", parseLoad;
        //     ]

        let parse' (_instrC, (root,suffix,pCond)) =
            parseLoad root suffix pCond

        Map.tryFind ls.OpCode opCodes
        |> Option.map parse'



    /// Parse Active Pattern used by top-level code
    let (|IMatch|_|)  = parse


