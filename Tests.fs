module Tests

open Expecto
open DP
open DPTests


/// Property based test for ADDS.
let ADDSpropTest1 =
    let compare src (r0,r1,r2) c z v =
        (visTest3Params src (r0, r1, r2) false c z v)
    testProperty "ADDS, reg, N clear"  <| (compare "ADDS r0, r1, r2")

/// Property based test for ADDS.
let ADDSpropTest2 =
    let compare src (r0,r1,r2) n c v =
        (visTest3Params src (r0, r1, r2) n c false v)
    testProperty "ADDS, reg, Z clear"  <| (compare "ADDS r0, r1, r2")

/// Property based test for ADDS.
let ADDSpropTest3 =
    let compare src n (shift:SInstr) (r0,r1,r2,r3) c z v =
        let src' = src + sInstrsStr.[shift] + " r3"
        (visTest4Params src' (r0, r1, r2, r3) n c z v)
    testProperty "ADDS, reg, shift, reg, N clear"  <| (compare "ADDS r0, r1, r2, " false)

/// Property based test for ADDS.
let ADDSpropTest4 =
    let compare src z (shift:SInstr) (r0,r1,r2,r3) n c v =
        let src' = src + sInstrsStr.[shift] + " r3"
        (visTest4Params src' (r0, r1, r2, r3) n c z v)
    testProperty "ADDS, reg, shift, reg, Z clear"  <| (compare "ADDS r0, r1, r2, " false)

// [<Tests>]

// let propTests =
//     testList "Property Based Testing"
//     [
//         ADDSpropTest1
//         ADDSpropTest2
//         ADDSpropTest3
//         ADDSpropTest4
//     ]


[<Tests>]
let ADDUnitTests =
    testList "ADD Unit Tests"
        [
            visUnitTest "ADD 1" "ADD r0, r1, r2"              [10u;11u;12u;13u]   false false false false
            visUnitTest "ADD 2" "ADD R0, R1, R2, LSL R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "ADD 3" "ADD R0, R1, R2, LSR R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "ADD 4" "ADD R0, R1, R2, ASR R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "ADD 5" "ADD R0, R1, R2, ROR R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "ADD 6" "ADD R0, R1, R2, RRX "        [10u;11u;12u;13u]   false false false false

            visUnitTest "ADD 7" "ADD R0, R1, R2"              [0x83f00000u;0x4895u;0x122u]          false false false false
            visUnitTest "ADD 11" "ADD R0, R1, R2, ROR R3"     [0x83f00000u;0x4895u;0x122u;0x1e3u]   false false false false
            visUnitTest "ADD 12" "ADD R0, R1, R2, RRX "       [0x83f00000u;0x4895u;0x122u;0x1e3u]   false false false false

            visUnitTest "ADD 13" "ADD r0, r1, r2"              [10u;11u;12u;13u]   false false false false
            visUnitTest "ADD 14" "ADD R0, R1, R2, LSL R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "ADD 15" "ADD R0, R1, R2, LSR R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "ADD 16" "ADD R0, R1, R2, ASR R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "ADD 17" "ADD R0, R1, R2, ROR R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "ADD 18" "ADD R0, R1, R2, RRX "        [10u;11u;12u;13u]   false false false false

            visUnitTest "ADD 19" "ADD R0, R1, R2"             [0x83f00000u;0x4895u;0x122u]          false true false false
            visUnitTest "ADD 23" "ADD R0, R1, R2, ROR R3"     [0x83f00000u;0x4895u;0x122u;0x1e3u]   false true false false
            visUnitTest "ADD 24" "ADD R0, R1, R2, RRX "       [0x83f00000u;0x4895u;0x122u;0x1e3u]   false true false false

            visUnitTest "ADD 25" "ADD R0, R1, R2"             [0x83f00000u;0x4895u;0x122u]          false true false true
            visUnitTest "ADD 29" "ADD R0, R1, R2, ROR R3"     [0x83f00000u;0x4895u;0x122u;0x1e3u]   false true false true
            visUnitTest "ADD 30" "ADD R0, R1, R2, RRX "       [0x83f00000u;0x4895u;0x122u;0x1e3u]   false true false true
        ]

[<Tests>]
let ADDSUnitTests =
    testList "ADDS Unit Tests"
        [
            visUnitTest "ADDS 1" "ADDS r0, r1, r2"              [10u;11u;12u;13u]   false false false false
            visUnitTest "ADDS 2" "ADDS R0, R1, R2, LSL R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "ADDS 3" "ADDS R0, R1, R2, LSR R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "ADDS 4" "ADDS R0, R1, R2, ASR R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "ADDS 5" "ADDS R0, R1, R2, ROR R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "ADDS 6" "ADDS R0, R1, R2, RRX "        [10u;11u;12u;13u]   false false false false

            visUnitTest "ADDS 7" "ADDS R0, R1, R2"              [0x83f00000u;0x4895u;0x122u]          false false false false
            visUnitTest "ADDS 11" "ADDS R0, R1, R2, ROR R3"     [0x83f00000u;0x4895u;0x122u;0x1e3u]   false false false false
            visUnitTest "ADDS 12" "ADDS R0, R1, R2, RRX "       [0x83f00000u;0x4895u;0x122u;0x1e3u]   false false false false

            visUnitTest "ADDS 13" "ADDS r0, r1, r2"              [10u;11u;12u;13u]   false false false false
            visUnitTest "ADDS 14" "ADDS R0, R1, R2, LSL R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "ADDS 15" "ADDS R0, R1, R2, LSR R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "ADDS 16" "ADDS R0, R1, R2, ASR R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "ADDS 17" "ADDS R0, R1, R2, ROR R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "ADDS 18" "ADDS R0, R1, R2, RRX "        [10u;11u;12u;13u]   false false false false

            visUnitTest "ADDS 19" "ADDS R0, R1, R2"             [0x83f00000u;0x4895u;0x122u]          false true false false
            visUnitTest "ADDS 23" "ADDS R0, R1, R2, ROR R3"     [0x83f00000u;0x4895u;0x122u;0x1e3u]   false true false false
            visUnitTest "ADDS 24" "ADDS R0, R1, R2, RRX "       [0x83f00000u;0x4895u;0x122u;0x1e3u]   false true false false

            visUnitTest "ADDS 25" "ADDS R0, R1, R2"             [0x83f00000u;0x4895u;0x122u]          false true false true
            visUnitTest "ADDS 29" "ADDS R0, R1, R2, ROR R3"     [0x83f00000u;0x4895u;0x122u;0x1e3u]   false true false true
        ]

[<Tests>]

let ADCUnitTests =
    testList "ADC Unit Tests"
        [
            visUnitTest "ADC 1" "ADC r0, r1, r2"              [10u;11u;12u;13u]   false false false false
            visUnitTest "ADC 2" "ADC R0, R1, R2, LSL R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "ADC 3" "ADC R0, R1, R2, LSR R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "ADC 4" "ADC R0, R1, R2, ASR R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "ADC 5" "ADC R0, R1, R2, ROR R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "ADC 6" "ADC R0, R1, R2, RRX "        [10u;11u;12u;13u]   false false false false

            visUnitTest "ADC 7" "ADC R0, R1, R2"              [0x83f00000u;0x4895u;0x122u]          false false false false
            visUnitTest "ADC 11" "ADC R0, R1, R2, ROR R3"     [0x83f00000u;0x4895u;0x122u;0x1e3u]   false false false false
            visUnitTest "ADC 12" "ADC R0, R1, R2, RRX "       [0x83f00000u;0x4895u;0x122u;0x1e3u]   false false false false

            visUnitTest "ADC 13" "ADC r0, r1, r2"              [10u;11u;12u;13u]   false false false false
            visUnitTest "ADC 14" "ADC R0, R1, R2, LSL R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "ADC 15" "ADC R0, R1, R2, LSR R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "ADC 16" "ADC R0, R1, R2, ASR R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "ADC 17" "ADC R0, R1, R2, ROR R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "ADC 18" "ADC R0, R1, R2, RRX "        [10u;11u;12u;13u]   false false false false

            visUnitTest "ADC 19" "ADC R0, R1, R2"             [0x83f00000u;0x4895u;0x122u]          false true false false
            visUnitTest "ADC 23" "ADC R0, R1, R2, ROR R3"     [0x83f00000u;0x4895u;0x122u;0x1e3u]   false true false false
            visUnitTest "ADC 24" "ADC R0, R1, R2, RRX "       [0x83f00000u;0x4895u;0x122u;0x1e3u]   false true false false

            visUnitTest "ADC 25" "ADC R0, R1, R2"             [0x83f00000u;0x4895u;0x122u]          false true false true
            visUnitTest "ADC 29" "ADC R0, R1, R2, ROR R3"     [0x83f00000u;0x4895u;0x122u;0x1e3u]   false true false true
            visUnitTest "ADC 30" "ADC R0, R1, R2, RRX "       [0x83f00000u;0x4895u;0x122u;0x1e3u]   false true false true
        ]

[<Tests>]
let ADCSUnitTests =
    testList "ADCS Unit Tests"
        [
            visUnitTest "ADCS 1" "ADCS r0, r1, r2"              [10u;11u;12u;13u]   false false false false
            visUnitTest "ADCS 2" "ADCS R0, R1, R2, LSL R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "ADCS 3" "ADCS R0, R1, R2, LSR R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "ADCS 4" "ADCS R0, R1, R2, ASR R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "ADCS 5" "ADCS R0, R1, R2, ROR R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "ADCS 6" "ADCS R0, R1, R2, RRX "        [10u;11u;12u;13u]   false false false false
            visUnitTest "ADCS 7" "ADCS R0, R1, R2"              [0x83f00000u;0x4895u;0x122u]          false false false false



            visUnitTest "ADCS 11" "ADCS R0, R1, R2, ROR R3"     [0x83f00000u;0x4895u;0x122u;0x1e3u]   false false false false
            visUnitTest "ADCS 12" "ADCS R0, R1, R2, RRX "       [0x83f00000u;0x4895u;0x122u;0x1e3u]   false false false false
            visUnitTest "ADCS 13" "ADCS r0, r1, r2"              [10u;11u;12u;13u]   false false false false
            visUnitTest "ADCS 14" "ADCS R0, R1, R2, LSL R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "ADCS 15" "ADCS R0, R1, R2, LSR R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "ADCS 16" "ADCS R0, R1, R2, ASR R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "ADCS 17" "ADCS R0, R1, R2, ROR R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "ADCS 18" "ADCS R0, R1, R2, RRX "        [10u;11u;12u;13u]   false false false false

            visUnitTest "ADCS 19" "ADCS R0, R1, R2"             [0x83f00000u;0x4895u;0x122u]          false true false false
            visUnitTest "ADCS 23" "ADCS R0, R1, R2, ROR R3"     [0x83f00000u;0x4895u;0x122u;0x1e3u]   false true false false
            visUnitTest "ADCS 24" "ADCS R0, R1, R2, RRX "       [0x83f00000u;0x4895u;0x122u;0x1e3u]   false true false false

            visUnitTest "ADCS 25" "ADCS R0, R1, R2"             [0x83f00000u;0x4895u;0x122u]          false true false true
            visUnitTest "ADCS 29" "ADCS R0, R1, R2, ROR R3"     [0x83f00000u;0x4895u;0x122u;0x1e3u]   false true false true
        ]

[<Tests>]
let SUBSUnitTests =
    testList "SUBS Unit Tests"
        [
            visUnitTest "SUBS 1" "SUBS r0, r1, r2"              [10u;11u;12u;13u]   false false false false
            visUnitTest "SUBS 2" "SUBS R0, R1, R2, LSL R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "SUBS 3" "SUBS R0, R1, R2, LSR R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "SUBS 4" "SUBS R0, R1, R2, ASR R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "SUBS 5" "SUBS R0, R1, R2, ROR R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "SUBS 6" "SUBS R0, R1, R2, RRX "        [10u;11u;12u;13u]   false false false false

            visUnitTest "SUBS 7" "SUBS R0, R1, R2"              [0x83f00000u;0x4895u;0x122u]          false false false false
            visUnitTest "SUBS 11" "SUBS R0, R1, R2, ROR R3"     [0x83f00000u;0x4895u;0x122u;0x1e3u]   false false false false
            visUnitTest "SUBS 12" "SUBS R0, R1, R2, RRX "       [0x83f00000u;0x4895u;0x122u;0x1e3u]   false false false false

            visUnitTest "SUBS 13" "SUBS r0, r1, r2"              [10u;11u;12u;13u]   false false false false
            visUnitTest "SUBS 14" "SUBS R0, R1, R2, LSL R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "SUBS 15" "SUBS R0, R1, R2, LSR R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "SUBS 16" "SUBS R0, R1, R2, ASR R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "SUBS 17" "SUBS R0, R1, R2, ROR R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "SUBS 18" "SUBS R0, R1, R2, RRX "        [10u;11u;12u;13u]   false false false false

            visUnitTest "SUBS 19" "SUBS R0, R1, R2"             [0x83f00000u;0x4895u;0x122u]          false true false false
            visUnitTest "SUBS 23" "SUBS R0, R1, R2, ROR R3"     [0x83f00000u;0x4895u;0x122u;0x1e3u]   false true false false
            visUnitTest "SUBS 24" "SUBS R0, R1, R2, RRX "       [0x83f00000u;0x4895u;0x122u;0x1e3u]   false true false false
            visUnitTest "SUBS 30" "SUBS R0, R1, R2, RRX "       [0x83f00000u;0x4895u;0x122u;0x1e3u]   false true false true
        ]

[<Tests>]
let SUBUnitTests =
    testList "SUB Unit Tests"
        [
            visUnitTest "SUB 1" "SUB r0, r1, r2"              [10u;11u;12u;13u]   false false false false
            visUnitTest "SUB 2" "SUB R0, R1, R2, LSL R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "SUB 3" "SUB R0, R1, R2, LSR R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "SUB 4" "SUB R0, R1, R2, ASR R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "SUB 5" "SUB R0, R1, R2, ROR R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "SUB 6" "SUB R0, R1, R2, RRX "        [10u;11u;12u;13u]   false false false false

            visUnitTest "SUB 7" "SUB R0, R1, R2"              [0x83f00000u;0x4895u;0x122u]          false false false false
            visUnitTest "SUB 11" "SUB R0, R1, R2, ROR R3"     [0x83f00000u;0x4895u;0x122u;0x1e3u]   false false false false
            visUnitTest "SUB 12" "SUB R0, R1, R2, RRX "       [0x83f00000u;0x4895u;0x122u;0x1e3u]   false false false false

            visUnitTest "SUB 13" "SUB r0, r1, r2"              [10u;11u;12u;13u]   false false false false
            visUnitTest "SUB 14" "SUB R0, R1, R2, LSL R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "SUB 15" "SUB R0, R1, R2, LSR R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "SUB 16" "SUB R0, R1, R2, ASR R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "SUB 17" "SUB R0, R1, R2, ROR R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "SUB 18" "SUB R0, R1, R2, RRX "        [10u;11u;12u;13u]   false false false false

            visUnitTest "SUB 19" "SUB R0, R1, R2"             [0x83f00000u;0x4895u;0x122u]          false true false false
            visUnitTest "SUB 23" "SUB R0, R1, R2, ROR R3"     [0x83f00000u;0x4895u;0x122u;0x1e3u]   false true false false
            visUnitTest "SUB 24" "SUB R0, R1, R2, RRX "       [0x83f00000u;0x4895u;0x122u;0x1e3u]   false true false false

            visUnitTest "SUB 25" "SUB R0, R1, R2"             [0x83f00000u;0x4895u;0x122u]          false true false true
            visUnitTest "SUB 29" "SUB R0, R1, R2, ROR R3"     [0x83f00000u;0x4895u;0x122u;0x1e3u]   false true false true
            visUnitTest "SUB 30" "SUB R0, R1, R2, RRX "       [0x83f00000u;0x4895u;0x122u;0x1e3u]   false true false true
        ]

[<Tests>]
let SBCSUnitTests =
    testList "SBCS Unit Tests"
        [
            visUnitTest "SBCS 1" "SBCS r0, r1, r2"              [10u;11u;12u;13u]   false false false false
            visUnitTest "SBCS 2" "SBCS R0, R1, R2, LSL R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "SBCS 3" "SBCS R0, R1, R2, LSR R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "SBCS 4" "SBCS R0, R1, R2, ASR R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "SBCS 5" "SBCS R0, R1, R2, ROR R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "SBCS 6" "SBCS R0, R1, R2, RRX "        [10u;11u;12u;13u]   false false false false

            visUnitTest "SBCS 7" "SBCS R0, R1, R2"              [0x83f00000u;0x4895u;0x122u]          false false false false
            visUnitTest "SBCS 11" "SBCS R0, R1, R2, ROR R3"     [0x83f00000u;0x4895u;0x122u;0x1e3u]   false false false false
            visUnitTest "SBCS 12" "SBCS R0, R1, R2, RRX "       [0x83f00000u;0x4895u;0x122u;0x1e3u]   false false false false

            visUnitTest "SBCS 13" "SBCS r0, r1, r2"              [10u;11u;12u;13u]   false false false false
            visUnitTest "SBCS 14" "SBCS R0, R1, R2, LSL R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "SBCS 15" "SBCS R0, R1, R2, LSR R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "SBCS 16" "SBCS R0, R1, R2, ASR R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "SBCS 17" "SBCS R0, R1, R2, ROR R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "SBCS 18" "SBCS R0, R1, R2, RRX "        [10u;11u;12u;13u]   false false false false

            visUnitTest "SBCS 19" "SBCS R0, R1, R2"             [0x83f00000u;0x4895u;0x122u]          false true false false
            visUnitTest "SBCS 23" "SBCS R0, R1, R2, ROR R3"     [0x83f00000u;0x4895u;0x122u;0x1e3u]   false true false false
            visUnitTest "SBCS 24" "SBCS R0, R1, R2, RRX "       [0x83f00000u;0x4895u;0x122u;0x1e3u]   false true false false
            visUnitTest "SBCS 30" "SBCS R0, R1, R2, RRX "       [0x83f00000u;0x4895u;0x122u;0x1e3u]   false true false true
        ]

[<Tests>]
let SBCUnitTests =
    testList "SBC Unit Tests"
        [
            visUnitTest "SBC 1" "SBC r0, r1, r2"              [10u;11u;12u;13u]   false false false false
            visUnitTest "SBC 2" "SBC R0, R1, R2, LSL R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "SBC 3" "SBC R0, R1, R2, LSR R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "SBC 4" "SBC R0, R1, R2, ASR R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "SBC 5" "SBC R0, R1, R2, ROR R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "SBC 6" "SBC R0, R1, R2, RRX "        [10u;11u;12u;13u]   false false false false

            visUnitTest "SBC 7" "SBC R0, R1, R2"              [0x83f00000u;0x4895u;0x122u]          false false false false
            visUnitTest "SBC 11" "SBC R0, R1, R2, ROR R3"     [0x83f00000u;0x4895u;0x122u;0x1e3u]   false false false false
            visUnitTest "SBC 12" "SBC R0, R1, R2, RRX "       [0x83f00000u;0x4895u;0x122u;0x1e3u]   false false false false

            visUnitTest "SBC 13" "SBC r0, r1, r2"              [10u;11u;12u;13u]   false false false false
            visUnitTest "SBC 14" "SBC R0, R1, R2, LSL R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "SBC 15" "SBC R0, R1, R2, LSR R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "SBC 16" "SBC R0, R1, R2, ASR R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "SBC 17" "SBC R0, R1, R2, ROR R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "SBC 18" "SBC R0, R1, R2, RRX "        [10u;11u;12u;13u]   false false false false

            visUnitTest "SBC 19" "SBC R0, R1, R2"             [0x83f00000u;0x4895u;0x122u]          false true false false
            visUnitTest "SBC 23" "SBC R0, R1, R2, ROR R3"     [0x83f00000u;0x4895u;0x122u;0x1e3u]   false true false false
            visUnitTest "SBC 24" "SBC R0, R1, R2, RRX "       [0x83f00000u;0x4895u;0x122u;0x1e3u]   false true false false

            visUnitTest "SBC 25" "SBC R0, R1, R2"             [0x83f00000u;0x4895u;0x122u]          false true false true
            visUnitTest "SBC 29" "SBC R0, R1, R2, ROR R3"     [0x83f00000u;0x4895u;0x122u;0x1e3u]   false true false true
            visUnitTest "SBC 30" "SBC R0, R1, R2, RRX "       [0x83f00000u;0x4895u;0x122u;0x1e3u]   false true false true
        ]

[<Tests>]
let RSBUnitTests =
    testList "RSB Unit Tests"
        [
            visUnitTest "RSB 1" "RSB r0, r1, r2"              [10u;11u;12u;13u]   false false false false
            visUnitTest "RSB 2" "RSB R0, R1, R2, LSL R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "RSB 3" "RSB R0, R1, R2, LSR R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "RSB 4" "RSB R0, R1, R2, ASR R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "RSB 5" "RSB R0, R1, R2, ROR R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "RSB 6" "RSB R0, R1, R2, RRX "        [10u;11u;12u;13u]   false false false false

            visUnitTest "RSB 7" "RSB R0, R1, R2"              [0x83f00000u;0x4895u;0x122u]          false false false false
            visUnitTest "RSB 11" "RSB R0, R1, R2, ROR R3"     [0x83f00000u;0x4895u;0x122u;0x1e3u]   false false false false
            visUnitTest "RSB 12" "RSB R0, R1, R2, RRX "       [0x83f00000u;0x4895u;0x122u;0x1e3u]   false false false false

            visUnitTest "RSB 13" "RSB r0, r1, r2"              [10u;11u;12u;13u]   false false false false
            visUnitTest "RSB 14" "RSB R0, R1, R2, LSL R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "RSB 15" "RSB R0, R1, R2, LSR R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "RSB 16" "RSB R0, R1, R2, ASR R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "RSB 17" "RSB R0, R1, R2, ROR R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "RSB 18" "RSB R0, R1, R2, RRX "        [10u;11u;12u;13u]   false false false false

            visUnitTest "RSB 19" "RSB R0, R1, R2"             [0x83f00000u;0x4895u;0x122u]          false true false false
            visUnitTest "RSB 23" "RSB R0, R1, R2, ROR R3"     [0x83f00000u;0x4895u;0x122u;0x1e3u]   false true false false
            visUnitTest "RSB 24" "RSB R0, R1, R2, RRX "       [0x83f00000u;0x4895u;0x122u;0x1e3u]   false true false false

            visUnitTest "RSB 25" "RSB R0, R1, R2"             [0x83f00000u;0x4895u;0x122u]          false true false true
            visUnitTest "RSB 29" "RSB R0, R1, R2, ROR R3"     [0x83f00000u;0x4895u;0x122u;0x1e3u]   false true false true
            visUnitTest "RSB 30" "RSB R0, R1, R2, RRX "       [0x83f00000u;0x4895u;0x122u;0x1e3u]   false true false true
        ]

[<Tests>]
let RSBSUnitTests =
    testList "RSBS Unit Tests"
        [
            visUnitTest "RSBS 1" "RSBS r0, r1, r2"              [10u;11u;12u;13u]   false false false false
            visUnitTest "RSBS 2" "RSBS R0, R1, R2, LSL R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "RSBS 3" "RSBS R0, R1, R2, LSR R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "RSBS 4" "RSBS R0, R1, R2, ASR R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "RSBS 5" "RSBS R0, R1, R2, ROR R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "RSBS 6" "RSBS R0, R1, R2, RRX "        [10u;11u;12u;13u]   false false false false

            visUnitTest "RSBS 7" "RSBS R0, R1, R2"              [0x83f00000u;0x4895u;0x122u]          false false false false
            visUnitTest "RSBS 11" "RSBS R0, R1, R2, ROR R3"     [0x83f00000u;0x4895u;0x122u;0x1e3u]   false false false false
            visUnitTest "RSBS 12" "RSBS R0, R1, R2, RRX "       [0x83f00000u;0x4895u;0x122u;0x1e3u]   false false false false

            visUnitTest "RSBS 13" "RSBS r0, r1, r2"              [10u;11u;12u;13u]   false false false false
            visUnitTest "RSBS 14" "RSBS R0, R1, R2, LSL R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "RSBS 15" "RSBS R0, R1, R2, LSR R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "RSBS 16" "RSBS R0, R1, R2, ASR R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "RSBS 17" "RSBS R0, R1, R2, ROR R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "RSBS 18" "RSBS R0, R1, R2, RRX "        [10u;11u;12u;13u]   false false false false

            visUnitTest "RSBS 19" "RSBS R0, R1, R2"             [0x83f00000u;0x4895u;0x122u]          false true false false
            visUnitTest "RSBS 23" "RSBS R0, R1, R2, ROR R3"     [0x83f00000u;0x4895u;0x122u;0x1e3u]   false true false false
            visUnitTest "RSBS 24" "RSBS R0, R1, R2, RRX "       [0x83f00000u;0x4895u;0x122u;0x1e3u]   false true false false
            visUnitTest "RSBS 30" "RSBS R0, R1, R2, RRX "       [0x83f00000u;0x4895u;0x122u;0x1e3u]   false true false true
        ]

[<Tests>]
let RSCUnitTests =
    testList "RSC Unit Tests"
        [
            visUnitTest "RSC 1" "RSC r0, r1, r2"              [10u;11u;12u;13u]   false false false false
            visUnitTest "RSC 2" "RSC R0, R1, R2, LSL R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "RSC 3" "RSC R0, R1, R2, LSR R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "RSC 4" "RSC R0, R1, R2, ASR R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "RSC 5" "RSC R0, R1, R2, ROR R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "RSC 6" "RSC R0, R1, R2, RRX "        [10u;11u;12u;13u]   false false false false
            visUnitTest "RSC 7" "RSC R0, R1, R2"              [0x83f00000u;0x4895u;0x122u]          false false false false



            visUnitTest "RSC 11" "RSC R0, R1, R2, ROR R3"     [0x83f00000u;0x4895u;0x122u;0x1e3u]   false false false false
            visUnitTest "RSC 12" "RSC R0, R1, R2, RRX "       [0x83f00000u;0x4895u;0x122u;0x1e3u]   false false false false
            visUnitTest "RSC 13" "RSC r0, r1, r2"              [10u;11u;12u;13u]   false false false false
            visUnitTest "RSC 14" "RSC R0, R1, R2, LSL R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "RSC 15" "RSC R0, R1, R2, LSR R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "RSC 16" "RSC R0, R1, R2, ASR R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "RSC 17" "RSC R0, R1, R2, ROR R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "RSC 18" "RSC R0, R1, R2, RRX "        [10u;11u;12u;13u]   false false false false
            visUnitTest "RSC 19" "RSC R0, R1, R2"             [0x83f00000u;0x4895u;0x122u]          false true false false



            visUnitTest "RSC 23" "RSC R0, R1, R2, ROR R3"     [0x83f00000u;0x4895u;0x122u;0x1e3u]   false true false false
            visUnitTest "RSC 24" "RSC R0, R1, R2, RRX "       [0x83f00000u;0x4895u;0x122u;0x1e3u]   false true false false
            visUnitTest "RSC 25" "RSC R0, R1, R2"             [0x83f00000u;0x4895u;0x122u]          false true false true
            visUnitTest "RSC 29" "RSC R0, R1, R2, ROR R3"     [0x83f00000u;0x4895u;0x122u;0x1e3u]   false true false true
            visUnitTest "RSC 30" "RSC R0, R1, R2, RRX "       [0x83f00000u;0x4895u;0x122u;0x1e3u]   false true false true
        ]

[<Tests>]
let RSCSUnitTests =
    testList "RSCS Unit Tests"
        [
            visUnitTest "RSCS 1" "RSCS r0, r1, r2"              [10u;11u;12u;13u]   false false false false
            visUnitTest "RSCS 2" "RSCS R0, R1, R2, LSL R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "RSCS 3" "RSCS R0, R1, R2, LSR R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "RSCS 4" "RSCS R0, R1, R2, ASR R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "RSCS 5" "RSCS R0, R1, R2, ROR R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "RSCS 6" "RSCS R0, R1, R2, RRX "        [10u;11u;12u;13u]   false false false false

            visUnitTest "RSCS 7" "RSCS R0, R1, R2"              [0x83f00000u;0x4895u;0x122u]          false false false false
            visUnitTest "RSCS 11" "RSCS R0, R1, R2, ROR R3"     [0x83f00000u;0x4895u;0x122u;0x1e3u]   false false false false
            visUnitTest "RSCS 12" "RSCS R0, R1, R2, RRX "       [0x83f00000u;0x4895u;0x122u;0x1e3u]   false false false false

            visUnitTest "RSCS 13" "RSCS r0, r1, r2"              [10u;11u;12u;13u]   false false false false
            visUnitTest "RSCS 14" "RSCS R0, R1, R2, LSL R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "RSCS 15" "RSCS R0, R1, R2, LSR R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "RSCS 16" "RSCS R0, R1, R2, ASR R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "RSCS 17" "RSCS R0, R1, R2, ROR R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "RSCS 18" "RSCS R0, R1, R2, RRX "        [10u;11u;12u;13u]   false false false false
            visUnitTest "RSCS 19" "RSCS R0, R1, R2"             [0x83f00000u;0x4895u;0x122u]          false true false false

            visUnitTest "RSCS 23" "RSCS R0, R1, R2, ROR R3"     [0x83f00000u;0x4895u;0x122u;0x1e3u]   false true false false
            visUnitTest "RSCS 24" "RSCS R0, R1, R2, RRX "       [0x83f00000u;0x4895u;0x122u;0x1e3u]   false true false false
            visUnitTest "RSCS 30" "RSCS R0, R1, R2, RRX "       [0x83f00000u;0x4895u;0x122u;0x1e3u]   false true false true
        ]

[<Tests>]
let ANDUnitTests =
    testList "AND Unit Tests"
        [
            visUnitTest "AND 1" "AND r0, r1, r2"              [10u;11u;12u;13u]   false false false false
            visUnitTest "AND 2" "AND R0, R1, R2, LSL R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "AND 3" "AND R0, R1, R2, LSR R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "AND 4" "AND R0, R1, R2, ASR R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "AND 5" "AND R0, R1, R2, ROR R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "AND 6" "AND R0, R1, R2, RRX "        [10u;11u;12u;13u]   false false false false

            visUnitTest "AND 7" "AND R0, R1, R2"              [0x83f00000u;0x4895u;0x122u]          false false false false
            visUnitTest "AND 11" "AND R0, R1, R2, ROR R3"     [0x83f00000u;0x4895u;0x122u;0x1e3u]   false false false false
            visUnitTest "AND 12" "AND R0, R1, R2, RRX "       [0x83f00000u;0x4895u;0x122u;0x1e3u]   false false false false

            visUnitTest "AND 13" "AND r0, r1, r2"              [10u;11u;12u;13u]   false false false false
            visUnitTest "AND 14" "AND R0, R1, R2, LSL R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "AND 15" "AND R0, R1, R2, LSR R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "AND 16" "AND R0, R1, R2, ASR R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "AND 17" "AND R0, R1, R2, ROR R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "AND 18" "AND R0, R1, R2, RRX "        [10u;11u;12u;13u]   false false false false

            visUnitTest "AND 19" "AND R0, R1, R2"             [0x83f00000u;0x4895u;0x122u]          false true false false
            visUnitTest "AND 23" "AND R0, R1, R2, ROR R3"     [0x83f00000u;0x4895u;0x122u;0x1e3u]   false true false false
            visUnitTest "AND 24" "AND R0, R1, R2, RRX "       [0x83f00000u;0x4895u;0x122u;0x1e3u]   false true false false

            visUnitTest "AND 25" "AND R0, R1, R2"             [0x83f00000u;0x4895u;0x122u]          false true false true
            visUnitTest "AND 29" "AND R0, R1, R2, ROR R3"     [0x83f00000u;0x4895u;0x122u;0x1e3u]   false true false true
            visUnitTest "AND 30" "AND R0, R1, R2, RRX "       [0x83f00000u;0x4895u;0x122u;0x1e3u]   false true false true
        ]

[<Tests>]
let ANDSUnitTests =
    testList "ANDS Unit Tests"
        [
            visUnitTest "ANDS 1" "ANDS r0, r1, r2"              [10u;11u;12u;13u]   false false false false
            visUnitTest "ANDS 2" "ANDS R0, R1, R2, LSL R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "ANDS 3" "ANDS R0, R1, R2, LSR R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "ANDS 4" "ANDS R0, R1, R2, ASR R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "ANDS 5" "ANDS R0, R1, R2, ROR R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "ANDS 6" "ANDS R0, R1, R2, RRX "        [10u;11u;12u;13u]   false false false false

            visUnitTest "ANDS 7" "ANDS R0, R1, R2"              [0x83f00000u;0x4895u;0x122u]          false false false false
            visUnitTest "ANDS 11" "ANDS R0, R1, R2, ROR R3"     [0x83f00000u;0x4895u;0x122u;0x1e3u]   false false false false
            visUnitTest "ANDS 12" "ANDS R0, R1, R2, RRX "       [0x83f00000u;0x4895u;0x122u;0x1e3u]   false false false false

            visUnitTest "ANDS 13" "ANDS r0, r1, r2"              [10u;11u;12u;13u]   false false false false
            visUnitTest "ANDS 14" "ANDS R0, R1, R2, LSL R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "ANDS 15" "ANDS R0, R1, R2, LSR R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "ANDS 16" "ANDS R0, R1, R2, ASR R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "ANDS 17" "ANDS R0, R1, R2, ROR R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "ANDS 18" "ANDS R0, R1, R2, RRX "        [10u;11u;12u;13u]   false false false false

            visUnitTest "ANDS 19" "ANDS R0, R1, R2"             [0x83f00000u;0x4895u;0x122u]          false true false false
            visUnitTest "ANDS 23" "ANDS R0, R1, R2, ROR R3"     [0x83f00000u;0x4895u;0x122u;0x1e3u]   false true false false
            visUnitTest "ANDS 24" "ANDS R0, R1, R2, RRX "       [0x83f00000u;0x4895u;0x122u;0x1e3u]   false true false false

            visUnitTest "ANDS 25" "ANDS R0, R1, R2"             [0x83f00000u;0x4895u;0x122u]          false true false true
            visUnitTest "ANDS 29" "ANDS R0, R1, R2, ROR R3"     [0x83f00000u;0x4895u;0x122u;0x1e3u]   false true false true
            visUnitTest "ANDS 30" "ANDS R0, R1, R2, RRX "       [0x83f00000u;0x4895u;0x122u;0x1e3u]   false true false true
        ]

[<Tests>]
// N C Z V
let ORRUnitTests =
    testList "ORR Unit Tests"
        [
            visUnitTest "ORR 1" "ORR r0, r1, r2"              [10u;11u;12u;13u]   false false false false
            visUnitTest "ORR 2" "ORR R0, R1, R2, LSL R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "ORR 3" "ORR R0, R1, R2, LSR R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "ORR 4" "ORR R0, R1, R2, ASR R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "ORR 5" "ORR R0, R1, R2, ROR R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "ORR 6" "ORR R0, R1, R2, RRX "        [10u;11u;12u;13u]   false false false false

            visUnitTest "ORR 7" "ORR R0, R1, R2"              [0x83f00000u;0x4895u;0x122u]          false false false false
            visUnitTest "ORR 11" "ORR R0, R1, R2, ROR R3"     [0x83f00000u;0x4895u;0x122u;0x1e3u]   false false false false
            visUnitTest "ORR 12" "ORR R0, R1, R2, RRX "       [0x83f00000u;0x4895u;0x122u;0x1e3u]   false false false false

            visUnitTest "ORR 13" "ORR r0, r1, r2"              [10u;11u;12u;13u]   false false false false
            visUnitTest "ORR 14" "ORR R0, R1, R2, LSL R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "ORR 15" "ORR R0, R1, R2, LSR R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "ORR 16" "ORR R0, R1, R2, ASR R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "ORR 17" "ORR R0, R1, R2, ROR R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "ORR 18" "ORR R0, R1, R2, RRX "        [10u;11u;12u;13u]   false false false false

            visUnitTest "ORR 19" "ORR R0, R1, R2"             [0x83f00000u;0x4895u;0x122u]          false true false false
            visUnitTest "ORR 23" "ORR R0, R1, R2, ROR R3"     [0x83f00000u;0x4895u;0x122u;0x1e3u]   false true false false
            visUnitTest "ORR 24" "ORR R0, R1, R2, RRX "       [0x83f00000u;0x4895u;0x122u;0x1e3u]   false true false false

            visUnitTest "ORR 25" "ORR R0, R1, R2"             [0x83f00000u;0x4895u;0x122u]          false true false true
            visUnitTest "ORR 29" "ORR R0, R1, R2, ROR R3"     [0x83f00000u;0x4895u;0x122u;0x1e3u]   false true false true
            visUnitTest "ORR 30" "ORR R0, R1, R2, RRX "       [0x83f00000u;0x4895u;0x122u;0x1e3u]   false true false true
        ]

[<Tests>]
let ORRSUnitTests =
    testList "ORRS Unit Tests"
        [
            visUnitTest "ORRS 1" "ORRS r0, r1, r2"              [10u;11u;12u;13u]   false false false false
            visUnitTest "ORRS 2" "ORRS R0, R1, R2, LSL R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "ORRS 3" "ORRS R0, R1, R2, LSR R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "ORRS 4" "ORRS R0, R1, R2, ASR R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "ORRS 5" "ORRS R0, R1, R2, ROR R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "ORRS 6" "ORRS R0, R1, R2, RRX "        [10u;11u;12u;13u]   false false false false

            visUnitTest "ORRS 7" "ORRS R0, R1, R2"              [0x83f00000u;0x4895u;0x122u]          false false false false
            visUnitTest "ORRS 11" "ORRS R0, R1, R2, ROR R3"     [0x83f00000u;0x4895u;0x122u;0x1e3u]   false false false false
            visUnitTest "ORRS 12" "ORRS R0, R1, R2, RRX "       [0x83f00000u;0x4895u;0x122u;0x1e3u]   false false false false

            visUnitTest "ORRS 13" "ORRS r0, r1, r2"              [10u;11u;12u;13u]   false false false false
            visUnitTest "ORRS 14" "ORRS R0, R1, R2, LSL R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "ORRS 15" "ORRS R0, R1, R2, LSR R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "ORRS 16" "ORRS R0, R1, R2, ASR R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "ORRS 17" "ORRS R0, R1, R2, ROR R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "ORRS 18" "ORRS R0, R1, R2, RRX "        [10u;11u;12u;13u]   false false false false

            visUnitTest "ORRS 19" "ORRS R0, R1, R2"             [0x83f00000u;0x4895u;0x122u]          false true false false
            visUnitTest "ORRS 23" "ORRS R0, R1, R2, ROR R3"     [0x83f00000u;0x4895u;0x122u;0x1e3u]   false true false false
            visUnitTest "ORRS 24" "ORRS R0, R1, R2, RRX "       [0x83f00000u;0x4895u;0x122u;0x1e3u]   false true false false
            visUnitTest "ORRS 25" "ORRS R0, R1, R2"             [0x83f00000u;0x4895u;0x122u]          false true false true
            visUnitTest "ORRS 29" "ORRS R0, R1, R2, ROR R3"     [0x83f00000u;0x4895u;0x122u;0x1e3u]   false true false true
            visUnitTest "ORRS 30" "ORRS R0, R1, R2, RRX "       [0x83f00000u;0x4895u;0x122u;0x1e3u]   false true false true
        ]


[<Tests>]
// N C Z V
let EORUnitTests =
    testList "EOR Unit Tests"
        [
            visUnitTest "EOR 1" "EOR r0, r1, r2"              [10u;11u;12u;13u]   false false false false
            visUnitTest "EOR 2" "EOR R0, R1, R2, LSL R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "EOR 3" "EOR R0, R1, R2, LSR R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "EOR 4" "EOR R0, R1, R2, ASR R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "EOR 5" "EOR R0, R1, R2, ROR R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "EOR 6" "EOR R0, R1, R2, RRX "        [10u;11u;12u;13u]   false false false false

            visUnitTest "EOR 7" "EOR R0, R1, R2"              [0x83f00000u;0x4895u;0x122u]          false false false false
            visUnitTest "EOR 11" "EOR R0, R1, R2, ROR R3"     [0x83f00000u;0x4895u;0x122u;0x1e3u]   false false false false
            visUnitTest "EOR 12" "EOR R0, R1, R2, RRX "       [0x83f00000u;0x4895u;0x122u;0x1e3u]   false false false false

            visUnitTest "EOR 13" "EOR r0, r1, r2"              [10u;11u;12u;13u]   false false false false
            visUnitTest "EOR 14" "EOR R0, R1, R2, LSL R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "EOR 15" "EOR R0, R1, R2, LSR R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "EOR 16" "EOR R0, R1, R2, ASR R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "EOR 17" "EOR R0, R1, R2, ROR R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "EOR 18" "EOR R0, R1, R2, RRX "        [10u;11u;12u;13u]   false false false false

            visUnitTest "EOR 19" "EOR R0, R1, R2"             [0x83f00000u;0x4895u;0x122u]          false true false false
            visUnitTest "EOR 23" "EOR R0, R1, R2, ROR R3"     [0x83f00000u;0x4895u;0x122u;0x1e3u]   false true false false
            visUnitTest "EOR 24" "EOR R0, R1, R2, RRX "       [0x83f00000u;0x4895u;0x122u;0x1e3u]   false true false false

            visUnitTest "EOR 25" "EOR R0, R1, R2"             [0x83f00000u;0x4895u;0x122u]          false true false true
            visUnitTest "EOR 29" "EOR R0, R1, R2, ROR R3"     [0x83f00000u;0x4895u;0x122u;0x1e3u]   false true false true
            visUnitTest "EOR 30" "EOR R0, R1, R2, RRX "       [0x83f00000u;0x4895u;0x122u;0x1e3u]   false true false true
        ]

[<Tests>]
let EORSUnitTests =
    testList "EORS Unit Tests"
        [
            visUnitTest "EORS 1" "EORS r0, r1, r2"              [10u;11u;12u;13u]   false false false false
            visUnitTest "EORS 2" "EORS R0, R1, R2, LSL R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "EORS 3" "EORS R0, R1, R2, LSR R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "EORS 4" "EORS R0, R1, R2, ASR R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "EORS 5" "EORS R0, R1, R2, ROR R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "EORS 6" "EORS R0, R1, R2, RRX "        [10u;11u;12u;13u]   false false false false

            visUnitTest "EORS 7" "EORS R0, R1, R2"              [0x83f00000u;0x4895u;0x122u]          false false false false
            visUnitTest "EORS 11" "EORS R0, R1, R2, ROR R3"     [0x83f00000u;0x4895u;0x122u;0x1e3u]   false false false false
            visUnitTest "EORS 12" "EORS R0, R1, R2, RRX "       [0x83f00000u;0x4895u;0x122u;0x1e3u]   false false false false

            visUnitTest "EORS 13" "EORS r0, r1, r2"              [10u;11u;12u;13u]   false false false false
            visUnitTest "EORS 14" "EORS R0, R1, R2, LSL R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "EORS 15" "EORS R0, R1, R2, LSR R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "EORS 16" "EORS R0, R1, R2, ASR R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "EORS 17" "EORS R0, R1, R2, ROR R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "EORS 18" "EORS R0, R1, R2, RRX "        [10u;11u;12u;13u]   false false false false

            visUnitTest "EORS 19" "EORS R0, R1, R2"             [0x83f00000u;0x4895u;0x122u]          false true false false
            visUnitTest "EORS 23" "EORS R0, R1, R2, ROR R3"     [0x83f00000u;0x4895u;0x122u;0x1e3u]   false true false false
            visUnitTest "EORS 24" "EORS R0, R1, R2, RRX "       [0x83f00000u;0x4895u;0x122u;0x1e3u]   false true false false

            visUnitTest "EORS 25" "EORS R0, R1, R2"             [0x83f00000u;0x4895u;0x122u]          false true false true
            visUnitTest "EORS 29" "EORS R0, R1, R2, ROR R3"     [0x83f00000u;0x4895u;0x122u;0x1e3u]   false true false true
            visUnitTest "EORS 30" "EORS R0, R1, R2, RRX "       [0x83f00000u;0x4895u;0x122u;0x1e3u]   false true false true
        ]

[<Tests>]
// N C Z V
let BICUnitTests =
    testList "BIC Unit Tests"
        [
            visUnitTest "BIC 1" "BIC r0, r1, r2"              [10u;11u;12u;13u]   false false false false
            visUnitTest "BIC 2" "BIC R0, R1, R2, LSL R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "BIC 3" "BIC R0, R1, R2, LSR R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "BIC 4" "BIC R0, R1, R2, ASR R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "BIC 5" "BIC R0, R1, R2, ROR R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "BIC 6" "BIC R0, R1, R2, RRX "        [10u;11u;12u;13u]   false false false false

            visUnitTest "BIC 7" "BIC R0, R1, R2"              [0x83f00000u;0x4895u;0x122u]          false false false false
            visUnitTest "BIC 11" "BIC R0, R1, R2, ROR R3"     [0x83f00000u;0x4895u;0x122u;0x1e3u]   false false false false
            visUnitTest "BIC 12" "BIC R0, R1, R2, RRX "       [0x83f00000u;0x4895u;0x122u;0x1e3u]   false false false false

            visUnitTest "BIC 13" "BIC r0, r1, r2"              [10u;11u;12u;13u]   false false false false
            visUnitTest "BIC 14" "BIC R0, R1, R2, LSL R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "BIC 15" "BIC R0, R1, R2, LSR R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "BIC 16" "BIC R0, R1, R2, ASR R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "BIC 17" "BIC R0, R1, R2, ROR R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "BIC 18" "BIC R0, R1, R2, RRX "        [10u;11u;12u;13u]   false false false false

            visUnitTest "BIC 19" "BIC R0, R1, R2"             [0x83f00000u;0x4895u;0x122u]          false true false false
            visUnitTest "BIC 23" "BIC R0, R1, R2, ROR R3"     [0x83f00000u;0x4895u;0x122u;0x1e3u]   false true false false
            visUnitTest "BIC 24" "BIC R0, R1, R2, RRX "       [0x83f00000u;0x4895u;0x122u;0x1e3u]   false true false false

            visUnitTest "BIC 25" "BIC R0, R1, R2"             [0x83f00000u;0x4895u;0x122u]          false true false true
            visUnitTest "BIC 29" "BIC R0, R1, R2, ROR R3"     [0x83f00000u;0x4895u;0x122u;0x1e3u]   false true false true
            visUnitTest "BIC 30" "BIC R0, R1, R2, RRX "       [0x83f00000u;0x4895u;0x122u;0x1e3u]   false true false true
        ]

[<Tests>]
let BICSUnitTests =
    testList "BICS Unit Tests"
        [
            visUnitTest "BICS 1" "BICS r0, r1, r2"              [10u;11u;12u;13u]   false false false false
            visUnitTest "BICS 2" "BICS R0, R1, R2, LSL R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "BICS 3" "BICS R0, R1, R2, LSR R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "BICS 4" "BICS R0, R1, R2, ASR R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "BICS 5" "BICS R0, R1, R2, ROR R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "BICS 6" "BICS R0, R1, R2, RRX "        [10u;11u;12u;13u]   false false false false

            visUnitTest "BICS 7" "BICS R0, R1, R2"              [0x83f00000u;0x4895u;0x122u]          false false false false
            visUnitTest "BICS 11" "BICS R0, R1, R2, ROR R3"     [0x83f00000u;0x4895u;0x122u;0x1e3u]   false false false false
            visUnitTest "BICS 12" "BICS R0, R1, R2, RRX "       [0x83f00000u;0x4895u;0x122u;0x1e3u]   false false false false

            visUnitTest "BICS 13" "BICS r0, r1, r2"              [10u;11u;12u;13u]   false false false false
            visUnitTest "BICS 14" "BICS R0, R1, R2, LSL R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "BICS 15" "BICS R0, R1, R2, LSR R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "BICS 16" "BICS R0, R1, R2, ASR R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "BICS 17" "BICS R0, R1, R2, ROR R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "BICS 18" "BICS R0, R1, R2, RRX "        [10u;11u;12u;13u]   false false false false

            visUnitTest "BICS 19" "BICS R0, R1, R2"             [0x83f00000u;0x4895u;0x122u]          false true false false
            visUnitTest "BICS 23" "BICS R0, R1, R2, ROR R3"     [0x83f00000u;0x4895u;0x122u;0x1e3u]   false true false false
            visUnitTest "BICS 24" "BICS R0, R1, R2, RRX "       [0x83f00000u;0x4895u;0x122u;0x1e3u]   false true false false

            visUnitTest "BICS 25" "BICS R0, R1, R2"             [0x83f00000u;0x4895u;0x122u]          false true false true
            visUnitTest "BICS 29" "BICS R0, R1, R2, ROR R3"     [0x83f00000u;0x4895u;0x122u;0x1e3u]   false true false true
            visUnitTest "BICS 30" "BICS R0, R1, R2, RRX "       [0x83f00000u;0x4895u;0x122u;0x1e3u]   false true false true
        ]
