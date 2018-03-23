; flexible second operand
mov r10, #1
add r0, r0, r10, lsl #5
add r1, r0, r0, asr #0b10
ror r2, r1, r0
sub r4, r0, r1, ror r0
sub r5, r4, r1, rrx
adds r0, r0, r0, lsr r2
