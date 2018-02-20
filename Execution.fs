module Execution
    open CommonData
    open CommonLex
    open DP
    open CommonTop
    open Mono.Cecil.Cil
    open System

    let initialiseDP n c z v (regVals:uint32 list) : DataPath<Instr> =
        let flags =
            {N = n; C = c; Z = z; V = v;}

        let fillRegs (regVals:uint32 list) =
            match List.length regVals with
            | 16 ->
                regVals
                |> List.zip [0u..15u]
                |> List.map (fun (r,v) -> (consRegG r, v))
                |> Map.ofList
            | _ ->
                [0u..15u]
                |> List.zip [0u..15u]
                |> List.map (fun (r, _v) -> (consRegG r, 0u))
                |> Map.ofList
                
        {
            Fl = flags; 
            Regs = fillRegs regVals; 
            MM = Map.empty<WAddr,MemLoc<Instr>>
        }                

    /// Return a new datapath with reg rX set to value
    let updateReg rX value dp =
        let updater reg old =
            match reg with
            | x when x = rX -> value
            | _ -> old
        {dp with Regs = Map.map updater dp.Regs}

    let updatePC (instr:CommonLex.Parse<Instr>) (dp:DataPath<Instr>) : DataPath<Instr> =
        let pc = dp.Regs.[R15]
        let size = instr.PSize
        updateReg R15 (pc+size) dp

    /// Return whether or not an instruction should be executed
    let condExecute (ins:CommonLex.Parse<Instr>) (data : DataPath<Instr>) =
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