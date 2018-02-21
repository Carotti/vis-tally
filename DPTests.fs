module DPTests

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

    let shiftTests = 
        [
        "LSL R0, R1, #2";
        "LSLS r0, r1, #0b101";
        "LSL r0, r1, #0xe";
        "LSLS r0, r1, #&f";
        "LSL R0, R1, R2";
        "LSR R0, R1, #2";
        "LSRS r0, r1, #0b101";
        "LSR r0, r1, #0xe";
        "LSRS r0, r1, #&f";
        "LSR R0, R1, R2";
        "ASRS R0, R1, #2";
        "ASR r0, r1, #0b101";
        "ASRS r0, r1, #0xe";
        "ASR r0, r1, #&f";
        "ASR R0, R1, R2";
        "RORS R0, R1, #2";
        "ROR r0, r1, #0b101";
        "ROR r0, r1, #0xe";
        "RORS r0, r1, #&f";
        "ROR R0, R1, R2"; 
        "RRXS R0, R1";
        "RRXS R12, R12";
        "LDRB r0, [r1, r2]!"
        "MOV r0, r1";
        "MOVS r1, r1";
        "MOV r3, #4";
        "MVNS r4, #0x56";
        "MVN r6, r7";
        ]

    let makeDP input = 
        match parseLine None (WA 0u) input with
        | Ok parsed -> parsed
        | _ -> failwithf "Some error" 
        
    let runDP input = execute (makeDP input) initDataPath

    let hopeRegs ins = (runDP ins |> returnCpuDataRegs)

    let hopeFlags ins = (runDP ins |> returnCpuDataFlags)

    let fateRegs ins = (returnVisualCpuData ins |> returnCpuDataRegs)

    let fateFlags ins = (returnVisualCpuData ins |> returnCpuDataFlags)

    let myDestinyDP ins = 
       hopeRegs ins = fateRegs ins

    let unitTest name input hope fate =
        testCase name <| fun () ->
            input |> qp |> ignore
            fst hope |> Map.toList |> qp |> ignore
            fst fate |> Map.toList |> qp |> ignore
            snd hope |> qp |> ignore
            snd fate |> qp |> ignore
            Expect.equal hope fate input

    let visualTest name input = 
        unitTest name input <| (hopeRegs input, hopeFlags input) <| (fateRegs input, fateFlags input) 

    [<Tests>]
    let visualTests =
        testList "DP Tests compared to visual, let us pray..." [
            testList "MOV unit tests" [
                visualTest "MOV hex 1a" "MOV R4, #0xa7";
                visualTest "MOV HEX 1a" "MOV R2, #0xB2";
                visualTest "MOV hex 1b" "MOV R3, #0Xa7";
                visualTest "MOV HEX 1b" "MOV R3, #0XB2";
                visualTest "MOV hex 2a" "MOV R4, #&8f";
                visualTest "MOV HEX 2b" "MOV R4, #&FF";
                visualTest "MOV binary" "MOV R7, #0b01011100";
                visualTest "MOV BINARY" "MOV R7, #0B01110011";
                visualTest "MOV decimal" "MOV R1, #5";
            ]
            testList "MOVS unit tests" [
                visualTest "MOVS with 0" "MOVS R3, #0";
            ]
        ]