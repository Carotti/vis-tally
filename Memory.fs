module Memory
    open CommonData
    open CommonLex
    open Expecto
    open Helpers
    open System.Text.RegularExpressions
    open FsCheck
    open System.Reflection.PortableExecutable
    open DP

    type OffsetType =
        | ImmOffset of uint32
        | RegOffset of RName
        | NoOffset
    
    [<Struct>]
    type Address = {addrReg: RName; offset: OffsetType}
    
    type PostIndex =
        | N of uint32
        | NoPostIndex

    [<Struct>]
    type InstrMemSingle = {Rn: RName; addr: Address; postOffset: PostIndex}
    
    type RegisterList = | RegList of List<RName>
    [<Struct>]
    type InstrMemMult = {Rn: RName; rList: RegisterList}

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
                Rn = regNames.[reg]; 
                addr = {addrReg = regNames.[mem]; offset = preoffset};
                postOffset = postoffset
            })
    
    let consMemMult reg rLst =
        Result.map (fun a ->
            {
                Rn = regNames.[reg];
                rList = RegList (List.map (fun a -> regNames.[a]) rLst)
            })

    let execute (cpuData: DataPath<'INS>) (instr: Parse<Instr>) =
        let PC = cpuData.Regs.[R15]
        let nextPC = PC + 4u
        let regContents r = cpuData.Regs.[r] // add 0 - 255
        let memContents = cpuData.MM

        let (|Valid|_|) (input: uint32) = 
            if input % 4u = 0u 
            then Valid |> Some
            else None
        
        let getOffsetType o =
            match o with
            | ImmOffset i -> i
            | RegOffset r -> regContents r
            | NoOffset -> 0u
        
        let getPostIndex i =
            match i with 
            | N num -> num
            | NoPostIndex -> 0u
        
        let wordAddress (a: uint32) = 
            match a with
            | Valid -> WA a
            | _ -> failwithf "Nope"

        let afterInstr = 
            match instr.PInstr with
            | LDR operands ->
                let memloc = memContents.[wordAddress ((regContents operands.addr.addrReg) + getOffsetType operands.addr.offset)] 
                let data = 
                    match memloc with
                    | DataLoc dl -> dl
                    | _ -> failwithf "You fucked it"
                let update = setReg operands.Rn data cpuData
                setReg operands.addr.addrReg (getPostIndex operands.postOffset) update
            | _ -> failwithf "Aint an instruction bro"

        setReg R15 nextPC afterInstr

    let parse (ls: LineData) : Result<Parse<Instr>,string> option =

        let (|MemMatch|_|) str =
            match str with 
            | ParseRegex "\[([rR][0-9]{1,2})\]" address -> address |> Some
            | ParseRegex "\[([rR][0-9]{1,2})" address -> address |> Some
            | _ -> None

        let (|RegListMatch|_|) str =
            match str with 
            | ParseRegex "([rR][0-9]{1,2})}" address -> address |> Some
            | ParseRegex "\[([rR][0-9]{1,2})" address -> address |> Some
            | _ -> None
        
        let (|OffsetMatch|_|) str =
            // let optionNBang b = 
            //     let postInd = N (uint32 b)
            //     let preInd = ImmOffset (uint32 b) // Somehow also construct postIndex with b
            //     preInd, postInd |> Some
            // // let optionRBang b = 
            // //     let postInd = N (uint32 b)
            // //     ImmOffset (uint32 b) |> Some // Somehow also construct postIndex with b
            // let optionN n = 
            //     let postInd = NoPostIndex
            //     let preInd = ImmOffset (uint32 n)

            // let optionR = function
            //     | a when (regValid a) -> RegOffset (regNames.[a]) |> Some
            //     | _ -> None

            let postNoPre n =
                let postInd = N (uint32 n)
                let preInd = NoOffset
                (preInd, postInd) |> Some
            
            let preNoPost n = 
                let postInd = NoPostIndex
                let preInd = ImmOffset (uint32 n)
                (preInd, postInd) |> Some
            
            let preAndPost n =
                let postInd = N (uint32 n)
                let preInd = ImmOffset (uint32 n)
                (preInd, postInd) |> Some

            match str with 
            | ParseRegex "([rR][0-9]{1,2})\]" preOffReg -> preOffReg |> optionR

            | ParseRegex "#(0[xX][0-9a-fA-F]+)" preOffHex -> preOffHex |> postNoPre
            | ParseRegex "#([0-9]+)" preOffDec -> preOffDec |> postNoPre
            | ParseRegex "#&([0-9a-fA-F]+)" preOffHex -> ("0x" + preOffHex) |> postNoPre
            | ParseRegex "#(0[bB][0-1]+)" preOffBin -> preOffBin |> postNoPre

            | ParseRegex "#(0[xX][0-9a-fA-F]+)\]" preOffHex -> preOffHex |> preNoPost
            | ParseRegex "#([0-9]+)\]" preOffDec -> preOffDec |> preNoPost
            | ParseRegex "#&([0-9a-fA-F]+)\]" preOffHex -> ("0x" + preOffHex) |> preNoPost
            | ParseRegex "#(0[bB][0-1]+)\]" preOffBin -> preOffBin |> preNoPost
            
            | ParseRegex "#(0[xX][0-9a-fA-F]+)\]!" preOffHex -> preOffHex |> preAndPost
            | ParseRegex "#([0-9]+)\]!" preOffDec -> preOffDec |> preAndPost
            | ParseRegex "#&([0-9a-fA-F]+)\]!" preOffHex -> ("0x" + preOffHex) |> preAndPost
            | ParseRegex "#(0[bB][0-1]+)\]!" preOffBin -> preOffBin |> preAndPost
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
                let optionNumToRegList n = 
                    match n with
                    | RegListExpand (low, high) -> 
                        let makeReg = (string >> (+) "R")
                        let fullRegList = List.map (fun r -> r |> makeReg) [int low..int high]
                        fullRegList |> Some
                    | _ -> None
                
                let optionMakeList n = 
                    [n] |> Some

                match str with
                | ParseRegex "(([rR][0-9]{1,2})-([rR][0-9]{1,2}))" listReg -> optionNumToRegList listReg
                | ParseRegex "([rR][0-9]{1,2})!" bangReg -> bangReg |> optionMakeList
                | ParseRegex "([rR][0-9]{1,2})" reg -> reg |> optionMakeList
                | _ -> None

            let splitMult = splitAny ls.Operands '{'

            let ops = 
                match splitMult with
                | [rn; rlst] ->
                    let regList = splitAny (rlst.Replace("}", "")) ','
                    let reg = rn.Replace(",", "")

                    let matcher = function
                        | RegListMatch x -> x 
                        | _ -> []

                    let rec applyToAll f list =
                        match list with
                        | [] -> []
                        | head :: tail -> f head :: applyToAll f tail

                    let matchedRegs = reg :: regList |> applyToAll matcher |> List.concat
                    match matchedRegs with
                    | head :: tail when (regsValid (head :: tail)) ->
                        (Ok splitMult)
                        |> consMemMult head tail
                    | _ -> Error "Registers probably not valid"
                | _ -> Error "Input not in correct form"

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
                            (Ok splitOps)
                            |> consMemSingle reg addr NoOffset NoPostIndex
                        | _ -> Error "Balls"
                    | _ -> Error "Bollocks"
                | [reg; addr; offset] ->
                    match addr with
                    | MemMatch addr ->
                        match [reg; addr] with
                        | [reg; addr] when (checkValid2 [reg; addr]) ->
                            match offset with
                            | OffsetMatch tuple  -> 
                                (Ok splitOps)
                                |> consMemSingle reg addr (fst tuple) (snd tuple)
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
            match root with
            | "LDR" -> parseSingle root suffix pCond
            | "STR" -> parseSingle root suffix pCond
            | "LDM" -> parseMult root suffix pCond
            | "STM" -> parseMult root suffix pCond
            | _ -> failwithf "We appear to have a rogue root"
           

        Map.tryFind ls.OpCode opCodes
        |> Option.map parse'

    /// Parse Active Pattern used by top-level code
    let (|IMatch|_|)  = parse


