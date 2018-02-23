module Memory
    open CommonData
    open CommonLex
    open Helpers

    // *********** //
    // LDR AND STR //
    // *********** //

    /// either a number or a register
    type OffsetType =
        | ImmPre of uint32
        | RegPre of RName
    
    /// Address consisting of register for base
    /// and an pre index offset of OffsetType
    /// being either a register or number
    [<Struct>]
    type Address = {addrReg: RName; offset: Option<OffsetType>}
    
    /// post index
    /// either register or number
    /// ldr r0, [r1], PostIndex
    type PostIndex =
        | ImmPost of uint32
        | RegPost of RName
    
    /// Suffix of LDR and STR instructions
    type SingleSuffix = 
        | B

    /// Single Store/Load memory instruction. LDR, LDRB, STR, STRB
    /// op{type}{cond} Rt, [Rn {, #offset}]        ; immediate offset
    /// op{type}{cond} Rt, [Rn, #offset]!          ; pre-indexed
    /// op{type}{cond} Rt, [Rn], #offset           ; post-indexed
    [<Struct>]
    type InstrMemSingle = {Rn: RName; addr: Address; postOffset: Option<PostIndex>; suff: Option<SingleSuffix>}
    
    // *********** //
    // LDM AND STM //
    // *********** //

    /// {....} list for LDM and STM
    /// either LDM r0, {r1, r2, r3}
    /// or     LDM r0, {r1 - r3}
    type RegisterList = | RegList of List<RName>

    /// Suffixes for LDM and STM
    type MultSuffix = 
        | IA | IB | DA | DB
        | FD | ED | FA | EA

    /// Multiple Store/Load memory instruction. LDM, STM
    /// op{addr_mode}{cond} Rn{!}, reglist
    [<Struct>]
    type InstrMemMult = {Rn: RName; rList: RegisterList; suff: Option<MultSuffix>}

    type MemInstr = 
        | LDR of InstrMemSingle
        | STR of InstrMemSingle
        | LDM of InstrMemMult
        | STM of InstrMemMult

    type Instr = 
        | Mem of MemInstr

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

    /// Contructs an Instruction of InstrMemSingle for LDR, STR
    let consMemSingle reg mem preoffset postoffset suffix = 
        Result.map (fun _ -> 
            {
                Rn = regNames.[reg]; 
                addr = {addrReg = regNames.[mem]; offset = preoffset};
                postOffset = postoffset;
                suff = suffix;
            })
    
    /// Contructs an Instruction of InstrMemMult for LDM, STM
    let consMemMult reg rLst suffix =
        Result.map (fun _ ->
            {
                Rn = regNames.[reg];
                rList = RegList (List.map (fun a -> regNames.[a]) rLst);
                suff = suffix;
            })

<<<<<<< HEAD
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
=======
    /// Where everything happens
>>>>>>> Half way through readme
    let parse (ls: LineData) : Result<Parse<Instr>,string> option =

        /// Partial Active pattern for matching regexes
        /// Looking for something like [r1] or [r13
        /// For matching the address location 
        let (|MemMatch|_|) str =
            match str with 
            | ParseRegex "\[([rR][0-9]{1,2})\]" address -> address |> Some
            | ParseRegex "\[([rR][0-9]{1,2})" address -> address |> Some
            | _ -> None

        /// For matching the list of regs
        let (|RegListMatch|_|) str =
            match str with 
            | ParseRegex "([rR][0-9]{1,2})}" address -> address |> Some
            | ParseRegex "\[([rR][0-9]{1,2})" address -> address |> Some
            | _ -> None
        
        /// Partial active pattern for matching both pre and post indexes
        /// e.g. str r0, [r1], r2   str r0, [r1], #4
        /// e.g. str r0, [r1, r2]   str r0, [r1, #4]
        /// e.g. str r0, [r1, r2]!  str r0, [r1, #4]!
        let (|OffsetMatch|_|) str =

            /// Register Post Index, No Pre Index
            /// e.g. str r0, [r1], r2
            let regNoPrePost = function
                | r when (regValid r) -> 
                    let postInd = Some (RegPost (regNames.[r]))
                    let preInd = None
                    (preInd, postInd) |> Some
                | _ -> None

            /// Register Pre Index, No Post Index
            /// e.g. str r0, [r1, r2]
            let regPreNoPost = function
                | r when (regValid r) -> 
                    let postInd = None
                    let preInd = Some (RegPre (regNames.[r]))
                    (preInd, postInd) |> Some
                | _ -> None
            
            /// Register Post Index and Pre Index
            /// e.g. str r0, [r1, r2]!
            let regPreAndPost = function
                | r when (regValid r) -> 
                    let postInd = Some (RegPost (regNames.[r]))
                    let preInd = Some (RegPre (regNames.[r]))
                    (preInd, postInd) |> Some
                | _ -> None

            /// Immediate Post Index, No Pre Index
            /// e.g. str r0, [r1], #4
            let immNoPrePost n =
                let postInd = Some (ImmPost (uint32 n))
                let preInd = None
                (preInd, postInd) |> Some
            
            /// Immediate Pre Index, No Post Index
            /// e.g. str r0, [r1, #4]
            let immPreNoPost n = 
                let postInd = None
                let preInd = Some (ImmPre (uint32 n))
                (preInd, postInd) |> Some
            
            /// Immediate Pre and Post Index
            /// e.g. str r0, [r1, #4]!
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
        
        /// parse for LDM, STM
        let parseMult (root: string) suffix pCond : Result<Parse<Instr>,string> =

            /// Regex match the numbers in a hyphen list {r1 - r7}
            /// in order to construct full reg list.
            /// return the two numbers as low, high
            let (|RegListExpand|_|) str =
                match str with
                | ParseRegex2 "[rR]([0-9]{1,2})-[rR]([0-9]{1,2})" (low, high) -> (low, high) |> Some
                | _ -> None

            /// Matches the registers
            let (|RegListMatch|_|) str =
                /// nice function to make register names from the 
                /// high and low values
                /// {r2-r7} -> 2, 7 -> R2,R3,R4,R5,R6,R7
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

            /// split the operands at a {
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
                | _ -> failwithf "Should never happen, not a suffix for LDM. Probably put a B"

            let ops = 
                match splitMult with
                | [rn; rlst] -> // LDM, STM
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

            /// split operands at ','
            let splitOps = splitAny ls.Operands ','

            let checkSingleSuffix = function
                | "B" -> Some B
                | "" -> None
                | _ -> failwithf "Should never happen, not a suffix"
            
            let ops =
                match splitOps with
                | [reg; addr] -> // str r0, [r1] or str r0, [r1, #4]
                    match addr with
                    | MemMatch addr -> 
                        match [reg; addr] with
                        | [reg; addr] when (regsValid [reg; addr]) ->
                            (Ok splitOps)
                            |> consMemSingle reg addr None None (checkSingleSuffix suffix)
                        | _ -> Error "Some registers are probably not valid"
                    | _ -> Error "MemMatch failed"
                | [reg; addr; offset] -> // str r0, [r1], #4
                    match addr with
                    | MemMatch addr ->
                        match [reg; addr] with
                        | [reg; addr] when (regsValid [reg; addr]) ->
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


