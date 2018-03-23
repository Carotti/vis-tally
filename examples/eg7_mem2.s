; basic dcd and mult mem instructions
foo dcd 10, 11, 12, 13, 14

adr r1, foo
ldm r1, {r2-r6}

ldmia r1, {r7, r8}

add r1, r1, #8
ldmdb r1, {r0, r3}

mov r5, #0
mov r6, #1
mov r7, #2

stmib r1, {r5-r7}