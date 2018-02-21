module Test

    open Expecto
    open Helpers
    open VisualTest.VCommon
    open VisualTest.VLog
    open VisualTest.Visual
    open VisualTest.VTest
    open VisualTest.VData
    open System.Threading
    open System.IO
    open CommonData
    // open TestShifts

    // from given files
    let memReadBase = 0x1000u
    let expectoConfig = { Expecto.Tests.defaultConfig with 
                            parallel = testParas.Parallel
                            parallelWorkers = 6 // try increasing this if CPU use is less than 100%
                    }
    let fsConfig = {FsCheckConfig.defaultConfig with replay = Some (0,0); maxTest = 100}

    let visualTestProperty name test = testPropertyWithConfig fsConfig name test
    let runVisualTests () = 
        initCaches testParas
        let rc = runTestsInAssembly expectoConfig [||]
        finaliseCaches testParas
        rc // return an integer exit code - 0 if all tests pass    

    let visualToReg = function
        | R reg -> makeReg (makeRegFn reg)
        
    let visualToRegs vRegs = 
        List.map (fun (rOut, rInt) -> (visualToReg rOut, rInt |> uint32)) vRegs
        |> Map.ofList
    
    // make an address list from base address of data 0x100 
    // up to 0x200 by 0x4 each time
    let visualToMem vMem = 
        let alst = 
            [memReadBase..word..memReadBase + 0x30u] // need this to stop list complaints
            |> List.map WA
            |> List.rev
        List.zip alst (List.map DataLoc vMem)
        |> Map.ofList

    let returnData _ d =
        match d with
        | DataLoc dl -> dl
        | _ -> 0u

    // pretty standard making cpuData
    let visualToDataPath visual = 
        let flags = {
                        N = visual.State.VFlags.FN; 
                        Z = visual.State.VFlags.FZ;
                        C = visual.State.VFlags.FC;
                        V = visual.State.VFlags.FV;
                    }
        let regs = visualToRegs visual.Regs
        let mem = visualToMem visual.State.VMemData
        {Fl = flags; Regs = regs; MM = mem}
    
    let returnVisualCpuData src = 
        let vRes = RunVisualBaseWithLocksCached defaultParas src 
                    |> Result.map visualToDataPath
        match vRes with
        | Ok res -> res
        | Error x -> failwithf "Visual failed to run with errors %A" x

    let returnCpuDataMem (cpuData: DataPath<CommonTop.Instr>) = 
        Map.map returnData cpuData.MM

    let returnCpuDataRegs (cpuData: DataPath<CommonTop.Instr>) =
        cpuData.Regs
    
    let returnCpuDataFlags (cpuData: DataPath<CommonTop.Instr>) =
        cpuData.Fl