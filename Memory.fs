module Memory
    open CommonData
    open CommonLex
    open Helpers
    open Errors
    open ErrorMessages

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
    type Address = 
        {
            addrReg: RName; 
            offset: Option<OffsetType>;
        }
    
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
    type InstrMemSingle = 
        {
            Rn: RName;
            addr: Address;
            postOffset: Option<PostIndex>;
            suff: Option<SingleSuffix>
        }
    
    // *********** //
    // LDM AND STM //
    // *********** //

    /// Suffixes for LDM and STM
    type MultSuffix = 
        | IA | IB | DA | DB
        | FD | ED | FA | EA

    /// Multiple Store/Load memory instruction. LDM, STM
    /// op{addr_mode}{cond} Rn{!}, reglist
    [<Struct>]
    type InstrMemMult = {Rn: RName; rList: List<RName>; suff: Option<MultSuffix>}

    type MemInstr = 
        | LDR of InstrMemSingle
        | STR of InstrMemSingle
        | LDM of InstrMemMult
        | STM of InstrMemMult

    type Instr = 
        | Mem of MemInstr

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
            {
                Rn = reg; 
                addr = {addrReg = mem; offset = preoffset};
                postOffset = postoffset;
                suff = suffix;
            }
    
    /// Contructs an Instruction of InstrMemMult for LDM, STM
    let consMemMult reg rLst suffix =
            {
                Rn = reg;
                rList = rLst;
                suff = suffix;
            }
    
    /// A partially active pattern that returns an error if a register argument is not valid.
    let (|RegCheck|_|) txt =
        match Map.tryFind txt regNames with
        | Some reg ->
            reg |> Ok |> Some
        | _ ->
            (txt, notValidRegEM)
            ||> makeError
            |> ``Invalid register``
            |> Error
            |> Some

    /// Where everything happens
    let parse (ls: LineData) : Result<Parse<Instr>,ErrParse> option =
        let (WA la) = ls.LoadAddr
        /// Partial Active pattern for matching regexes
        /// Looking for something like [r1] or [r13
        /// For matching the address location 
        let (|MemMatch|_|) str =
            match str with 
            | ParseRegex "\[([rR][0-9]+)\]" address -> address |> makeReg |> Ok |> Some
            | ParseRegex "\[([rR][0-9]+)" address -> address |> makeReg |> Ok |> Some
            | _ -> 
                ("["+str+"]", notValidRegEM)
                ||> makeError
                |> ``Invalid register``
                |> Error
                |> Some

        /// For matching the list of regs
        let (|RegListMatch|_|) str =
            match str with 
            | ParseRegex "([rR][0-9]+)}" address -> address |> Some
            | ParseRegex "\[([rR][0-9]+)" address -> address |> Some
            | _ -> None
        
        /// Partial active pattern for matching both pre and post indexes
        /// e.g. str r0, [r1], r2   str r0, [r1], #4
        /// e.g. str r0, [r1, r2]   str r0, [r1, #4]
        /// e.g. str r0, [r1, r2]!  str r0, [r1, #4]!
        let (|OffsetMatch|_|) str =

            /// Register Post Index, No Pre Index
            /// e.g. str r0, [r1], r2
            let regNoPrePost = function
                | RegCheck r ->
                    match r with
                    | Ok r' -> 
                        let postInd = RegPost r' |> Some
                        let preInd = None
                        (preInd, postInd) |> Ok
                    | Error e -> Error e
                | _ -> failwith alwaysMatchesFM

            /// Register Pre Index, No Post Index
            /// e.g. str r0, [r1, r2]
            let regPreNoPost = function
                | RegCheck r ->
                    match r with
                    | Ok r' -> 
                        let postInd = None
                        let preInd = RegPre r' |> Some
                        (preInd, postInd) |> Ok
                    | Error e -> Error e
                | _ -> failwith alwaysMatchesFM

            
            /// Register Post Index and Pre Index
            /// e.g. str r0, [r1, r2]!
            let regPreAndPost = function
                | RegCheck r -> 
                    match r with
                    | Ok r' ->
                        let postInd = RegPost r' |> Some
                        let preInd = RegPre r' |> Some
                        (preInd, postInd) |> Ok
                    | Error e -> Error e
                | _ -> failwith alwaysMatchesFM

            /// Immediate Post Index, No Pre Index
            /// e.g. str r0, [r1], #4
            let immNoPrePost n =
                let postInd = ImmPost (uint32 n) |> Some
                let preInd = None
                (preInd, postInd) |> Ok
            
            /// Immediate Pre Index, No Post Index
            /// e.g. str r0, [r1, #4]
            let immPreNoPost n = 
                let postInd = None
                let preInd = ImmPre (uint32 n) |> Some
                (preInd, postInd) |> Ok
            
            /// Immediate Pre and Post Index
            /// e.g. str r0, [r1, #4]!
            let immPreAndPost n =
                let postInd = ImmPost (uint32 n) |> Some
                let preInd = ImmPre (uint32 n) |> Some
                (preInd, postInd) |> Ok


            match str with 
            | ParseRegex "([rR][0-9]+)" preOffReg -> preOffReg |> regNoPrePost |> Some
            | ParseRegex "([rR][0-9]+)\]" preOffReg -> preOffReg |> regPreNoPost |> Some
            | ParseRegex "([rR][0-9]+)\]!" preOffReg -> preOffReg |> regPreAndPost |> Some
            | ParseRegex "#0[xX]([0-9a-fA-F]+)" preOffHex -> ("0x" + preOffHex) |> immNoPrePost |> Some
            | ParseRegex "#([0-9]+)" preOffDec -> preOffDec |> immNoPrePost |> Some
            | ParseRegex "#&([0-9a-fA-F]+)" preOffHex -> ("0x" + preOffHex) |> immNoPrePost |> Some
            | ParseRegex "#0[bB]([0-1]+)" preOffBin -> ("0b" + preOffBin) |> immNoPrePost |> Some
            | ParseRegex "#0[xX]([0-9a-fA-F]+)\]" preOffHex -> ("0x" + preOffHex) |> immPreNoPost |> Some
            | ParseRegex "#([0-9]+)\]" preOffDec -> preOffDec |> immPreNoPost |> Some
            | ParseRegex "#&([0-9a-fA-F]+)\]" preOffHex -> ("0x" + preOffHex) |> immPreNoPost |> Some
            | ParseRegex "#0[bB]([0-1]+)\]" preOffBin -> ("0b" + preOffBin) |> immPreNoPost |> Some
            | ParseRegex "#0[xX]([0-9a-fA-F]+)\]!" preOffHex -> ("0x" + preOffHex) |> immPreAndPost |> Some
            | ParseRegex "#([0-9]+)\]!" preOffDec -> preOffDec |> immPreAndPost |> Some
            | ParseRegex "#&([0-9a-fA-F]+)\]!" preOffHex -> ("0x" + preOffHex) |> immPreAndPost |> Some
            | ParseRegex "#0[bB]([0-1]+)\]!" preOffBin -> ("0b" + preOffBin) |> immPreAndPost |> Some
            | _ -> 
                (str, notValidOffsetEM)
                ||> makeError
                |> ``Invalid offset``
                |> Error
                |> Some

        /// parse for LDM, STM
        let parseMult (root: string) suffix pCond : Result<Parse<Instr>, ErrParse> =

            /// Regex match the numbers in a hyphen list {r1 - r7}
            /// in order to construct full reg list.
            /// return the two numbers as low, high
            let (|RegListExpand|_|) str =
                match str with
                | ParseRegex2 "[rR]([0-9]+)-[rR]([0-9]+)" (low, high) -> (low, high) |> Some
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
                | ParseRegex "(([rR][0-9]+)-([rR][0-9]+))" listReg -> optionNumToRegList listReg
                | ParseRegex "([rR][0-9]+)!" bangReg -> bangReg |> optionMakeList
                | ParseRegex "([rR][0-9]+)" reg -> reg |> optionMakeList
                | _ -> None

            /// split the operands at a {
            let splitMult = splitAny ls.Operands '{'
            
            let checkMultSuffix = function
                | "IA" -> Some IA |> Ok
                | "IB" -> Some IB |> Ok
                | "DA" -> Some DA |> Ok
                | "DB" -> Some DB |> Ok
                | "FD" -> Some FD |> Ok
                | "ED" -> Some ED |> Ok
                | "FA" -> Some FA |> Ok
                | "EA" -> Some EA |> Ok
                | ""   -> Some IA |> Ok
                | _ -> 
                    (suffix, notValidSuffixEM)
                    ||> makeError
                    |> ``Invalid suffix``
                    |> Error

            let ops = 
                match splitMult with
                | [rOp1; rlst] -> // LDM, STM
                    let regList = splitAny (rlst.Replace("}", "")) ','
                    let reg = rOp1.Replace(",", "")

                    let matcher = function
                        | RegListMatch x -> x 
                        | _ -> []
                    
                    let checker = function
                        | RegCheck x -> x
                        | _ -> failwith alwaysMatchesFM

                    let rec applyToAll f list =
                        match list with
                        | [] -> []
                        | head :: tail -> f head :: applyToAll f tail

                    let allRegs = regList |> applyToAll matcher |> List.concat
                    let checkedRegs = 
                        allRegs
                        |> (applyToAll checker) 
                        |> condenseResultList (id)
                    match reg with
                    | RegCheck r' -> 
                        combineErrorMapResult r' checkedRegs consMemMult
                        |> mapErrorApplyResult (checkMultSuffix suffix)
                    | _ -> failwith alwaysMatchesFM     
                | _ ->
                    (ls.Operands, notValidFormatEM)
                    ||> makeError
                    |> ``Invalid instruction``
                    |> Error


            let make ops =
                { 
                    PInstr= memTypeMultMap.[root] ops |> Mem;
                    PLabel = ls.Label |> Option.map (fun lab -> lab, la); 
                    PSize = 4u; 
                    PCond = pCond 
                }
            Result.map make ops

        let parseSingle (root: string) suffix pCond : Result<Parse<Instr>,ErrParse> =         

            /// split operands at ','
            let splitOps = splitAny ls.Operands ','

            let checkSingleSuffix = function
                | "B" -> Some B |> Ok
                | "" -> None |> Ok
                | _ -> 
                    (suffix, notValidSuffixEM)
                    ||> makeError
                    |> ``Invalid suffix``
                    |> Error
            
            let ops =
                match splitOps with
                | [rOp1; addr] -> // str r0, [r1] or str r0, [r1, #4]
                    match rOp1 with
                    | RegCheck rOp1' ->  
                        match addr with
                        | MemMatch addr' ->
                            let partialConsMem = combineErrorMapResult rOp1' addr' consMemSingle
                            partialConsMem
                            |> mapErrorApplyResult (None |> Ok)
                            |> mapErrorApplyResult (None |> Ok)
                            |> mapErrorApplyResult (checkSingleSuffix suffix)
                        | _ -> failwith alwaysMatchesFM
                    | _ -> failwith alwaysMatchesFM
                | [rOp1; addr; postOff] -> // str r0, [r1], #4
                    match rOp1 with
                    | RegCheck rOp1' ->
                        match addr with
                        | MemMatch addr' ->
                            match postOff with
                            | OffsetMatch tuple ->
                                let partialConsMem = combineErrorMapResult rOp1' addr' consMemSingle
                                partialConsMem
                                |> mapErrorApplyResult (Result.map (fst) tuple)
                                |> mapErrorApplyResult (Result.map (snd) tuple)
                                |> mapErrorApplyResult (checkSingleSuffix suffix)
                            | _ -> failwith alwaysMatchesFM
                        | _ -> failwith alwaysMatchesFM
                    | _ -> failwith alwaysMatchesFM
                | _ -> 
                    (ls.Operands, notValidFormatEM)
                    ||> makeError
                    |> ``Invalid instruction``
                    |> Error

            let make ops =
                { 
                    PInstr= memTypeSingleMap.[root] ops |> Mem;
                    PLabel =ls.Label |> Option.map (fun lab -> lab, la); 
                    PSize = 4u; 
                    PCond = pCond 
                }
            Result.map (make) ops

        let parse' (_instrC, (root : string,suffix : string,pCond)) =
            let uRoot = root.ToUpper()
            let uSuffix = suffix.ToUpper()
            match root.ToUpper() with
            | "LDR" -> parseSingle uRoot uSuffix pCond
            | "STR" -> parseSingle uRoot uSuffix pCond
            | "LDM" -> parseMult uRoot uSuffix pCond
            | "STM" -> parseMult uRoot uSuffix pCond
            | _ -> failwith "We appear to have a rogue root"
           

        Map.tryFind (uppercase ls.OpCode) opCodes
        |> Option.map parse'

    /// Parse Active Pattern used by top-level code
    let (|IMatch|_|)  = parse
