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

        let dataFn m =
            match m with 
            | DataLoc dl -> dl
            // | Code c -> c
            | _ -> failwithf "Ah"

        let wordOrByte suffix d = 
            match suffix with
            | Some B -> d &&& 0x000000FFu
            | None -> d 
        
        let executeLDR suffix rn addr offset cpuData = 
            let memloc = memContents.[wordAddress (regContents addr.addrReg + getOffsetType addr.offset)] 
            let value = wordOrByte suffix (dataFn memloc)
            let update = setReg rn value cpuData
            setReg addr.addrReg (regContents addr.addrReg + getPostIndex offset) update
                
        let executeSTR suffix rn addr offset cpuData = 
            let value = wordOrByte suffix (regContents rn)
            let update = setMem (wordAddress (regContents addr.addrReg + getOffsetType addr.offset)) value cpuData
            setReg addr.addrReg (regContents addr.addrReg + getPostIndex offset) update

        let executeLDM suffix rn regList cpuData =
            let rl =
                match regList with
                | RegList rl -> rl
            let offsetList start = 
                let lst =
                    match suffix with           
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

            let baseAddrInt = (regContents rn) |> int32
            let wordAddrList = List.map wordAddress (offsetList baseAddrInt)
            let memLocList = List.map (fun m -> memContents.[m]) wordAddrList
            let dataLocList = List.map dataFn memLocList
            setMultRegs rl dataLocList cpuData

        let executeSTM suffix rn regList cpuData = 
            let rl =
                match regList with
                | RegList rl -> rl  

            let offsetList start = 
                let lst =
                    match suffix with           
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

            let baseAddrInt = (regContents rn) |> int32
            let wordAddrList = List.map wordAddress (offsetList baseAddrInt)
            let regContentsList = List.map regContents rl
            setMultMem wordAddrList regContentsList cpuData

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
            

