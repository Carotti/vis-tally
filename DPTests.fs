module DPTests

    open VisualTest.VCommon
    open VisualTest.VLog
    open VisualTest.Visual
    open VisualTest.VTest
    open VisualTest.VData


    open CommonData
    open CommonLex
    open CommonTop

    open Test
    open DP

    open DPExecution
    open Execution
    open ExecutionTop
    open Expecto
    open Helpers
    open FsCheck

    let shiftTests = 
        [
            "mov r0, #4";
            "lsl r1, r0, #3";
            "mov r4, #0xf";
            "mvn r5, r4";
            "lsr r7, r5, #0b10";  
        ]

    let constructParam flags regs = {defaultParas with InitFlags = flags; InitRegs = regs}

    let paramsToDataPath (param: Params) : DataPath<CommonTop.Instr> = 
        let flags = {
                        N = param.InitFlags.FN;
                        C = param.InitFlags.FC;
                        Z = param.InitFlags.FZ;
                        V = param.InitFlags.FV;
                    }
        let createRegs = 
            List.zip [0u..14u]
            >> List.map (fun (r, v) -> (makeRegFromNum r, v))
            >> Map.ofList

        let initRegs vals =
            let noPC = createRegs vals
            Map.add R15 0u noPC

        let regs = initRegs param.InitRegs
        let mem = Map.empty<WAddr,MemLoc<CommonTop.Instr>>
        {Fl = flags; Regs = regs; MM = mem};
    
    let makeDP (input: string) = 
        match parseLine None (WA 0u) (uppercase input) with
        | Ok parsed -> parsed
        | _ -> 
            "ARE WE HERE!" |> qp
            failwithf "GAh!"
    
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
        
    let runDP param input = execute (makeDP input) (testDataPath param)
    // let runDP param input = execute (makeDP input) (paramsToDataPath param)
    let hopeRegs param ins = ins |> (runDP param >> returnCpuDataRegs)
    let hopeFlags param ins = ins |> (runDP param >> returnCpuDataFlags)
    let fateRegs param ins = ins |> ((returnVisualCpuData param) >> returnCpuDataRegs)
    let fateFlags param ins = ins |> ((returnVisualCpuData param) >> returnCpuDataFlags)


    let runListDP dp input = listExecute dp input   



    let hopeRegsList param ins = ins |> List.map (hopeRegs param)



    let hopeFlagsList param ins = ins |> List.map (hopeFlags param)
    let fateRegsList param ins = ins |> List.map (fateRegs param)
    let fateFlagsList param ins = ins |> List.map (fateFlags param)
    
    let removePC hope fate =
        let aNewHope = Map.remove R15 hope
        let aNewFate = Map.remove R15 fate
        aNewHope, aNewFate

    let unitTest name input (hope: Map<RName,uint32> * bool list) (fate: Map<RName,uint32> * bool list) =
        let rhope, rfate = removePC (fst hope) (fst fate)
        let nhope = rhope, (snd hope)
        let nfate = rfate, (snd fate)
        testCase name <| fun () ->
            fst nhope |> Map.toList |> List.map (fun (r, v) -> printfn "%A : %x" r v) |> ignore
            fst nfate |> Map.toList |> List.map (fun (r, v) -> printfn "%A : %x" r v) |> ignore
            snd nhope |> qp |> ignore
            snd nfate |> qp |> ignore
            Expect.equal nhope nfate input
    
    let unitTestList name input (hopeLst: (Map<RName,uint32> * bool list) list) (fateLst: (Map<RName,uint32> * bool list) list) = 
        let combined =
            List.zip (List.map (fst) hopeLst) (List.map (fst) fateLst)
            |> List.map (fun (a, b) -> removePC a b)
        let newHopeReg = List.map fst combined
        let newFateReg = List.map snd combined
        let newHopeFlags = List.map snd hopeLst
        let newFateFlags = List.map snd fateLst
        let nhope = List.zip newHopeReg newHopeFlags
        let nfate = List.zip newFateReg newFateFlags
        testCase name <| fun () ->
            nhope |> List.map (fst >> Map.toList >> List.map (fun (r, v) -> printfn "%A : %x" r v)) |> ignore
            nfate |> List.map (fst >> Map.toList >> List.map (fun (r, v) -> printfn "%A : %x" r v)) |> ignore
            nhope |> List.map (snd >> qp) |> ignore
            nfate |> List.map (snd >> qp) |> ignore
            Expect.equal nhope nfate input

    let visualTest name input initReg param = 
        unitTest name input
        <| (hopeRegs initReg input, hopeFlags initReg input) 
        <| (fateRegs param input, fateFlags param input) 

    let visualTestList name input (lst: string list) initReg param =
        unitTestList name input 
        <| List.zip (hopeRegsList initReg lst) (hopeFlagsList initReg lst)
        <| List.zip (fateRegsList param lst) (fateFlagsList param lst)



    let zeroParam = {defaultParas with InitRegs = List.map (fun _i -> 0u) [0..14]}
    let zeros = List.map (fun _ -> 0u) [0..14]

    [<Tests>]
    let visualTests =
        testList "DP Tests compared to visual, let us pray..." [
            testList "MOV unit tests" [
                visualTest "MOV hex 1a" "MOV R4, #0xa7" zeros zeroParam;
                visualTest "MOV HEX 1a" "MOV R2, #0xB2" zeros zeroParam;
                visualTest "MOV hex 1b" "MOV R3, #0Xa7" zeros zeroParam;
                visualTest "MOV HEX 1b" "MOV R3, #0XB2" zeros zeroParam;
                visualTest "MOV hex 2a" "MOV R4, #&8f" zeros zeroParam;
                visualTest "MOV HEX 2b" "MOV R4, #&FF" zeros zeroParam;
                visualTest "MOV binary" "MOV R7, #0b01011100" zeros zeroParam;
                visualTest "MOV BINARY" "MOV R7, #0B01110011" zeros zeroParam;
                visualTest "MOV decimal" "MOV R1, #5" zeros zeroParam;
            ]
            testList "MOVS unit tests" [
                visualTest "MOVS with num" "MOVS R3, #5" zeros zeroParam;
                visualTest "MOVS with reg" "MOVS R5, r6"zeros  zeroParam;
                visualTest "MOVS with hex" "MOVS R7, #0xff"zeros  zeroParam;
                visualTest "MOVS with zero" "MOVS R6, #0"zeros  zeroParam;
            ]
            testList "MVN unit tests" [
                visualTest "MVN hex 1a" "MVN R4, #0xa7" zeros zeroParam;
                visualTest "MVN HEX 1a" "MVN R2, #0xB2" zeros zeroParam;
                visualTest "MVN hex 1b" "MVN R3, #0Xa7" zeros zeroParam;
                visualTest "MVN HEX 1b" "MVN R3, #0XB2" zeros zeroParam;
                visualTest "MVN hex 2a" "MVN R4, #&8f" zeros zeroParam;
                visualTest "MVN HEX 2b" "MVN R4, #&FF" zeros zeroParam;
                visualTest "MVN binary" "MVN R7, #0b01011100" zeros zeroParam;
                visualTest "MVN BINARY" "MVN R7, #0B01110011" zeros zeroParam;
                visualTest "MVN decimal" "MVN R1, #5" zeros zeroParam;
            ]
            testList "MVNS unit tests" [
                visualTest "MVNS with 0" "MVNS r6, #0" zeros zeroParam;
                visualTest "MVNS with 0xff" "MVNS r3, #0xff" zeros zeroParam;
            ]
            testList "LSL unit tests" [
                visualTest "LSL basic" "LSL r2, r1, #5" [0u..10u..140u] defaultParas;
                visualTest "LSL large shift" "LSL r4, r1, #100" [0u..10u..140u] defaultParas;
                visualTest "LSLS no shift" "LSL r6, r1, #0" [0u..10u..140u] defaultParas;
                visualTest "LSL reg shift" "LSL r3, r2, r1" [0u..10u..140u] defaultParas;
                visualTest "LSLS reg with 0 shift" "LSL r10, r2, r0" [0u..10u..140u] defaultParas;
            ]
            testList "ASR unit tests" [
                visualTest "ASRS basic" "ASR r8, r1, #0x5" [0u..10u..140u] defaultParas;
                visualTest "ASR large shift" "ASR r4, r1, #100" [0u..10u..140u] defaultParas;
                visualTest "ASRS no shift" "ASR r7, r1, #0b0" [0u..10u..140u] defaultParas;
                visualTest "ASR reg shift" "ASR r9, r2, r1" [0u..10u..140u] defaultParas;
                visualTest "ASR reg with 0 shift" "ASR r10, r2, r0" [0u..10u..140u] defaultParas;
            ]
            testList "LSR unit tests" [
                visualTest "LSR basic" "LSR r8, r1, #0x5" [0u..10u..140u] defaultParas;
                visualTest "LSRS large shift" "LSR r4, r1, #100" [0u..10u..140u] defaultParas;
                visualTest "LSR no shift" "LSR r7, r1, #0b0" [0u..10u..140u] defaultParas;
                visualTest "LSRS reg shift" "LSR r9, r2, r1" [0u..10u..140u] defaultParas;
                visualTest "LSR reg with 0 shift" "LSR r10, r2, r0" [0u..10u..140u] defaultParas;
            ]
            testList "ROR unit tests" [
                visualTest "ROR basic" "ROR r8, r1, #0x5" [0u..10u..140u] defaultParas;
                visualTest "RORS large shift" "ROR r4, r1, #100" [0u..10u..140u] defaultParas;
                visualTest "ROR no shift" "ROR r7, r1, #0b0" [0u..10u..140u] defaultParas;
                visualTest "RORS reg shift" "ROR r9, r2, r1" [0u..10u..140u] defaultParas;
                visualTest "ROR reg with 0 shift" "ROR r10, r2, r0" [0u..10u..140u] defaultParas;
            ]
            testList "RRX unit tests" [
                visualTest "RRX basic" "RRX r8, r4" [0u..10u..140u] defaultParas;
                visualTest "RRX large shift" "RRX r4, r7" [0u..10u..140u] defaultParas;
                visualTest "RRXS no shift" "RRX r7, r10" [0u..10u..140u] defaultParas;
                visualTest "RRXS reg shift" "RRX r9, r2" [0u..10u..140u] defaultParas;
                visualTest "RRX reg with 0 shift" "RRX r10, r2" [0u..10u..140u] defaultParas;
            ]
        ]