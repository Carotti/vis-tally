module Memory
    open CommonData
    open CommonLex
    open Expecto
    open Helpers
    // open Execution
    open System.Text.RegularExpressions
    open FsCheck
    open System

    type OffsetType =
        | ImmPre of uint32
        | RegPre of RName
    
    [<Struct>]
    type Address = {addrReg: RName; offset: Option<OffsetType>}
    
    type PostIndex =
        | ImmPost of uint32
        | RegPost of RName
    
    type SingleSuffix = 
        | B

    [<Struct>]
    type InstrMemSingle = {Rn: RName; addr: Address; postOffset: Option<PostIndex>; suff: Option<SingleSuffix>}
    
    type RegisterList = | RegList of List<RName>

    type MultSuffix = 
        | IA | IB | DA | DB
        | FD | ED | FA | EA

    [<Struct>]
    type InstrMemMult = {Rn: RName; rList: RegisterList; suff: Option<MultSuffix>}

    type MemInstr = 
        | LDR of InstrMemSingle
        | STR of InstrMemSingle
        | LDM of InstrMemMult
        | STM of InstrMemMult

    type Instr = 
        | Mem of MemInstr

    /// parse error (dummy, but will do)
    type ErrInstr = string

    let memSpec = {
        InstrC = MEM
        Roots = ["LDR";"STR";"STM";"LDM"]
        Suffixes = [""; "B";"IA";"IB";"DA";"DB";"FD";"ED";"FA";"EA"]
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

    let consMemSingle reg mem preoffset postoffset suffix = 
        Result.map (fun _ -> 
            {
                Rn = regNames.[reg]; 
                addr = {addrReg = regNames.[mem]; offset = preoffset};
                postOffset = postoffset;
                suff = suffix;
            })
    
    let consMemMult reg rLst suffix =
        Result.map (fun _ ->
            {
                Rn = regNames.[reg];
                rList = RegList (List.map (fun a -> regNames.[a]) rLst);
                suff = suffix;
            })

<<<<<<< HEAD
    let execute (cpuData: DataPath<'INS>) (instr: Parse<Instr>) =
        let pc = cpuData.Regs.[R15]
        let pcNext = pc + 4u
        let regContents r = cpuData.Regs.[r] // add 0 - 255
        let memContents = cpuData.MM

        let (|Valid|_|) (input: uint32) = 
            if input % 4u = 0u 
            then Valid |> Some
            else None
        
        let getOffsetType o =
            match o with
            | Some (ImmPre i) -> i
            | Some (RegPre r) -> regContents r
            | None -> 0u
        
        let getPostIndex i =
            match i with 
            | Some (ImmPost i) -> i
            | Some (RegPost r) -> regContents r
            | None -> 0u
        
        let wordAddress (a: uint32) = 
            match a with
            | Valid -> WA a
            | _ -> failwithf "Nope"
        
        
        let rec makeOffsetList inlst outlist incr start = 
            match inlst with
            | _ :: tail -> (start + incr) |> makeOffsetList tail (start :: outlist) incr
            | [] -> outlist

        let dataFn m =
            match m with 
            | DataLoc dl -> dl
            // | Code c -> c
            | _ -> failwith "Ah"

//         Restrictions
// In these instructions:
//     Rn must not be PC
//     reglist must not contain SP
//     in any STM instruction, reglist must not contain PC
//     in any LDM instruction, reglist must not contain PC if it contains LR
//     reglist must not contain Rn if you specify the writeback suffix.
// When PC is in reglist in an LDM instruction:
//     bit[0] of the value loaded to the PC must be 1 for correct execution, and a branch occurs to this halfword-aligned address
//     if the instruction is conditional, it must be the last instruction in the IT block.
        let afterInstr = 
            match instr.PInstr with
            | LDR operands ->
                let wordOrByte d = 
                    match operands.suff with
                    | Some B -> d &&& 0x000000FFu
                    | None -> d  
                let memloc = memContents.[wordAddress ((regContents operands.addr.addrReg) + getOffsetType operands.addr.offset)] 
                let value = wordOrByte (dataFn memloc)
                let update = setReg operands.Rn value cpuData
                setReg operands.addr.addrReg (getPostIndex operands.postOffset) update
            | STR operands ->
                let wordOrByte d = 
                    match operands.suff with
                    | Some B -> d &&& 0x000000FFu
                    | None -> d 
                let value = wordOrByte (regContents operands.Rn)
                let update = setMem (wordAddress ((regContents operands.addr.addrReg) + getOffsetType operands.addr.offset)) value cpuData
                setReg operands.addr.addrReg (getPostIndex operands.postOffset) update
            | LDM operands ->
                let rl =
                    match operands.rList with
                    | RegList rl -> rl
                let offsetList start = 
                    let lst =
                        match operands.suff with           
                        | Some IA -> 
                            start
                            |> makeOffsetList rl [] 4
                            |> List.rev
                        | Some IB -> 
                            (start + 4)
                            |> makeOffsetList rl [] 4
                            |> List.rev
                        | Some DA -> 
                            start
                            |> makeOffsetList rl [] -4
                        | Some DB ->
                            (start - 4) 
                            |> makeOffsetList rl [] -4
                        | Some FD ->
                            start
                            |> makeOffsetList rl [] 4
                            |> List.rev
                        | Some ED ->
                            (start + 4)
                            |> makeOffsetList rl [] 4
                            |> List.rev
                        | Some FA ->
                            start
                            |> makeOffsetList rl [] -4
                        | Some EA ->
                            (start - 4) 
                            |> makeOffsetList rl [] -4
                        | _ -> failwithf "Isn't a valid suffix"
                    List.map (fun el -> el |> uint32) lst

                let baseAddrInt = (regContents operands.Rn) |> int32
                let wordAddrList = List.map wordAddress (offsetList baseAddrInt)
                let memLocList = List.map (fun m -> memContents.[m]) wordAddrList
                let dataLocList = List.map dataFn memLocList
                setMultRegs rl dataLocList cpuData
            | STM operands ->
                let rl =
                    match operands.rList with
                    | RegList rl -> rl  

                let offsetList start = 
                    let lst =
                        match operands.suff with           
                        | Some IA -> 
                            start
                            |> makeOffsetList rl [] 4
                            |> List.rev
                        | Some IB -> 
                            (start + 4)
                            |> makeOffsetList rl [] 4
                            |> List.rev
                        | Some DA -> 
                            start
                            |> makeOffsetList rl [] -4
                        | Some DB ->
                            (start - 4) 
                            |> makeOffsetList rl [] -4
                        | Some EA ->
                            start
                            |> makeOffsetList rl [] 4
                            |> List.rev
                        | Some FA ->
                            (start + 4)
                            |> makeOffsetList rl [] 4
                            |> List.rev
                        | Some ED ->
                            start
                            |> makeOffsetList rl [] -4
                        | Some FD ->
                            (start - 4) 
                            |> makeOffsetList rl [] -4
                        | _ -> failwithf "Isn't a valid suffix"
                    List.map (fun el -> el |> uint32) lst

                let baseAddrInt = (regContents operands.Rn) |> int32
                let wordAddrList = List.map wordAddress (offsetList baseAddrInt)
                let regContentsList = List.map regContents rl
                setMultMem wordAddrList regContentsList cpuData

        setReg R15 pcNext afterInstr

=======
>>>>>>> Memory excution running but currently not doing anything lmao
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
            let regNoPrePost = function
                | r when (regValid r) -> 
                    let postInd = Some (RegPost (regNames.[r]))
                    let preInd = None
                    (preInd, postInd) |> Some
                | _ -> None
                
            let regPreNoPost = function
                | r when (regValid r) -> 
                    let postInd = None
                    let preInd = Some (RegPre (regNames.[r]))
                    (preInd, postInd) |> Some
                | _ -> None
            
            let regPreAndPost = function
                | r when (regValid r) -> 
                    let postInd = Some (RegPost (regNames.[r]))
                    let preInd = Some (RegPre (regNames.[r]))
                    (preInd, postInd) |> Some
                | _ -> None

            let immNoPrePost n =
                let postInd = Some (ImmPost (uint32 n))
                let preInd = None
                (preInd, postInd) |> Some
            
            let immPreNoPost n = 
                let postInd = None
                let preInd = Some (ImmPre (uint32 n))
                (preInd, postInd) |> Some
            
            let immPreAndPost n =
                let postInd = Some (ImmPost (uint32 n))
                let preInd = Some (ImmPre (uint32 n))
                (preInd, postInd) |> Some

            match str with 
            | ParseRegex "([rR][0-9]{1,2})" preOffReg -> preOffReg |> regNoPrePost
            | ParseRegex "([rR][0-9]{1,2})\]" preOffReg -> preOffReg |> regPreNoPost
            | ParseRegex "([rR][0-9]{1,2})\]!" preOffReg -> preOffReg |> regPreAndPost
            | ParseRegex "#(0[xX][0-9a-fA-F]+)" preOffHex -> preOffHex |> immNoPrePost
            | ParseRegex "#([0-9]+)" preOffDec -> preOffDec |> immNoPrePost
            | ParseRegex "#&([0-9a-fA-F]+)" preOffHex -> ("0x" + preOffHex) |> immNoPrePost
            | ParseRegex "#(0[bB][0-1]+)" preOffBin -> preOffBin |> immNoPrePost
            | ParseRegex "#(0[xX][0-9a-fA-F]+)\]" preOffHex -> preOffHex |> immPreNoPost
            | ParseRegex "#([0-9]+)\]" preOffDec -> preOffDec |> immPreNoPost
            | ParseRegex "#&([0-9a-fA-F]+)\]" preOffHex -> ("0x" + preOffHex) |> immPreNoPost
            | ParseRegex "#(0[bB][0-1]+)\]" preOffBin -> preOffBin |> immPreNoPost    
            | ParseRegex "#(0[xX][0-9a-fA-F]+)\]!" preOffHex -> preOffHex |> immPreAndPost
            | ParseRegex "#([0-9]+)\]!" preOffDec -> preOffDec |> immPreAndPost
            | ParseRegex "#&([0-9a-fA-F]+)\]!" preOffHex -> ("0x" + preOffHex) |> immPreAndPost
            | ParseRegex "#(0[bB][0-1]+)\]!" preOffBin -> preOffBin |> immPreAndPost
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
                        let fullRegList = List.map (fun r -> r |> makeRegFn) [int low..int high]
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
            
            let checkMultSuffix = function
                | "IA" -> Some IA
                | "IB" -> Some IB
                | "DA" -> Some DA
                | "DB" -> Some DB
                | "FD" -> Some FD
                | "ED" -> Some ED
                | "FA" -> Some FA
                | "EA" -> Some EA
                | "" -> None
                | _ -> failwithf "Should never happen, not a suffix"

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
                        |> consMemMult head tail (checkMultSuffix suffix)
                    | _ -> Error "Registers probably not valid"
                | _ -> Error "Input not in correct form"

            let make ops =
                Ok { 
                    PInstr= memTypeMultMap.[root] ops |> Mem;
                    PLabel = None ; 
                    PSize = 4u; 
                    PCond = pCond 
                }
            Result.bind make ops

        let parseSingle (root: string) suffix pCond : Result<Parse<Instr>,string> =         

            let splitOps = splitAny ls.Operands ','

            let checkSingleSuffix = function
                | "B" -> Some B
                | "" -> None
                | _ -> failwithf "Should never happen, not a suffix"
            
            let ops =
                match splitOps with
                | [reg; addr] ->
                    match addr with
                    | MemMatch addr -> 
                        match [reg; addr] with
                        | [reg; addr] when (checkValid2 [reg; addr]) ->
                            (Ok splitOps)
                            |> consMemSingle reg addr None None (checkSingleSuffix suffix)
                        | _ -> Error "Some registers are probably not valid"
                    | _ -> Error "MemMatch failed"
                | [reg; addr; offset] ->
                    match addr with
                    | MemMatch addr ->
                        match [reg; addr] with
                        | [reg; addr] when (checkValid2 [reg; addr]) ->
                            match offset with
                            | OffsetMatch tuple  -> 
                                (Ok splitOps)
                                |> consMemSingle reg addr (fst tuple) (snd tuple) (checkSingleSuffix suffix)
                            | _ -> Error "Cobblers"
                        | _ -> Error "Goolies"
                    | _ -> Error "Gonads"
                | _ -> Error "Split bollocked"

            let make ops =
                Ok { 
                    PInstr= memTypeSingleMap.[root] ops |> Mem;
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


