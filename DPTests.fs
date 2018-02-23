module DPTests
    open VisualTest.VCommon
    open VisualTest.VTest
    open CommonData
    open CommonTop
    open Test
    open ExecutionTop
    open Expecto
    open Helpers

    let removeTail lst = 
        match List.rev lst with 
        | _ :: tail -> List.rev tail
        | _ -> failwithf "failed"

    let constructParam flags regs = {defaultParas with InitFlags = flags; InitRegs = regs}
    let constructParamRegs regs = {defaultParas with InitRegs = regs}
    let constructParamFlags flags = {defaultParas with InitFlags = flags}

    let paramsToDataPath (param: Params) : DataPath<CommonTop.Instr> = 
        let flags = {
                        N = param.InitFlags.FN;
                        C = param.InitFlags.FC;
                        Z = param.InitFlags.FZ;
                        V = param.InitFlags.FV;
                    }
        let createNewRegs = 
            List.zip [0u..14u]
            >> List.map (fun (r, v) -> (makeRegFromNum r, v))
            >> Map.ofList

        let resetRegs =
            List.zip [0u..15u]
            >> List.map (fun (r, v) -> (makeRegFromNum r, v))
            >> Map.ofList

        let initRegs vals =
            match List.length vals with
            | 15 -> 
                let noPC = createNewRegs vals
                Map.add R15 0u noPC
            | _ -> 
                resetRegs vals
            

        let regs = initRegs param.InitRegs
        let mem = Map.empty<WAddr,MemLoc<CommonTop.Instr>>
        {Fl = flags; Regs = regs; MM = mem};
    
    let makeDP (input: string) = 
        match parseLine None (WA 0u) (uppercase input) with
        | Ok parsed -> parsed
        | _ -> failwithf "Parsing on instruction failed"
    
    let parseInstr input =
        parseLine None (WA 0u) (uppercase input)
    let prettyPrint cpuData =
        cpuData.Regs |> Map.toList |> qpl |> ignore
        cpuData.Fl |> qp |> ignore
        cpuData.MM |> Map.toList |> qpl |> ignore

    let listExecute cpuData lst = 
        prettyPrint cpuData
        let rec listExecute' cpuData' lst' = 
            match lst' with
            | head :: tail -> 
                match head with
                | Ok instr ->
                    instr |> qp |> ignore
                    execute instr cpuData'
                    |> function    
                    | cpuData'' ->
                          prettyPrint cpuData''
                          listExecute' cpuData'' tail
                | Error err ->
                    err |> qp
                    listExecute' cpuData' tail
            | [] -> cpuData'
        List.map parseInstr lst
        |> listExecute' cpuData
        
    let runDP param input = execute (makeDP input) (paramsToDataPath param)
    let hopeRegs param ins = ins |> (runDP param >> returnCpuDataRegs)
    let hopeFlags param ins = ins |> (runDP param >> returnCpuDataFlags)
    let fateRegs param ins = ins |> ((returnVisualCpuData param) >> returnCpuDataRegs)
    let fateFlags param ins = ins |> ((returnVisualCpuData param) >> returnCpuDataFlags)

    let rec runListDP param input = listExecute (paramsToDataPath param) input   

    let rec hopeRegsList param lst = 
        match lst with
        | head :: tail ->
            let regs = head |> (hopeRegs param)
            let vals = List.map snd (regs |> Map.toList)
            let newParam = constructParamRegs vals
            hopeRegsList newParam tail
        | [] -> hopeRegs param "MOV R0, R0"
        
    let rec hopeFlagsList param lst = 
        match lst with
        | head :: tail ->
            let flags = head |> (hopeFlags param)
            let rightFlags = 
                {
                    VisualTest.VCommon.Flags.FN = flags.N;  
                    VisualTest.VCommon.Flags.FC = flags.C;
                    VisualTest.VCommon.Flags.FZ = flags.Z; 
                    VisualTest.VCommon.Flags.FV = flags.V; 
                }
            let newParam = constructParamFlags rightFlags
            hopeFlagsList newParam tail
        | [] -> hopeFlags param "MOV R0, R0"

    let rec fateRegsList param lst =
        let validLength =
            match List.length param.InitRegs with
            | 15 -> true
            | _ -> false
        match lst with
        | head :: tail ->
            match validLength with
            | true -> 
                let regs = head |> (fateRegs param)
                let vals = List.map snd (regs |> Map.toList)
                let newParam = constructParamRegs vals
                fateRegsList newParam tail
            | false -> 
                let newList = removeTail param.InitRegs
                let noPCParam = constructParamRegs newList
                let regs = head |> (fateRegs noPCParam)
                let vals = List.map snd (regs |> Map.toList)
                let newParam = constructParamRegs vals
                fateRegsList newParam tail           
        | [] -> 
            match validLength with
            | true ->
                fateRegs param "MOV R0, R0"
            | false ->
                let newList = removeTail param.InitRegs
                let noPCParam = constructParamRegs newList
                fateRegs noPCParam "MOV R0, R0"

    let rec fateFlagsList param lst =
        match lst with
        | head :: tail ->
            let flags = head |> (hopeFlags param)
            let rightFlags = 
                {
                    VisualTest.VCommon.Flags.FN = flags.N;  
                    VisualTest.VCommon.Flags.FC = flags.C;
                    VisualTest.VCommon.Flags.FZ = flags.Z; 
                    VisualTest.VCommon.Flags.FV = flags.V; 
                }
            let newParam = constructParamFlags rightFlags
            fateFlagsList newParam tail
        | [] -> fateFlags param "MOV R0, R0"

    
    let removePC hope fate =
        let aNewHope = Map.remove R15 hope
        let aNewFate = Map.remove R15 fate
        aNewHope, aNewFate

    let unitTest name input hope fate =
        let rhope, rfate = removePC (fst hope) (fst fate)
        let nhope = rhope, (snd hope)
        let nfate = rfate, (snd fate)
        testCase name <| fun () ->
            Expect.equal nhope nfate input
    
    let unitTestList name input hope fate = 
        let rhope, rfate = removePC (fst hope) (fst fate)
        let nhope = rhope, (snd hope)
        let nfate = rfate, (snd fate)
        testCase name <| fun () ->
            Expect.equal nhope nfate input

    let visualTest name input initReg param = 
        unitTest name input
        <| (hopeRegs initReg input, hopeFlags initReg input) 
        <| (fateRegs param input, fateFlags param input) 

    let visualTestList name input (lst: string list) initReg param =
        unitTestList name input 
        <| ((hopeRegsList initReg lst), (hopeFlagsList initReg lst))
        <| (fateRegsList param lst, fateFlagsList param lst)

    let zeroParam = {defaultParas with InitRegs = List.map (fun _i -> 0u) [0..14]}

    [<Tests>]
    let visualTests =
        let shiftTest1 = 
            [
                "mov r0, #4";
                "lsl r1, r0, #3";
                "mov r4, #0xf";
                "mvn r5, r4";
                "lsr r7, r5, #0b10";  
            ]
        let shiftTest2 = 
            [
                "MVN R0, #0";
                "MVN R0, R0";
                "MVN R0, R0";
                "MVN R1, #0xFFFFFFFF";
                "MVN R2, #10";
            ]
        let shiftTest3 =
            [
                "MOV R0, #0x100";
                "MOV R1, #201";
                "MOV R3, R0";
                "MOV R0, R1";
                "MOV R1, R3";
            ]
        let shiftTest4 = 
            [
                "mov r0, #0b1";
                "mov r1, #2";
                "mov r3, #3";
                "lsl r4, r0, r3";
                "rrxs r0, r0";
                "asrs r3, r4, #1";
                "ror r7, r2, r1";
            ]

        testList "DP Tests compared to visual, let us pray..." [
            testList "MOV unit tests" [
                visualTest "MOV hex 1a" "MOV R4, #0xa7" zeroParam zeroParam;
                visualTest "MOV HEX 1a" "MOV R2, #0xB2" zeroParam zeroParam;
                visualTest "MOV hex 1b" "MOV R3, #0Xa7" zeroParam zeroParam;
                visualTest "MOV HEX 1b" "MOV R3, #0XB2" zeroParam zeroParam;
                visualTest "MOV hex 2a" "MOV R4, #&8f" zeroParam zeroParam;
                visualTest "MOV HEX 2b" "MOV R4, #&FF" zeroParam zeroParam;
                visualTest "MOV binary" "MOV R7, #0b01011100" zeroParam zeroParam;
                visualTest "MOV BINARY" "MOV R7, #0B01110011" zeroParam zeroParam;
                visualTest "MOV decimal" "MOV R1, #5" zeroParam zeroParam;
            ]
            testList "MOVS unit tests" [
                visualTest "MOVS with num" "MOVS R3, #5" zeroParam zeroParam;
                visualTest "MOVS with reg" "MOVS R5, r6"zeroParam  zeroParam;
                visualTest "MOVS with hex" "MOVS R7, #0xff"zeroParam  zeroParam;
                visualTest "MOVS with zero" "MOVS R6, #0"zeroParam  zeroParam;
            ]
            testList "MVN unit tests" [
                visualTest "MVN hex 1a" "MVN R4, #0xa7" zeroParam zeroParam;
                visualTest "MVN HEX 1a" "MVN R2, #0xB2" zeroParam zeroParam;
                visualTest "MVN hex 1b" "MVN R3, #0Xa7" zeroParam zeroParam;
                visualTest "MVN HEX 1b" "MVN R3, #0XB2" zeroParam zeroParam;
                visualTest "MVN hex 2a" "MVN R4, #&8f" zeroParam zeroParam;
                visualTest "MVN HEX 2b" "MVN R4, #&FF" zeroParam zeroParam;
                visualTest "MVN binary" "MVN R7, #0b01011100" zeroParam zeroParam;
                visualTest "MVN BINARY" "MVN R7, #0B01110011" zeroParam zeroParam;
                visualTest "MVN decimal" "MVN R1, #5" zeroParam zeroParam;
            ]
            testList "MVNS unit tests" [
                visualTest "MVNS with 0" "MVNS r6, #0" zeroParam zeroParam;
                visualTest "MVNS with 0xff" "MVNS r3, #0xff" zeroParam zeroParam;
            ]
            testList "LSL unit tests" [
                visualTest "LSL basic" "LSL r2, r1, #5" defaultParas defaultParas;
                visualTest "LSL large shift" "LSL r4, r1, #100" defaultParas defaultParas;
                visualTest "LSLS no shift" "LSL r6, r1, #0" defaultParas defaultParas;
                visualTest "LSL reg shift" "LSL r3, r2, r1" defaultParas defaultParas;
                visualTest "LSLS reg with 0 shift" "LSL r10, r2, r0" defaultParas defaultParas;
            ]
            testList "ASR unit tests" [
                visualTest "ASRS basic" "ASR r8, r1, #0x5" defaultParas defaultParas;
                visualTest "ASR large shift" "ASR r4, r1, #100" defaultParas defaultParas;
                visualTest "ASRS no shift" "ASR r7, r1, #0b0" defaultParas defaultParas;
                visualTest "ASR reg shift" "ASR r9, r2, r1" defaultParas defaultParas;
                visualTest "ASR reg with 0 shift" "ASR r10, r2, r0" defaultParas defaultParas;
            ]
            testList "LSR unit tests" [
                visualTest "LSR basic" "LSR r8, r1, #0x5" defaultParas defaultParas;
                visualTest "LSRS large shift" "LSR r4, r1, #100" defaultParas defaultParas;
                visualTest "LSR no shift" "LSR r7, r1, #0b0" defaultParas defaultParas;
                visualTest "LSRS reg shift" "LSR r9, r2, r1" defaultParas defaultParas;
                visualTest "LSR reg with 0 shift" "LSR r10, r2, r0" defaultParas defaultParas;
            ]
            testList "ROR unit tests" [
                visualTest "ROR basic" "ROR r8, r1, #0x5" defaultParas defaultParas;
                visualTest "RORS large shift" "ROR r4, r1, #100" defaultParas defaultParas;
                visualTest "ROR no shift" "ROR r7, r1, #0b0" defaultParas defaultParas;
                visualTest "RORS reg shift" "ROR r9, r2, r1" defaultParas defaultParas;
                visualTest "ROR reg with 0 shift" "ROR r10, r2, r0" defaultParas defaultParas;
            ]
            testList "RRX unit tests" [
                visualTest "RRX basic" "RRX r8, r4" defaultParas defaultParas;
                visualTest "RRX large shift" "RRX r4, r7" defaultParas defaultParas;
                visualTest "RRXS no shift" "RRX r7, r10" defaultParas defaultParas;
                visualTest "RRXS reg shift" "RRX r9, r2" defaultParas defaultParas;
                visualTest "RRX reg with 0 shift" "RRX r10, r2" defaultParas defaultParas;
            ]
            testList "LISTS" [
                visualTestList "Shift Test 1" "1" shiftTest1 zeroParam zeroParam
                visualTestList "Shift Test 2" "2" shiftTest2 zeroParam zeroParam
                visualTestList "Shift Test 3" "3" shiftTest3 zeroParam zeroParam
                visualTestList "Shift Test 4" "4" shiftTest4 zeroParam zeroParam
            ]
        ]