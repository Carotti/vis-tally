module Execution
    open CommonData
    open CommonLex

    /// Return a new datapath with reg rX set to value
    let updateReg value rX dp =
        {dp with Regs = Map.add rX value dp.Regs}

    // Update the whole word at addr with value in dp
    let updateMem value (addr : uint32) dp =
        match addr % 4u with
        | 0u -> {dp with MM = Map.add (WA addr) value dp.MM}
        | _ -> failwithf "Trying to update memory at unaligned address"

    /// Update a single byte in memory (Little Endian)
    let updateMemByte (value : byte) (addr : uint32) dp =
        let baseAddr = addr % 4u
        let shft = (int (baseAddr * 8u))
        let mask = 0xFFu <<< shft |> (~~~)
        let newVal = 
            match dp.MM.[WA baseAddr] with
            | DataLoc x -> (x &&& mask) ||| ((uint32 value) <<< shft)
            | _ -> failwithf "Updating byte at instruction address"
        updateMem (DataLoc newVal) baseAddr dp

    let updateMemData value = updateMem (DataLoc value)

    /// Return the next aligned address after addr
    let alignAddress addr = (addr / 4u) * 4u
        
    /// Return whether or not an instruction should be executed
    let condExecute ins (data : DataPath<'INS>) =
        let (n, c, z, v) = (data.Fl.N, data.Fl.C, data.Fl.Z, data.Fl.V)
        match ins.PCond with
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