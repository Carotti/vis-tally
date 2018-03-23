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

    [<Tests>]
    let allTests =
        testList "Tests" [
            sanityTests
            directiveTests
            branchTests
        ]