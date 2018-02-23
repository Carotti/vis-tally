MOV R0, #0
ADDS R0, R0, R0
MOVS R0, #1
MOV R0, #0xa
MOV R1, #0xb
MOV R2, #0xc
MOV R3, #0x1
MOV R4, #0x2
MOV R5, #0x3
MOV R6, #0x4
MOV R7, #0x5
MOV R8, #0x6
MOV R9, #0x7
MOV R10, #0x8
MOV R11, #0x9
MOV R12, #0xa
MOV R13, #0xb
MOV R14, #0xc


ADDS r0, r1, r2
MOV R13, #0x1000
LDMIA R13, {R0-R12}
MOV R0, #0
              ADDMI R0, R0, #8
              ADDEQ R0, R0, #4
              ADDCS R0, R0, #2
              ADDVS R0, R0, #1
