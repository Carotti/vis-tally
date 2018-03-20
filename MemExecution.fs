module MemExecution
    open CommonData
    open Helpers
    open Memory
    open CommonTop
    open Execution
    open Errors

    let executeMem instr (cpuData: DataPath<Instr>) =

        let regContents r = cpuData.Regs.[r]

        let getMemLoc addr cpuData =
            cpuData.MM.[addr]

        let locExists m = 
            Map.containsKey m cpuData.MM

        /// check if word address is valid and multiple of 4
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
        
        /// check if valid, if so return Word Address
        let wordAddress a = 
            match a with
            | Valid -> WA a |> Ok
            | _ -> 
                (a |> string, " is not a valid word address.")
                ||> makeError 
                |> ``Run time error``
                |> Error
        
        /// make an offset list for ldm and stm by recursively
        /// adding an incr to the address for the length of the list
        let rec makeOffsetList inlst outlist incr start = 
            match inlst with
            | _ :: tail -> (start + incr) |> makeOffsetList tail (start :: outlist) incr
            | [] -> outlist
 
        /// STRB to set the correct byte
        let setCorrectByte value addr = 
            let shift = 8u * (addr % 4u) |> int32
            (value &&& 0x000000FFu) <<< shift

        /// LDRB to load the correct byte
        let getCorrectByte value addr = 
            let shift = 8u * (addr % 4u) |> int32
            ((0x000000FFu <<< shift) &&& value) >>> shift

        /// Check if B suffix is presesnt on STR
        let setWordOrByte suffix (value: uint32) addr cpuData = 
            match suffix with
            | Some B -> updateMemByte (value |> byte) addr cpuData
            | None -> updateMemData value addr cpuData

        /// Check if B suffix is presesnt on LDR
        let getWordOrByte suffix value addr = 
            match suffix with
            | Some B -> getCorrectByte value addr
            | None -> value
        
        let fetchMemData reg addr cpuData =
            match addr with
            | a when (a < minAddress) ->
                (a |> string, " Trying to access memory where instructions are stored.")
                ||> makeError 
                |> ``Run time error``
                |> Error
            | _ -> 
                let wordAddr = WA addr
                match locExists wordAddr with
                | true -> 
                    match getMemLoc wordAddr cpuData with
                    | DataLoc dl ->
                        setReg reg dl cpuData |> Ok
                    | Code c -> 
                        (c |> string, " Trying to access memory where instructions are stored.")
                        ||> makeError 
                        |> ``Run time error``
                        |> Error
                | false -> setReg reg 0u cpuData |> Ok
        
        let fetchMemByte reg addr cpuData =
            match addr with
            | a when (a < minAddress) ->
                (a |> string, " Trying to access memory where instructions are stored.")
                ||> makeError 
                |> ``Run time error``
                |> Error
            | _ -> 
                let wordAddr = WA addr
                match locExists wordAddr with
                | true -> 
                    match getMemLoc wordAddr cpuData with
                    | DataLoc dl ->
                        let byteValue = getCorrectByte dl addr
                        setReg reg byteValue cpuData |> Ok
                    | Code c -> 
                        (c |> string, " Trying to access memory where instructions are stored.")
                        ||> makeError 
                        |> ``Run time error``
                        |> Error
                | false -> setReg reg 0u cpuData |> Ok

        /// get memory stored a address check its not in code segment 
        let getMem addr cpuData = 
            match addr with
            | x when (x < minAddress) ->
                "Trying to access code memory location. < 0x100" |> qp |> ignore
                0u
            | _ -> 
                let wordAddr = WA addr
                match locExists wordAddr with
                | true -> 
                    let memloc = cpuData.MM.[wordAddr] 
                    getMemData memloc
                | false -> 
                    "Nothing stored at provided address" |> qp
                    0u
        
        /// get multiple memory 
        let rec getMemMult addrList contentsLst cpuData = 
            match addrList with
            | head :: tail ->
                let addedVal = (getMem head cpuData) :: contentsLst
                getMemMult tail addedVal cpuData
            | [] -> contentsLst |> List.rev
        
        let executeLDR suffix rn addr offset cpuData = 
            let address = (regContents addr.addrReg + getOffsetType addr.offset)
            let alignedAddr = alignAddress address
            let contents = getMem alignedAddr cpuData
            let value = getWordOrByte suffix contents (regContents addr.addrReg + getOffsetType addr.offset)
            let newCpuData = setReg rn value cpuData
            setReg addr.addrReg (regContents addr.addrReg + getPostIndex offset) newCpuData
                
        let executeSTR suffix rn addr offset cpuData = 
            let address = (regContents addr.addrReg + getOffsetType addr.offset)
            let cpuData' = setWordOrByte suffix (regContents rn) address cpuData
            setReg addr.addrReg (regContents addr.addrReg + getPostIndex offset) cpuData'

        let executeLDM suffix rn rl cpuData =
            let offsetList start = 
                let lst =
                    match suffix with
                    | None ->
                         start
                        |> makeOffsetList rl [] 4
                        |> List.rev      
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
                List.map (fun el -> el |> uint32) lst
            let baseAddrInt = (regContents rn) |> int32
            let contents = getMemMult (offsetList baseAddrInt) [] cpuData
            setMultRegs rl contents cpuData

        let executeSTM suffix rn rl cpuData = 
            let offsetList start = 
                let lst =
                    match suffix with
                    | None ->
                        start
                        |> makeOffsetList rl [] 4
                        |> List.rev      
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
                List.map (fun el -> el |> uint32) lst
            let baseAddrInt = (regContents rn) |> int32
            let regContentsList = List.map regContents rl
            setMultMemData regContentsList (offsetList baseAddrInt) cpuData

        let executeInstr (instr: MemInstr) (cpuData: DataPath<Instr>) = 
            match instr with
            | LDR operands -> 
                executeLDR operands.suff operands.Rn operands.addr operands.postOffset cpuData
            | STR operands ->
                executeSTR operands.suff operands.Rn operands.addr operands.postOffset cpuData
            | LDM operands ->
                executeLDM operands.suff operands.Rn operands.rList cpuData
            | STM operands ->
                executeSTM operands.suff operands.Rn operands.rList cpuData

        executeInstr instr cpuData |> Ok


