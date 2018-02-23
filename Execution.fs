module Execution
    open CommonData
    open CommonLex
    open Helpers
    open CommonTop
    
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
    
