module Execution
    open CommonData
    open CommonLex
    open CommonTop

    let initDP n c z v (regVals: uint32 list) : DataPath<Instr> =
        let flags =
            {N = n; C = c; Z = z; V = v;}

        let fillRegs (regVals: uint32 list) =
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


    let setReg reg contents cpuData =
        let setter reg' old = 
            match reg' with
            | x when x = reg -> contents
            | _ -> old
        {cpuData with Regs = Map.map setter cpuData.Regs}
    
    let rec setMultRegs regLst contentsLst cpuData =
        match regLst, contentsLst with
        | rhead :: rtail, chead :: ctail when (List.length regLst = List.length contentsLst) ->
            let newCpuData = setReg rhead chead cpuData
            setMultRegs rtail ctail newCpuData
        | [], [] -> cpuData
        | _ -> failwithf "Something went wrong with lists"
    
    let setMem mem contents cpuData =
        let setter mem' old =
            match mem' with
            | x when x = mem -> DataLoc contents
            | _ -> old
        {cpuData with MM = Map.map setter cpuData.MM}
    
    let rec setMultMem memLst contentsLst cpuData =
        match memLst, contentsLst with
        | mhead :: mtail, chead :: ctail when (List.length memLst = List.length contentsLst) ->
            let newCpuData = setMem mhead chead cpuData
            setMultMem mtail ctail newCpuData
        | [], [] -> cpuData
        | _ -> failwithf "Something went wrong with lists"
    
    let updatePC (instr: Parse<Instr>) (cpuData: DataPath<Instr>) : DataPath<Instr> =
        let pc = cpuData.Regs.[R15]
        let size = instr.PSize
        setReg R15 (pc + size) cpuData

    let condExecute (instr: Parse<Instr>) (cpuData: DataPath<Instr>) =
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
