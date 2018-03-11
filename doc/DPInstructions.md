# Data Processing Instructions Guide
## ADD, ADC, SUB, SBC, RSB
```
op{S}{cond} {Rd,} Rn, Operand2
op{cond} {Rd,} Rn, #imm12			; ADD and SUB only
```
`S` Is an optional suffix. If S is specified, the condition code flags are updated on the result of the operation, see Conditional execution.

`cond` Is an optional condition code, see Conditional execution.

`Rd` Specifies the destination register. If Rd is omitted, the destination register is Rn.

`Rn` Specifies the register holding the first operand.

`Operand2` Is a flexible second operand. See Flexible second operand for details of the options.

`imm12` This is any value in the range 0-4095.

## AND, ORR, EOR, BIC, ORN
```
op{S}{cond} {Rd,} Rn, Operand2
```
`S` Is an optional suffix. If S is specified, the condition code flags are updated on the result of the operation, see Conditional execution.

`cond` Is an optional condition code, see Conditional execution.

`Rd` Specifies the destination register.

`Rn` Specifies the register holding the first operand.

`Operand2` Is a flexible second operand. See Flexible second operand for details of the options.

## ASR, LSL, LSR, ROR, RRX
```
op{S}{cond} Rd, Rm, Rs
op{S}{cond} Rd, Rm, #n
RRX{S}{cond} Rd, Rm
```
`S` Is an optional suffix. If S is specified, the condition code flags are updated on the result of the operation, see Conditional execution.

`Rd` Specifies the destination register.

`Rm` Specifies the register holding the value to be shifted.

`Rs` Specifies the register holding the shift length to apply to the value in Rm. Only the least significant byte is used and can be in the range 0 to 255.

`n` Specifies the shift length. The range of shift length depends on the instruction:
* ASR shift length from 1 to 32
* LSL shift length from 0 to 31
* LSR shift length from 1 to 32
* ROR shift length from 1 to 31.

## CLZ
```
CLZ{cond} Rd, Rm
```
`cond` Is an optional condition code, see Conditional execution.

`Rd` Specifies the destination register.

`Rm` Specifies the operand register.

## CMP, CMN
```
CMP{cond} Rn, Operand2
CMN{cond} Rn, Operand2
```
`cond` Is an optional condition code, see Conditional execution.

`Rn` Specifies the register holding the first operand.

`Operand2` Is a flexible second operand. See Flexible second operand for details of the options.

## MOV, MVN
```
MOV{S}{cond} Rd, Operand2
MOV{cond} Rd, #imm16
MVN{S}{cond} Rd, Operand2
```
`S` Is an optional suffix. If S is specified, the condition code flags are updated on the result of the operation, see Conditional execution.

`cond` Is an optional condition code, see Conditional execution.

`Rd` Specifies the destination register.

`Operand2` Is a flexible second operand. See Flexible second operand for details of the options.

`imm16` This is any value in the range 0-65535.

## MOVT
```
MOVT{cond} Rd, #imm16
```
`cond` Is an optional condition code, see Conditional execution.

`Rd` Specifies the destination register.

`imm16` Is a 16-bit immediate constant.

## REV, REV16, REVSH, RBIT
```
op{cond} Rd, Rn
```
`cond` Is an optional condition code, see Conditional execution.

`Rd` Specifies the destination register.

`Rn` Specifies the register holding the operand.

## TST, TEQ
```
TST{cond} Rn, Operand2
TEQ{cond} Rn, Operand2
```
`cond` Is an optional condition code, see Conditional execution.

`Rn` Specifies the register holding the first operand.

`Operand2` Is a flexible second operand. See Flexible second operand for details of the options.




