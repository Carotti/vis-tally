module Test
    open Expecto

    open Integration
    open CommonData
    open Mono.Cecil.Cil

    type Flags =
        | N
        | Z
        | C
        | V

    let expectRegSet rNum value dp =
        Expect.equal dp.Regs.[rNum] value (sprintf "%A value" rNum)

    let expectMemSet addr value dp =
        match dp.MM.[WA addr] with
        | DataLoc x -> Expect.equal x value (sprintf "Mem Value at 0x%x" addr)
        | Code _ -> failwithf "Expecting on code memory"

    let expectFlagSet flag value dp =
        let fVal =
            match flag with
            | N -> dp.Fl.N
            | Z -> dp.Fl.Z
            | C -> dp.Fl.C
            | V -> dp.Fl.V
        Expect.equal fVal value (sprintf "%A flag" flag)

    let unitTest name code expecters =
        let result = 
            match runCode code with
            | Some x -> x
            | None -> failwithf "Error in unit test"
        testCase name <|
            (fun () -> List.fold (fun _ exp -> exp result) () expecters)

    let sanityTests =
        testList "Sanity Tests" [
            unitTest "Basic Mov" <|
            "
                mov r0, #1
            " <|
            [
                expectRegSet R0 1u
                expectRegSet R1 0u
            ]

            unitTest "Valid rotation literal" <|
            "
                mov r0, #0xff000000
            " <|
            [
                expectRegSet R0 0xff000000u
            ]

            unitTest "Zero flag test" <|
            "
                movs r0, #0
            " <|
            [
                expectFlagSet Z true
            ]
        ]

    let directiveTests =
        testList "Directives" [
            unitTest "Forward label dependence" <|
            "
                label1 EQU label2
                label2 EQU label3
                label3 EQU label4
                label4 EQU label5
                label5 EQU hello

                mov r0, #0
                hello ADR R0, label1
            " <|
            [
                expectRegSet R0 4u
            ]

            unitTest "Fill dependent on EQU" <|
            "
                some FILL amount, 5
                amount EQU 5
            " <|
            [
                expectMemSet 0x100u 0x05050505u
                expectMemSet 0x104u 0x5u
            ]

            unitTest "ADR LDR" <|
            "
                foo DCD 178, 130
                ADR R0, foo
                LDR R1, [R0]
                LDR R2, [R0, #4]
            " <|
            [
                expectRegSet R1 178u
                expectRegSet R2 130u
            ]
        ]
    
    let memoryTests = 
        testList "Memory" [
            unitTest "Testing pre-increment single store" <|
            "
                mov r0, #1
                mov r1, #2
                mov r2, #3
                mov r3, #0x100
                str r0, [r3]
                str r1, [r3, #4]
                str r2, [r3, #8]
            " <|
            [
                expectMemSet 0x100u 1u
                expectMemSet 0x104u 2u
                expectMemSet 0x108u 3u
            ]

            unitTest "Testing post-increment single store" <|
            "
                mov r0, #0xff
                mov r1, #0xfe
                mov r2, #0xfd
                mov r3, #0x100
                str r0, [r3], #4
                str r1, [r3], #4
                str r2, [r3], #4
            " <|
            [
                expectMemSet 0x100u 0xffu
                expectMemSet 0x104u 0xfeu
                expectMemSet 0x108u 0xfdu
            ]

            unitTest "Testing pre and post-increment single store" <|
            "
                mov r0, #0b01
                mov r1, #0b11
                mov r2, #0b111
                mov r3, #0x100
                str r0, [r3, #4]!
                str r1, [r3, #4]!
                str r2, [r3, #4]!
            " <|
            [
                expectMemSet 0x104u 1u
                expectMemSet 0x108u 3u
                expectMemSet 0x10Cu 7u
            ]

            unitTest "Testing single load from DCD with register pre-increment" <|
            "
                foo dcd 10, 11, 12
                adr r0, foo
                mov r4, #8
                ldr r1, [r0]
                ldr r2, [r0, #4]
                ldr r3, [r0, r4]
            " <|
            [
                expectRegSet R1 10u
                expectRegSet R2 11u
                expectRegSet R3 12u
            ]

            unitTest "Testing single load byte from DCB" <|
            "
                foo dcb 1, 2, 3, 4, 5
                adr r0, foo
                mov r4, #8
                ldrb r1, [r0]
                ldrb r2, [r0, #1]
                ldrb r3, [r0, #2]
                ldrb r4, [r0, #4]
                ldr  r5, [r0]
                ldr  r6, [r0, #4]
            " <|
            [
                expectRegSet R1 1u
                expectRegSet R2 2u
                expectRegSet R3 3u
                expectRegSet R4 5u
                expectRegSet R5 0x4030201u
                expectRegSet R6 5u
            ]

            unitTest "Testing single store byte" <|
            "
                mov r0, #0b1
                mov r1, #0b11
                mov r3, #0x100
                strb r0, [r3]
                strb r1, [r3, #2]
            " <|
            [
                expectMemSet 0x100u 0x30001u
            ]

            unitTest "Testing multiple load with suffixes" <|
            "
                foo dcd 1, 2, 3, 4, 5, 6, 7, 8
                adr r0, foo
                ldmia r0, {r1, r2}
                add r0, r0, #4
                ldmib r0, {r3-r4}
                add r0, r0, #20
                ldmda r0, {r5, r6}
                ldmdb r0, {r7-r8}
            " <|
            [
                expectRegSet R1 1u
                expectRegSet R2 2u
                expectRegSet R3 3u
                expectRegSet R4 4u
                expectRegSet R5 6u
                expectRegSet R6 7u
                expectRegSet R7 5u
            ]

            unitTest "Testing multiple store with suffixes" <|
            "
                mov r0, #0x100
                mov r1, #1
                mov r2, #2
                mov r3, #3
                mov r4, #4
                mov r5, #5
                mov r6, #6
                stmia r0, {r1, r2}
                add r0, r0, #8
                stmib r0, {r3-r4}
                add r0, r0, #20
                stmda r0, {r5, r6}
                add r0, r0, #12
                stmdb r0, {r1-r2}
            " <|
            [
                expectMemSet 0x100u 1u
                expectMemSet 0x104u 2u
                expectMemSet 0x10Cu 3u
                expectMemSet 0x110u 4u
                expectMemSet 0x118u 5u
                expectMemSet 0x11Cu 6u
                expectMemSet 0x120u 1u
                expectMemSet 0x124u 2u

            ]   
        ]

    let branchTests =
        testList "Branches" [
            unitTest "First multiple of 3 >= 100" <|
                "
                    start add r0, r0, #3
                    cmp r0, #100
                    blt start
                    end
                " <|
                [
                    expectRegSet R0 102u
                ]

            unitTest "Branchlink basic" <|
                "
                    mov r0, #100
                    bl fin
                    mov r0, #50
                    fin end
                " <|
                [
                    expectRegSet R14 8u
                    expectRegSet R0 100u
                ]

            unitTest "subroutine" <|
                "
                    mov r0, #50
                    bl double
                    bl double
                    bl double
                    end

                    double add r0, r0, r0
                    mov pc, lr
                " <|
                [
                    expectRegSet R0 400u
                ]
        ]

        // edge case tests for flags
    // the instructions used to set/clear the flags have been tested (and passed those tests!)
    // at the individual stage and so are assumed to be correct
    let flagTests =
        testList "flags" [
            unitTest "small numbers, zero" <|
                "
                    adds r0, r0, #0
                " <|
                [
                    expectRegSet R0 0u
                    expectFlagSet N false
                    expectFlagSet C false
                    expectFlagSet Z true
                    expectFlagSet V false
                ]

            unitTest "large numbers, no flags" <|
                "   
                    mov r0, #0x7e000000
                    mov r1, #0x01000000
                    adds r2, r1, r0
                " <|
                [
                    expectRegSet R2 0x7f000000u
                    expectFlagSet N false
                    expectFlagSet C false
                    expectFlagSet Z false
                    expectFlagSet V false
                ]
            
            unitTest "large numbers, overflow" <|
                "   
                    mov r0, #0x7f000000
                    mov r1, #0x01000000
                    adds r2, r1, r0
                " <|
                [
                    expectRegSet R2 0x80000000u
                    expectFlagSet N true
                    expectFlagSet C false
                    expectFlagSet Z false
                    expectFlagSet V true
                ]
            
            unitTest "large numbers, carry" <|
                "   
                    mov r0, #0xff000000
                    mov r1, #0x01000000
                    adds r2, r1, r0
                " <|
                [
                    expectRegSet R2 0x00000000u
                    expectFlagSet N false
                    expectFlagSet C true
                    expectFlagSet Z true
                    expectFlagSet V false
                ]
            
            unitTest "large numbers, zero" <|
                "   
                    mov r0, #0x14000000
                    mvn r0, r0
                    add r0, r0, #1
                    mov r1, #0x14000000
                    adds r2, r1, r0
                " <|
                [
                    expectRegSet R2 0x00000000u
                    expectFlagSet N false
                    expectFlagSet C true
                    expectFlagSet Z true
                    expectFlagSet V false
                ]
        ]
    
    // edge case tests for the flexible second operand
    // the instructions used to set/clear the flags have been tested (and passed those tests!)
    // at the individual stage and so are assumed to be correct
    let flexOp2Tests =
        testList "flex op 2" [
            unitTest "modulo 32 shifting left symmetry" <|
                "
                    adds r1, r0, #1 lsl #32 
                " <|
                [
                    expectRegSet R1 1u
                    expectFlagSet N false
                    expectFlagSet C false
                    expectFlagSet Z false
                    expectFlagSet V false
                ]
            
            unitTest "modulo 32 shifting right symmetry" <|
                "
                    adds r1, r0, #1 lsr #32 
                " <|
                [
                    expectRegSet R1 1u
                    expectFlagSet N false
                    expectFlagSet C false
                    expectFlagSet Z false
                    expectFlagSet V false
                ]

            unitTest "rotation 32 symmetry" <|
                "
                    adds r1, r0, #1 ror #32 
                " <|
                [
                    expectRegSet R1 1u
                    expectFlagSet N false
                    expectFlagSet C false
                    expectFlagSet Z false
                    expectFlagSet V false
                ]
            
            unitTest "rrx with c set" <|
                "
                    mov r0, #0xff000000
                    mov r1, #0x01000000
                    adds r2, r1, r0
                    mov r0, #0
                    adds r1, r0, r0, rrx
                " <|
                [
                    expectRegSet R1 0x80000000u
                    expectFlagSet N true
                    expectFlagSet C false
                    expectFlagSet Z false
                    expectFlagSet V false
                ]
            
            unitTest "rrx with c not set" <|
                "
                    adds r1, r0, #0 rrx
                " <|
                [
                    expectRegSet R1 0u
                    expectFlagSet N false
                    expectFlagSet C false
                    expectFlagSet Z true
                    expectFlagSet V false
                ]
            
        ]
      

    [<Tests>]
    let allTests =
        testList "Tests" [
            sanityTests
            directiveTests
            branchTests
            memoryTests
            flagTests
            flexOp2Tests
        ]