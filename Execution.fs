module Execution
    open CommonData
    open CommonLex

    /// Return a new datapath with reg rX set to value
    let updateReg rX value dp =
        let updater reg old =
            match reg with
            | x when x = rX -> value
            | _ -> old
        {dp with Regs = Map.map updater dp.Regs}

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

    let emptyRegs = 
        let map0 x = (x, 0u)
        [0..15]
        |> List.map (register >> map0)
        |> Map.ofList

    let initialDp () = {
            Fl = {N = false ; C = false ; Z = false ; V = false};
            Regs = emptyRegs;
            MM = Map.ofList []
        }