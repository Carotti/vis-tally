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

     /// Error types for parsing.
    type ErrInstr =
        | ``Invalid memory address`` of ErrorBase
        | ``Invalid offset`` of ErrorBase
        | ``Invalid register`` of ErrorBase
        | ``Invalid shift`` of ErrorBase
        | ``Invalid flexible second operand`` of ErrorBase
        | ``Invalid suffix`` of ErrorBase
        | ``Invalid instruction`` of ErrorBase
        | ``Syntax error`` of ErrorBase

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
            (txt, " is not a valid register.")
            ||> makeError
            |> ``Invalid register``
            |> Error
            |> Some

    /// Where everything happens
    let parse (ls: LineData) : Result<Parse<Instr>,ErrInstr> option =

        /// Partial Active pattern for matching regexes
        /// Looking for something like [r1] or [r13
        /// For matching the address location 
        let (|MemMatch|_|) str =
            match str with 
            | ParseRegex "\[([rR][0-9]+)\]" address -> address |> makeReg |> Ok |> Some
            | ParseRegex "\[([rR][0-9]+)" address -> address |> makeReg |> Ok |> Some
            | _ -> 
                ("["+str+"]", " is not a valid register.")
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
                | _ -> 
                    failwith "Should never happen! Match statement always matches."

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
                | _ -> 
                    failwith "Should never happen! Match statement always matches."

            
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
                | _ ->
                    failwith "Should never happen! Match statement always matches."

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
            | ParseRegex "#(0[xX][0-9a-fA-F]+)" preOffHex -> preOffHex |> immNoPrePost |> Some
            | ParseRegex "#([0-9]+)" preOffDec -> preOffDec |> immNoPrePost |> Some
            | ParseRegex "#&([0-9a-fA-F]+)" preOffHex -> ("0x" + preOffHex) |> immNoPrePost |> Some
            | ParseRegex "#(0[bB][0-1]+)" preOffBin -> preOffBin |> immNoPrePost |> Some
            | ParseRegex "#(0[xX][0-9a-fA-F]+)\]" preOffHex -> preOffHex |> immPreNoPost |> Some
            | ParseRegex "#([0-9]+)\]" preOffDec -> preOffDec |> immPreNoPost |> Some
            | ParseRegex "#&([0-9a-fA-F]+)\]" preOffHex -> ("0x" + preOffHex) |> immPreNoPost |> Some
            | ParseRegex "#(0[bB][0-1]+)\]" preOffBin -> preOffBin |> immPreNoPost |> Some
            | ParseRegex "#(0[xX][0-9a-fA-F]+)\]!" preOffHex -> preOffHex |> immPreAndPost |> Some
            | ParseRegex "#([0-9]+)\]!" preOffDec -> preOffDec |> immPreAndPost |> Some
            | ParseRegex "#&([0-9a-fA-F]+)\]!" preOffHex -> ("0x" + preOffHex) |> immPreAndPost |> Some
            | ParseRegex "#(0[bB][0-1]+)\]!" preOffBin -> preOffBin |> immPreAndPost |> Some
            | _ -> 
                (str, " is not a valid offset.")
                ||> makeError
                |> ``Invalid offset``
                |> Error
                |> Some

        /// parse for LDM, STM
        let parseMult (root: string) suffix pCond : Result<Parse<Instr>,ErrInstr> =

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
                | "IA" -> Some IA
                | "IB" -> Some IB
                | "DA" -> Some DA
                | "DB" -> Some DB
                | "FD" -> Some FD
                | "ED" -> Some ED
                | "FA" -> Some FA
                | "EA" -> Some EA
                | _ -> None

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
                        | _ -> failwithf "Should never happen! Match statement always matches."

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
                        |> mapErrorApplyResult ((checkMultSuffix suffix) |> Ok)
                    | _ -> failwithf "Should never happen! Match statement always matches."     
                | _ ->
                    (ls.Operands, "Syntax error. Instruction format is incorrect.")
                    ||> makeError
                    |> ``Invalid instruction``
                    |> Error


            let make ops =
                { 
                    PInstr= memTypeMultMap.[root] ops |> Mem;
                    PLabel = None ; 
                    PSize = 4u; 
                    PCond = pCond 
                }
            Result.map (make) ops

        let parseSingle (root: string) suffix pCond : Result<Parse<Instr>,ErrInstr> =         

            /// split operands at ','
            let splitOps = splitAny ls.Operands ','

            let checkSingleSuffix = function
                | "B" -> Some B
                | "" -> None
                | _ -> failwithf "Should never happen, not a suffix"
            
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
                            |> mapErrorApplyResult ((checkSingleSuffix suffix) |> Ok)
                        | _ -> failwith "Should never happen! Match statement always matches."
                    | _ -> failwith "Should never happen! Match statement always matches."
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
                                |> mapErrorApplyResult ((checkSingleSuffix suffix) |> Ok)
                            | _ -> failwith "Should never happen! Match statement always matches."
                        | _ -> failwith "Should never happen! Match statement always matches."
                    | _ -> failwith "Should never happen! Match statement always matches."
                | _ -> 
                    (ls.Operands, "Syntax error. Instruction format is incorrect.")
                    ||> makeError
                    |> ``Invalid instruction``
                    |> Error

            let make ops =
                { 
                    PInstr= memTypeSingleMap.[root] ops |> Mem;
                    PLabel = None ; 
                    PSize = 4u; 
                    PCond = pCond 
                }
            Result.map (make) ops

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
