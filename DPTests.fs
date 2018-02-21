module DPTests
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