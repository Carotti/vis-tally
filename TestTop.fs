module TestTop

    open VisualTest.Visual
    open VisualTest.VTest
    open VisualTest.VData
    open VisualTest.VCommon

    open CommonData
    open CommonTop

    open Expecto

    open TestFormats
    open VisualTest.VCommon

    let expectoConfig = { Expecto.Tests.defaultConfig with 
                            parallel = testParas.Parallel
                            parallelWorkers = 6 // try increasing this if CPU use is less than 100%
                }

    let runVisualTests () = 
        initCaches testParas
        let rc = runTestsInAssembly expectoConfig [||]
        finaliseCaches testParas
        rc // return an integer exit code - 0 if all tests pass

    let assumedMemBase = 0x1000u

    let visualSyms = 
        Map.map (fun key v -> sprintf "%s EQU %d\n" key v) ts
        |> Map.toArray
        |> Array.toList
        |> List.map snd
        |> List.reduce (+)

    // Have to set register because else Visual doesn't output for data directives
    let loadRegParas = 
        {defaultParas with 
            Postlude = READMEMORY assumedMemBase
            Prelude = visualSyms + "visLabelEnd MOV R0, R0\n"
        }

    let fsConfig = {
            FsCheckConfig.defaultConfig with
                replay = Some (0,0)
                maxTest = 100
            }

    let testPropertyVis name tst = testPropertyWithConfig fsConfig name tst

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

    let runVisualRun paras src =
        let vRes = RunVisualBaseWithLocksCached paras src
                    |> Result.map vDataToUsefulData
        match vRes with
        | Ok res -> res
        | Error x -> failwithf "Visual failed to run with errors: %A" x

    // Run a command src, returning the equivalent datapath ONLY reading the first 13 words of memory
    // Just going to have to assume the rest of memory works!
    let runVisualGetMem = runVisualRun loadRegParas

    let runVisualWithFlags n c z v =
        runVisualRun {defaultParas with 
                        InitFlags = 
                            {
                                FN = n
                                FC = c
                                FZ = z
                                FV = v
                            }
                    }


    /// Remove all zeroed memory from the memory map for comparison with visUAL
    let removeZeroedMemory mm =
        let filter _ v =
            match v with
            | DataLoc x when x = 0u -> false
            | _ -> true
        Map.filter filter mm

    let getDpDataMem (dp : DataPath<CommonTop.Instr>) =
        let getData _ v =
            match v with
            | DataLoc x -> x
            | _ -> 0u
        removeZeroedMemory dp.MM
        |> Map.map getData

    /// Construct a unit test
    let unitTest name txt expected actual =
        testCase name <| fun () ->
            Expect.equal actual expected txt

    let parseTop = parseLine (Some ts) (WA 0u)

    /// Highest level for producing a function from its text
    /// Only this can be at this level since the other functions
    /// rely on functions which are module dependent
    let produceTop resolver downcaster txt = 
        // Don't care about the word address for these instructions
        let ins = parseTop txt
        match ins with 
        | Ok top ->
            match resolver ts (downcaster top) with
            | Ok miscIns -> miscIns
            | _ -> failwithf "Invalid symbol"
        | _ -> failwithf "Invalid production of instruction"