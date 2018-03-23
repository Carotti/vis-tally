; non-contiguous memory demonstration
mov r0, #0x50
mov r1, #0x100

mov r2, #0x200
mov r3, #0x300

str r0, [r2]
str r2, [r3]