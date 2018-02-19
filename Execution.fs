module Execution
    open CommonData
    open CommonLex
    open DP
    open CommonTop
    open Mono.Cecil.Cil

    let initialiseDP n c z v (vals:uint32 list) : DataPath<Instr> =
        let flags =
            {N = n; C = c; Z = z; V = v;}

        let regVals (vals:uint32 list) =
            match List.length vals with
            | 16 ->
                Map.ofList [    (R0, vals.[0]); (R1, vals.[1]); (R2, vals.[2]); (R3, vals.[3]);
                                (R4, vals.[4]); (R5, vals.[5]); (R6, vals.[6]); (R7, vals.[7]);
                                (R8, vals.[8]); (R9, vals.[9]); (R10, vals.[10]); (R11, vals.[11]);
                                (R12, vals.[12]); (R13, vals.[13]); (R14, vals.[14]); (R15, vals.[15]);
                            ]
            | _ ->
                let zeroVals = List.map (fun _i -> 0 |> uint32) [0..15]
                Map.ofList [    (R0, zeroVals.[0]); (R1, zeroVals.[1]); (R2, zeroVals.[2]); (R3, zeroVals.[3]);
                                (R4, zeroVals.[4]); (R5, zeroVals.[5]); (R6, zeroVals.[6]); (R7, zeroVals.[7]);
                                (R8, zeroVals.[8]); (R9, zeroVals.[9]); (R10, zeroVals.[10]); (R11, zeroVals.[11]);
                                (R12, zeroVals.[12]); (R13, zeroVals.[13]); (R14, zeroVals.[14]); (R15, zeroVals.[15]);
                            ]

        {
            Fl = flags; 
            Regs = regVals vals; 
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