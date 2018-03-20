module Execution
    open CommonData
    open CommonLex
    open Helpers
    open CommonTop
    open Errors
    
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

    // Update the whole word at addr with value in dp
    let updateMem value (addr : uint32) dp =
        match addr % 4u with
        | 0u -> {dp with MM = Map.add (WA addr) value dp.MM}
        | _ -> failwithf "Trying to update memory at unaligned address"

    // let updateMem value (addr : uint32) dp =
    //     match addr % 4u with
    //     | 0u -> {dp with MM = Map.add (WA addr) value dp.MM} |> Ok
    //     | _ -> 
    //         (addr |> string, " Trying to update memory at unaligned address.")
    //         ||> makeError 
    //         |> ``Run time error``
    //         |> Error

    let updateMemData value = updateMem (DataLoc value)

    /// Return the next aligned address after addr
    let alignAddress addr = (addr / 4u) * 4u
        
    /// Update a single byte in memory (Little Endian)
    let updateMemByte (value : byte) (addr : uint32) dp =
        let baseAddr = alignAddress (addr)
        let shft = (int ((addr % 4u)* 8u))
        let mask = 0xFFu <<< shft |> (~~~)
        let oldVal = 
            match Map.containsKey (WA baseAddr) dp.MM with
            | true -> dp.MM.[WA baseAddr]
            | false -> DataLoc 0u // Uninitialised memory is zeroed
        let newVal = 
            match oldVal with
            | DataLoc x -> (x &&& mask) ||| ((uint32 value) <<< shft)
            | _ -> failwithf "Updating byte at instruction address"
        updateMem (DataLoc newVal) baseAddr dp
    
    // let updateMemByte (value : byte) (addr : uint32) dp =
    //     let baseAddr = alignAddress (addr)
    //     let shft = (int ((addr % 4u)* 8u))
    //     let mask = 0xFFu <<< shft |> (~~~)
    //     let oldVal = 
    //         match Map.containsKey (WA baseAddr) dp.MM with
    //         | true -> dp.MM.[WA baseAddr]
    //         | false -> DataLoc 0u // Uninitialised memory is zeroed
    //     match oldVal with
    //     | DataLoc x -> 
    //         let newVal = (x &&& mask) ||| ((uint32 value) <<< shft)
    //         updateMem (DataLoc newVal) baseAddr dp |> Ok
    //     | Code c -> 
    //         (c |> string, " Updating a byte at an instruction address.")
    //         ||> makeError 
    //         |> ``Run time error``
    //         |> Error
        
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