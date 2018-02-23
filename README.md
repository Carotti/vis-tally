# Specification

There were three main stages to this project: parsing, execution and testing. I had a mix of memory and data processing instructions, which I shall discuss below.

## Data Processing Instructions

### Parsing (DP.fs)

The parsing of my data processing instructions is found in the file `DP.fs`. Shift instructions have been implemented here as Christopher Macca worked on the rest. The type of all my instructions in DP was `ShiftInstrType` which can handle `LSL`, `LSR`, `ASR`, `ROR`, `RRX`, `MOV` and `MVN` all also with suffix `S`.

The first four of the list above, `LSL`, `LSR`, `ASR`, `ROR` are all very similar consisting of either `op{S}{cond} Rd, Rm, Rs` or `op{S}{cond} Rd, Rm, #n`, both of which the parser handles.
The latter three `RRX`, `MOV`, `MVN` are also similar being `op{S}{cond} Rd, Rm` or `op{S}{cond} Rd, #n`. I am able to use the same record for all of these due to the use of Option and the creation of a DU which can either be a RName or a uint32.

The parser splits the operands of the instruction at commas and creates a list. Each element of this list is then checked with a partial active pattern and the correct type is constructed. The Result monad is used to catch errors which may have occured thanks to invalid input or other factors.

Note: In the parser there are no FlexOp2 types or a proper Literal type as these were worked on by other members of the team, and thus seemed fruitless writing them myself.

### Execution (DPExecution.fs)

The execution of my data processing instructions is found in the file `DPExecution.fs`. The main function `executeDP` is called if `condExe` (A function Tom Carotti wrote first for his code) matches to true.

The instructions are matched and then their relevant execution functions are called, with each of them checking for flags. The Overflow flag check has not been implemented due to not being required for my instructions, however, another team member has implemented the check.

Naturally, as FlexOp2 and Literal were not implemented in the parser the execution of `MOV` may be some what rogue as apposed to the desired execution, however this is fixed on the parsers end not execution once we combine our work.

In the official ARM specification `ASR` has a shift length from 1 to 32, `LSL` from 0 to 31, `LSR` 1 to 32 and `ROR` 1 to 31. I believe though these are not implemented in visual and thus I didn't treat them as a priority, in the final group version they will be added. In `Rs` for the shifts I am currently not limiting the shift amount to the least significant byte (0-255) as this can be easily added on later.

All other functionality of the instruction I implemented is present and correct.

### Testing (DPTests.fs)

The testing of my data processing instructions is found in the file `DPTests.fs`. All of my implemented shift instructions are tested against visUAL using unit tests.

Singular instructions can be run against visUAL with the initialState of each being able to be configured by the tester. For now they are either the defaultConfig or all zeros. These individual unit tests check all the different types of Literals such as binary, hex, dec and registers as well as all the shift instructions both with and without their suffixes.

A test list can also be created. A recursive function then individually runs each of the instructions in the test list and passes on the created DataPath to the next instruction thus the visUAL output of a larger program can be easily compared against my implementation. This I believe will be most useful for testing in the later phase of the project when large programs with all instructions can be run.

