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

    let hope ins = (runDP ins |> returnCpuDataRegs)

    let fate ins = (returnVisualCpuData ins |> returnCpuDataRegs)

    let sameAnswerDP ins = 
       hope ins = fate ins

    let unitTest name input hope fate =
        testCase name <| fun () ->
            hope |> Map.toList |> qp |> ignore
            fate |> Map.toList |> qp |> ignore
            Expect.equal hope fate input
            
    
    let visualTest name input = 
        unitTest name input <| (hope input) <| (fate input) 

    [<Tests>]
    let visualTests =
        testList "DP Tests compared to visual, let us pray..." [
            testList "MOV unit tests" [
                visualTest "MOV decimal" "MOV R0, #4";
                visualTest "MOV hex 1a" "MOV R4, #0xa7";
                visualTest "MOV HEX 1a" "MOV R2, #0xB2";
                visualTest "MOV hex 1b" "MOV R3, #0Xa7";
                visualTest "MOV HEX 1b" "MOV R3, #0XB2";
                visualTest "MOV hex 2a" "MOV R4, #&8f";
                visualTest "MOV HEX 2b" "MOV R4, #&FF";
                visualTest "MOV binary" "MOV R7, #0b01011100";
                visualTest "MOV BINARY" "MOV R7, #0B01110011";
            ]
        ]