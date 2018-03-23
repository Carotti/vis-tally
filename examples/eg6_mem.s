; loading and storing single memory
mov r0, #0x1000
mov r1, #23

str r1, [r0]

add r1, r1, #0b10
str r1, [r0], #4

add r1, r1, #0b10
add r0, r0, #8
str r1, [r0, #4]

add r1, r1, #0b10
str r1, [r0, #4]!

mov r0, #0x1000

ldr r7, [r0]
ldr r8, [r0], #4

add r0, r0, #8

ldr r9, [r0, #4]
ldr r10, [r0, #4]!


