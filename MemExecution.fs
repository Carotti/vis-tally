module MemExecution
    open CommonData
    open Helpers
    open Memory
    open CommonTop
    open Execution

    let executeMem (instr: CommonLex.Parse<Instr>) (cpuData: DataPath<Instr>) : DataPath<Instr> =

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


        // Only works on the first Byte at the moment
        let wordOrByte suffix d = 
            match suffix with
            | Some B -> d &&& 0x000000FFu
            | None -> d

        let getMem addr cpuData = 
            match addr with
            | x when (x < minAddress) ->
                failwithf "getMem called with code section address: %x" addr
            | _ -> 
                let memValid m = Map.containsKey m cpuData.MM
                let wordAddr = WA addr
                match memValid wordAddr with
                | true -> 
                    let memloc = cpuData.MM.[wordAddr] 
                    getMemData memloc
                | false -> 
                    "Nothing stored at provided address" |> qp
                    0u
        
        let rec getMemMult addrList contentsLst cpuData = 
            match addrList with
            | head :: tail ->
                let addedVal = (getMem head cpuData) :: contentsLst
                getMemMult tail addedVal cpuData
            | [] -> contentsLst |> List.rev
        
        let executeLDR suffix rn addr offset cpuData = 
            let contents = getMem (regContents addr.addrReg + getOffsetType addr.offset) cpuData
            let value = wordOrByte suffix contents
            let newCpuData = setReg rn value cpuData
            setReg addr.addrReg (regContents addr.addrReg + getPostIndex offset) newCpuData
                
        let executeSTR suffix rn addr offset cpuData = 
            let value = wordOrByte suffix (regContents rn)
            let update = setMemData value (regContents addr.addrReg + getOffsetType addr.offset) cpuData
            setReg addr.addrReg (regContents addr.addrReg + getPostIndex offset) update

        let executeLDM suffix rn regList cpuData =
            let rl =
                match regList with
                | RegList rl -> rl
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

        let executeSTM suffix rn regList cpuData = 
            let rl =
                match regList with
                | RegList rl -> rl  
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

        match condExecute instr cpuData with
        | true -> 
            match instr.PInstr with
            | CommonTop.IMEM (Mem instr') ->
                executeInstr instr' cpuData            
            | _ -> failwithf "Not a valid instruction"
        | false -> 
            updatePC instr cpuData
            

