; settings flags example
mov r2, #0xff
lsl r2, r2, #3
add r1, r2, #1
subs r3, r2, r1
movs r6, #0
lsl r4, r2, #24
adds r7, r4, r4

mov r0, #0xff
lsl r0, r0, #23
add r0, r0, r0

mov r0, #0
mvn r0, r0
lsr r0, r0, #1
add r0, r0, #1

