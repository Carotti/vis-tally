module Execution
    open CommonData
    open CommonLex
    open Helpers
    open CommonTop
    open Errors

    /// Function for setting a register
    /// Takes RName and value and returns
    /// new DataPath with that register set.
    let setReg reg contents cpuData =
        let setter reg' old = 
            match reg' with
            | x when x = reg -> contents
            | _ -> old
        {cpuData with Regs = Map.map setter cpuData.Regs}
    
    /// Recursive function for setting multiple registers
    /// Need to check that the lists provided are the same length
    let rec setMultRegs regLst contentsLst cpuData =
        match regLst, contentsLst with
        | rhead :: rtail, chead :: ctail when (List.length regLst = List.length contentsLst) ->
            let newCpuData = setReg rhead chead cpuData
            setMultRegs rtail ctail newCpuData 
        | [], [] -> cpuData
        | _ -> failwith "Lists given to setMultRegs function were of different sizes."
        
    /// Blank dataPath with all regs set to Zero and flags to false
    let initDataPath : DataPath<Instr> =
        let flags =
            {N = false; C = false; Z = false; V = false}

        let initRegs vals =
            vals
            |> List.zip [0u..15u]
            |> List.map (fun (r, _v) -> (makeRegFromNum r, 0u))
            |> Map.ofList
                
        {
            Fl = flags;    
            Regs = initRegs [0u..15u] 
            MM = Map.ofList []
        }

    let updatePC (instr: Parse<Instr>) (cpuData: DataPath<Instr>) : DataPath<Instr> =
        let pc = cpuData.Regs.[R15]
        let size = instr.PSize
        setReg R15 (pc + size) cpuData
    
    let getPC (cpuData: DataPath<Instr>) =
        cpuData.Regs.[R15]

    let getMemLoc addr cpuData =
        cpuData.MM.[addr]

    let locExists m cpuData = 
        Map.containsKey m cpuData.MM

    /// Tom's condExecute instruction as he made it first (don't reinvent the wheel)
    let condExecute (instr: CommonLex.Parse<Instr>) (cpuData: DataPath<Instr>) =
        let n, c, z, v = (cpuData.Fl.N, cpuData.Fl.C, cpuData.Fl.Z, cpuData.Fl.V)
        match instr.PCond with
        | Cal -> true
        | Cnv -> false
        | Ceq -> z
        | Cne -> (not z)
        | Chs -> c
        | Clo -> (not c)
        | Cmi -> n
        | Cpl -> (not n)
        | Cvs -> v
        | Cvc -> (not v)
        | Chi -> (c && not z)
        | Cls -> (not c || z)
        | Cge -> (n = v)
        | Clt -> (n <> v)
        | Cgt -> (not z && (n = v))
        | Cle -> (z || (n <> v))
    
    /// Return a new datapath with reg rX set to value
    let updateReg value rX dp =
        {dp with Regs = Map.add rX value dp.Regs}
        
    let validateWA addr =
        match addr % word with
        | 0u -> true
        | _ -> false
            

    // Update the whole word at addr with value in dp
    // let updateMem value (addr : uint32) dp =
    //     match addr % 4u with
    //     | 0u -> {dp with MM = Map.add (WA addr) value dp.MM}
    //     | _ -> failwithf "Trying to update memory at unaligned address"

    let updateMem value (addr : uint32) (dp: DataPath<Instr>) =
        match validateWA addr with
        | true -> {dp with MM = Map.add (WA addr) value dp.MM} |> Ok
        | false -> 
            (addr |> string, " Trying to update memory at unaligned address.")
            ||> makeError 
            |> ``Run time error``
            |> Error

    let updateMemData value = updateMem (DataLoc value)

    /// Return the next aligned address after addr
    let alignAddress addr = (addr / word) * word
        
    /// Update a single byte in memory (Little Endian)
    // let updateMemByte (value : byte) (addr : uint32) dp =
    //     let baseAddr = alignAddress (addr)
    //     let shft = (int ((addr % 4u)* 8u))
    //     let mask = 0xFFu <<< shft |> (~~~)
    //     let oldVal = 
    //         match Map.containsKey (WA baseAddr) dp.MM with
    //         | true -> dp.MM.[WA baseAddr]
    //         | false -> DataLoc 0u // Uninitialised memory is zeroed
    //     let newVal = 
    //         match oldVal with
    //         | DataLoc x -> (x &&& mask) ||| ((uint32 value) <<< shft)
    //         | _ -> failwithf "Updating byte at instruction address"
    //     updateMem (DataLoc newVal) baseAddr dp
    
    let updateMemByte (value : byte) (addr : uint32) (dp: DataPath<Instr>) =
        let baseAddr = alignAddress addr
        let shft = (int ((addr % word)* 8u))
        let mask = 0xFFu <<< shft |> (~~~)
        let oldVal = 
            match Map.containsKey (WA baseAddr) dp.MM with
            | true -> dp.MM.[WA baseAddr]
            | false -> DataLoc 0u // Uninitialised memory is zeroed
        match oldVal with
        | DataLoc x -> 
            let newVal = (x &&& mask) ||| ((uint32 value) <<< shft)
            updateMem (DataLoc newVal) baseAddr dp
        | Code c -> 
            (c |> string, " Updating a byte at an instruction address.")
            ||> makeError 
            |> ``Run time error``
            |> Error

   /// LDRB to load the correct byte
    let getCorrectByte value addr = 
        let shift = 8u * (addr % word) |> int32
        ((0x000000FFu <<< shift) &&& value) >>> shift

    let fetchMemData reg addr (cpuData: DataPath<Instr>) =
        match validateWA addr with
        | true ->
            match addr with
            | a when (a < minAddress) ->
                (a |> string, " Trying to access memory where instructions are stored.")
                ||> makeError 
                |> ``Run time error``
                |> Error
            | _ -> 
                let baseAddr = alignAddress addr
                let wordAddr = WA baseAddr
                match locExists wordAddr cpuData with
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
        | false -> 
            (addr |> string, " Trying to update memory at unaligned address.")
            ||> makeError 
            |> ``Run time error``
            |> Error
    
    let fetchMemByte reg addr (cpuData: DataPath<Instr>) =
        match addr with
        | a when (a < minAddress) ->
            (a |> string, " Trying to access memory where instructions are stored.")
            ||> makeError 
            |> ``Run time error``
            |> Error
        | _ -> 
            let baseAddr = alignAddress addr
            let wordAddr = WA baseAddr
            match locExists wordAddr cpuData with
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
            | false -> 
                // (addr |> string, " You have not stored anything at this address, value is set to 0.")
                // ||> makeError 
                // |> ``Run time warning``
                // |> Error
                setReg reg 0u cpuData |> Ok

    /// Recursive function for storing multiple values at multiple memory addresses
    /// Need to check that the lists provided are the same length
    let rec setMultMem contentsLst addrLst cpuData : Result<DataPath<Instr>, ErrExe> =
        match addrLst, contentsLst with
        | mhead :: mtail, chead :: ctail when (List.length addrLst = List.length contentsLst) ->
            let newCpuData = updateMemData chead mhead cpuData
            Result.bind (setMultMem ctail mtail) newCpuData
        | [], [] -> cpuData |> Ok
        | _ -> failwith "Lists given to setMultMem function were of different sizes."
    
    /// Multiple setMemDatas 
    // let setMultMemData contentsLst = setMultMem (List.map DataLoc contentsLst)

        
    let fillRegs (vals : uint32 list) =
        List.zip [0..15] vals
        |> List.map (fun (r, v) -> (register r, v))
        |> Map.ofList

    let emptyRegs = 
        [0..15]
        |> List.map (fun _ -> 0u)
        |> fillRegs
        
    let initialDp () = {
            Fl = {N = false ; C = false ; Z = false ; V = false};
            Regs = emptyRegs;
            MM = Map.ofList []
        }
    let isMisc instr =
        match instr.PInstr with
        | CommonTop.IMISC _ ->
            true
        | _ ->
            false