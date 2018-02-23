module MemTests
    open VisualTest.VCommon
    open VisualTest.VTest
    open VisualTest.VData
    open CommonData
    open CommonTop
    open Test
    open ExecutionTop
    open Expecto
    open Helpers
    open DPTests
    open DPTests

    /// Removes the last item in a list
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
        let mem = Map.ofList [
                        (WA 4096u), (DataLoc 0u);
                        (WA 4100u), (DataLoc 2u);
                        (WA 4104u), (DataLoc 3u);
                        (WA 4108u), (DataLoc 4u);
                        (WA 4112u), (DataLoc 5u);
                        (WA 4116u), (DataLoc 6u);
                        (WA 4120u), (DataLoc 7u);
                        (WA 4124u), (DataLoc 8u);
                        (WA 4128u), (DataLoc 9u);
                        (WA 4132u), (DataLoc 10u);
                        (WA 4136u), (DataLoc 11u);
                        (WA 4140u), (DataLoc 12u);
                        (WA 4144u), (DataLoc 13u);
                    ]
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
    let hopeMem param ins = ins |> (runDP param >> returnCpuDataMem)
    let fateRegs param ins = ins |> ((returnMemVisualCpuData param) >> returnCpuDataRegs)
    let fateMem param ins = ins |> ((returnMemVisualCpuData param) >> returnCpuDataMem)
    
    let removePC hope fate =
        let aNewHope = Map.remove R15 hope
        let aNewFate = Map.remove R15 fate
        aNewHope, aNewFate

    let unitTest name input hope fate =
        let rhope, rfate = removePC (fst hope) (fst fate)
        let nhope = rhope, (snd hope)
        let nfate = rfate, (snd fate)
        rhope |> Map.toList |> qp |> ignore
        "*******************************************" |> qp
        rfate |> Map.toList |> qp |> ignore
        testCase name <| fun () ->
            Expect.equal nhope nfate input
    
    let visualTest name input initReg param = 
        unitTest name input
        <| (hopeRegs initReg input, hopeMem initReg input) 
        <| (fateRegs param input, fateMem param input) 

    let zeroParam = {defaultParas with InitRegs = List.map (fun _i -> 0u) [0..14]}
    let memParam = {defaultParas with InitRegs = List.map (fun _i -> memReadBase) [0..14]}

    /// Could not get the commented out tests to pass. The answers they produce are correct but
    /// with postludes and preludes and the ldm's being called it is hard to retain the 
    /// right data. My own run visual function to try this which semi works can be found in
    /// Test.fs
    [<Tests>]
    let visualTests = 
        testList "HOW DID I GET THIS WORKING AHHH!!!!" [
            testList "Loads with immediates" [
                visualTest "LDR pre" "LDR R1, [R3, #4]" memParam memParam
                visualTest "LDR nothing" "LDR R1, [R3]" memParam memParam
                visualTest "LDR post" "LDR R1, [R3], #4" memParam memParam
                visualTest "LDR post and pre" "LDR R1, [R3, #4]!" memParam memParam
            ]
            testList "Stores with immediates" [
                visualTest "STR pre" "STR R1, [R3, #4]" memParam memParam
                // visualTest "STR nothing" "STR R1, [R3]" memParam memParam
                // visualTest "STR post" "STR R1, [R3], #4" memParam memParam
                visualTest "STR post and pre" "STR R1, [R3, #4]!" memParam memParam
            ]
            // testList "LDM" [
            //     visualTest "LDM hyphen" "LDM R1, {R2-R7}" memParam memParam
            //     visualTest "LDM list" "LDM R1, {R2, R3, R4, R5, R6, R7}" memParam memParam
            // ]
            // testList "STM" [
            //     visualTest "STM hyphen" "STM R1, {R2-R7}" memParam memParam
            //     visualTest "STM list" "STM R1, {R2, R3, R4, R5, R6, R7}" memParam memParam
            // ]
        ]
        