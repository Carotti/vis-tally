module Memory
    open CommonData
    open CommonLex
    open Expecto
    open Helpers
    open System.Text.RegularExpressions

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
    type InstrMemSingle = {valReg: RName; addr: Address; postOffset: PostIndex}
    
    type RegisterList = | RegList of List<RName>
    [<Struct>]
    type InstrMemMult = {rn: RName; rList: RegisterList}

    type Instr = 
        | LDR of InstrMemSingle
        | STR of InstrMemSingle
        | LDM of InstrMemMult
        | STM of InstrMemMult

    /// parse error (dummy, but will do)
    type ErrInstr = string

    let memSpec = {
        InstrC = MEM
        Roots = ["LDR";"STR";"STM";"LDM"]
        Suffixes = [""; "B";"IA";"IB";"DA";"DB"]
    }

    let memTypeSingleMap = 
        Map.ofList [
            "LDR", LDR;
            "STR", STR;
        ]
    let memTypeMultMap =
        Map.ofList [
            "LDM", LDM;
            "STM", STM;
        ]

    /// map of all possible opcodes recognised
    let opCodes = opCodeExpand memSpec

    let consMemSingle reg mem preoffset postoffset = 
        Result.map (fun a -> 
            {
                valReg = regNames.[reg]; 
                addr = {addrReg = regNames.[mem]; offset = preoffset};
                postOffset = postoffset
            })
    
    let consMemMult reg rLst =
        Result.map (fun a ->
            {
                rn = regNames.[reg];
                rList = RegList (List.map (fun a -> regNames.[a]) rLst)
            })

    let parse (ls: LineData) : Result<Parse<Instr>,string> option =

        let (|MemMatch|_|) str =
            match str with 
            | ParseRegex "\[([rR][0-9]{1,2})\]" address -> address |> Some
            | ParseRegex "\[([rR][0-9]{1,2})" address -> address |> Some
            | _ -> "mem fail" |> Some

        let (|RegListMatch|_|) str =
            match str with 
            | ParseRegex "([rR][0-9]{1,2})}" address -> address |> Some
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
            | _ -> None
        
        let parseMult (root: string) suffix pCond : Result<Parse<Instr>,string> =

            let (|ParseRegex2|_|) (regex: string) (str: string) =
               let m = Regex("^" + regex + "[\\s]*" + "$").Match(str)
               if m.Success
               then Some (m.Groups.[1].Value, m.Groups.[2].Value)
               else None

            let (|RegListExpand|_|) str =
                match str with
                | ParseRegex2 "[rR]([0-9]{1,2})-[rR]([0-9]{1,2})" (low, high) -> (low, high) |> Some
                | _ -> None

            let (|RegListMatch|_|) str =
                let createList n = 
                    match n with
                    | RegListExpand (low, high) -> 
                        let makeReg = (string >> (+) "R")
                        let ilow = int low
                        let ihigh = int high
                        let fullRegList = List.map (fun r -> r |> makeReg) [ilow..ihigh]
                        fullRegList |> Some
                    | _ -> None
                
                match str with
                | ParseRegex "(([rR][0-9]{1,2})-([rR][0-9]{1,2}))" listReg -> createList listReg
                | ParseRegex "([rR][0-9]{1,2})!" bangReg -> [bangReg] |> Some
                | ParseRegex "([rR][0-9]{1,2})" reg -> [reg] |> Some
                | _ -> None

            let splitMult = splitAny ls.Operands '{'

            let ops = 
                match splitMult with
                | [rn; rlst] ->
                    let splitList = splitAny (rlst.Replace("}", "")) ','
                    let firstReg = rn.Replace(",", "")
                    let matcher x =
                        match x with
                        | RegListMatch x -> x
                        | _ -> ["poop"]

                    let rec doAll f list =
                        match list with
                        | [] -> []
                        | head :: tail -> f head :: doAll f tail

                    let fullValues = doAll matcher splitList
                    let doneAH = List.concat fullValues
                    match firstReg :: doneAH with
                    | first :: rest when (regsValid (first :: rest)) ->
                        (Ok splitMult)
                        |> consMemMult first rest
                    | _ -> Error "shit"
                | _ -> Error "fuck"

            let make ops =
                Ok { 
                    PInstr= memTypeMultMap.[root] ops;
                    PLabel = None ; 
                    PSize = 4u; 
                    PCond = pCond 
                }
            Result.bind make ops

        let parseSingle (root: string) suffix pCond : Result<Parse<Instr>,string> =         

            let splitOps = splitAny ls.Operands ','
            
            let ops =
                match splitOps with
                | [reg; addr] ->
                    match addr with
                    | MemMatch addr -> 
                        match [reg; addr] with
                        | [reg; addr] when (checkValid2 [reg; addr]) ->
                            (Ok NoPost)
                            |> consMemSingle reg addr NoPre NoPost
                        | _ -> Error "Balls"
                    | _ -> Error "Bollocks"
                | [reg; addr; offset] ->
                    match addr with
                    | MemMatch addr ->
                        match [reg; addr] with
                        | [reg; addr] when (checkValid2 [reg; addr]) ->
                            match offset with
                            | OffsetMatch offset -> 
                                (Ok NoPost)
                                |> consMemSingle reg addr offset NoPost
                            | _ -> Error "Cobblers"
                        | _ -> Error "Goolies"
                    | _ -> Error "Gonads"
                | _ -> Error "Split bollocked"

            let make ops =
                Ok { 
                    PInstr= memTypeSingleMap.[root] ops;
                    PLabel = None ; 
                    PSize = 4u; 
                    PCond = pCond 
                }
            Result.bind make ops

        let parse' (_instrC, (root,suffix,pCond)) =
            parseMult root suffix pCond

        Map.tryFind ls.OpCode opCodes
        |> Option.map parse'

    /// Parse Active Pattern used by top-level code
    let (|IMatch|_|)  = parse


