module MemTests
    open VisualTest.VCommon
    open VisualTest.VTest
    open CommonData
    open CommonTop
    open Test
    open ExecutionTop
    open Expecto
    open Helpers

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
        let mem = Map.ofList []
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
    let fateRegs param ins = ins |> ((returnVisualCpuData param) >> returnCpuDataRegs)
    let fateMem param ins = ins |> ((returnVisualCpuData param) >> returnCpuDataMem)

    let rec runListDP param input = listExecute (paramsToDataPath param) input   

    let rec hopeRegsList param lst = 
        match lst with
        | head :: tail ->
            let regs = head |> (hopeRegs param)
            let vals = List.map snd (regs |> Map.toList)
            let newParam = constructParamRegs vals
            hopeRegsList newParam tail
        | [] -> hopeRegs param "MOV R0, R0"
        
    let rec hopeMemList param lst = 
        match lst with
        | head :: tail ->
            hopeMem param "MOV R0, R0"
        | [] -> hopeMem param "MOV R0, R0"

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

    let rec fateMemList param lst =
        match lst with
        | head :: tail -> fateMem param "MOV R0, R0"
            
        | [] -> fateMem param "MOV R0, R0"

    
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
        <| (hopeRegs initReg input, hopeMem initReg input) 
        <| (fateRegs param input, fateMem param input) 

    let visualTestList name input (lst: string list) initReg param =
        unitTestList name input 
        <| ((hopeRegsList initReg lst), (hopeMemList initReg lst))
        <| (fateRegsList param lst, fateMemList param lst)

    let zeroParam = {defaultParas with InitRegs = List.map (fun _i -> 0u) [0..14]}