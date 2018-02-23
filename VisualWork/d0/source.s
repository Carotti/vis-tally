MOV R0, #0x80000000
ADDS R0, R0, R0
MOVS R0, #1
MOV R0, #0xf00000
ADD R0, R0, #0x83000000
MOV R1, #0x95
ADD R1, R1, #0x4800
MOV R2, #0x22
ADD R2, R2, #0x100
MOV R3, #0xe3
ADD R3, R3, #0x100
MOV R4, #0x1
MOV R5, #0x2
MOV R6, #0x3
MOV R7, #0x4
MOV R8, #0x5
MOV R9, #0x6
MOV R10, #0x7
MOV R11, #0x8
MOV R12, #0x9
MOV R13, #0xa
MOV R14, #0xb


BICS R0, R1, R2, RRX 
MOV R13, #0x1000
LDMIA R13, {R0-R12}
MOV R0, #0
              ADDMI R0, R0, #8
              ADDEQ R0, R0, #4
              ADDCS R0, R0, #2
              ADDVS R0, R0, #1
