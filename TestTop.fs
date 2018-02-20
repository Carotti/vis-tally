module TestTop

    open VisualTest.Visual
    open VisualTest.VTest
    open VisualTest.VData
    open VisualTest.VCommon

    open CommonData
    open VisualTest

    // Symbol table used by all tests, AND in visUAL prelude
    let ts = Map.ofList [
                "moo", 17123u
                "fOO", 402u
                "Bar", 19721u
                "z1", 139216u
                "rock74", 16u
                "Nice1", 0xF0F0F0F0u
                "Nice2", 0x0F0F0F0Fu
                "bigNum", 0xFFFFFFFFu
                "n0thing", 0u
        ]

    let assumedMemBase = 0x1000u

    let visualSyms = 
        Map.map (fun key v -> sprintf "%s EQU %d\n" key v) ts
        |> Map.toArray
        |> Array.toList
        |> List.map snd
        |> List.reduce (+)

    // Have to set register to 0 because else Visual doesn't output for data directives
    let loadRegParas = 
        {defaultParas with 
            Postlude = READMEMORY assumedMemBase
            Prelude = visualSyms + (SETREG 0 0u)
        }

    let vRegDPReg reg =
        match reg with
        | R x -> regNames.["R" + string(x)]

    let vDataRegsDPRegs vDataRegs = 
        List.map (fun (reg, value) -> (vRegDPReg reg, uint32 value)) vDataRegs
        |> Map.ofList

    let vMemDPMem vMem =
        let addrs = [assumedMemBase..4u..assumedMemBase + 48u] 
                    |> List.map WA 
                    |> List.rev
        List.zip addrs (List.map DataLoc vMem)
        |> Map.ofList

    // Convert Visual datapath into one that I can use
    let vDataToUsefulData (vData : VisOutput) : DataPath<CommonTop.Instr> = 
        {
            Fl = {
                    N = vData.State.VFlags.FN
                    Z = vData.State.VFlags.FN
                    C = vData.State.VFlags.FN
                    V = vData.State.VFlags.FN
            }
            Regs = vDataRegsDPRegs vData.Regs
            MM = vMemDPMem vData.State.VMemData
        }

    // Run a command src, returning the equivalent datapath ONLY reading the first 13 words of memory
    // Just going to have to assume the rest of memory works!
    let runVisualGetMem src =
        let vRes = RunVisualBaseWithLocksCached loadRegParas src 
                    |> Result.map vDataToUsefulData
        match vRes with
        | Ok res -> res
        | Error x -> failwithf "Visual failed to run with errors %A" x


    /// Remove all zeroed memory from the memory map for comparison with visUAL
    let removeZeroedMemory dp =
        let filter _ v =
            match v with
            | DataLoc x when x = 0u -> false
            | _ -> true
        {dp with MM = Map.filter filter dp.MM}

    let compareDpDataMem (dp1 : DataPath<CommonTop.Instr>) (dp2 : DataPath<CommonTop.Instr>) =
        let dp1' = removeZeroedMemory dp1
        let dp2' = removeZeroedMemory dp2
        let dataMem dp =
            Map.map (fun _ v -> 
                match v with
                | DataLoc x -> x
                | _ -> 0u
            ) dp.MM
        (dataMem dp1') = (dataMem dp2')
