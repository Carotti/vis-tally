// module DPExecution
//     open CommonData
//     open CommonLex
//     open CommonTop

//     // let initDP n c z v (regVals: uint32 list) : DataPath<Instr> =
//     //     let flags =
//     //         {N = n; C = c; Z = z; V = v;}

//     //     let fillRegs (regVals: uint32 list) =
//     //         match List.length regVals with
//     //         | 16 ->
//     //             regVals
//     //             |> List.zip [0u..15u]
//     //             |> List.map (fun (r,v) -> (consRegG r, v))
//     //             |> Map.ofList
//     //         | _ ->
//     //             [0u..15u]
//     //             |> List.zip [0u..15u]
//     //             |> List.map (fun (r, _v) -> (consRegG r, 0u))
//     //             |> Map.ofList
                
//     //     {
//     //         Fl = flags; 
//     //         Regs = fillRegs regVals; 
//     //         MM = Map.empty<WAddr,MemLoc<Instr>>
//     //     }                
    
//     let updatePC (instr: Parse<Instr>) (cpuData: DataPath<Instr>) : DataPath<Instr> =
//         let pc = cpuData.Regs.[R15]
//         let size = instr.PSize
//         setReg R15 (pc + size) cpuData

//     let condExecute (instr: Parse<Instr>) (cpuData: DataPath<Instr>) =
//         let n, c, z, v = (cpuData.Fl.N, cpuData.Fl.C, cpuData.Fl.Z, cpuData.Fl.V)
//         match instr.PCond with
//         | Cal -> true
//         | Cnv -> false
//         | Ceq -> z
//         | Cne -> (not z)
//         | Chs -> c
//         | Clo -> (not c)
//         | Cmi -> n
//         | Cpl -> (not n)
//         | Cvs -> v
//         | Cvc -> (not v)
//         | Chi -> (c && not z)
//         | Cls -> (not c || z)
//         | Cge -> (n = v)
//         | Clt -> (n <> v)
//         | Cgt -> (not z && (n = v))
//         | Cle -> (z || (n <> v))