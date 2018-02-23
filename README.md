# Specification

## How to use?

The program can being run using with `dotnet run` plus various arguments in order to test different aspects.

`dotnet run xrepl` will run a REPL executing ARM instructions and displaying registers, flags and memory.

`dotnet run prepl` will run a REPL parsing ARM instructions and displaying the output records.

`dotnet run xlist` will run execute all the instructions in a given list and display the registers, flags and memory.

`dotnet run vtests` will run the visUAL based tests.

`dotnet run tests` will run other non-visUAL tests.

`dotnet run` will parse the input list and display the output records.

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

## Memory Instructions

### Parsing (Memory.fs)

The parsing of my memory instructions is found in the file `Memory.fs`. I have two records for handling the two types of memory instruction, these are `InstrMemSingle` and `InstrMemMult`, which deal with LDR, STR and LDM, STM respectively. The former also accepts the suffix `B` for the storage/loading of Bytes. The latter handles all the {addr_modes} `IA`, `IB`, `DA`, `DB`, `FD`, `ED`, `FA`, `EA` as suffixes and parses them into the record.

`LDM` and `STM` are of the following form `op{addr_mode}{cond} Rn{!}, reglist`

There are two parsing functions for each type of instrucion (single, multiple). I shall discuss single first. These instructions can take a few different forms e.g. `ldr r0, [r1]`, `ldr r0, [r1], #4`, `ldr r0, [r1], r2`, `ldr r0, [r1, #4]`, `ldr r0, [r1, r2]`, `ldr r0, [r1, #4]!`, `ldr r0, [r1, r2]!` and the same for `STR` and with the suffix `B`. All of these forms parse correctly. Unprivileged loads/stores `op{type}T{cond} Rt, [Rn {, #offset}]` are not being parsed at this current time as well as register offset such as `op{type}{cond} Rt, [Rn, Rm {, LSL #n}]` as Chris has been working on FlexOp2 as previous mentioned. Additionally, two word storing is not being parsed e.g. `opD{cond} Rt, Rt2, [Rn {, #offset}]`

Multiple load and store instructions as mentioned use a different parsing function. Thse instructions can also take different forms e.g. `ldm r0, {r1, r2, r3}`,  `ldm r0, {r1 - r3}`, `ldm r0!, {r1, r2, r3}`,  `ldm r0!, {r1 - r3}`. These also parse without error. However, changes will need to occur as although the `!` parses the resulting record has no indication that a bang was present thus in execution the address cannot be updated.

### Execution (MemExecution.fs)

The execution of my memory instructions is found in the file `MemExecution.fs`. This is very similar in structure to the `DPExecution.fs` file. From my testing I believe all implementation of parsed single memory instructions is functional. Words can be loaded and stored as well as Bytes. Pre and post indexing works as well as the `!` for both. 

`LDM` and `STM` both are mostly functional with them storing/loading multiple words at a time. As highlighted currently there is no implementation of updating the address when a writeback suffix (`!`) is present. All the suffixes produce a correct offset list so that memory is stored/loaded in the correct order.

### Testing (MemTests.fs)

A framework has been written very similar to the `DP` testing, however as of yet no tests.
Feel free to use the REPL or Lists though to see that the execution is correct and functional.

## Other
The other files that are used in the project, but are not DP or Mem specific. 

### All round (Helpers.fs)
Find functions for setting memory and registers as well as matching regexes and splitting strings and many more! A few trivial property based tests were written to test some core functions.

### Execution (ExecutionTop.fs and Execution.fs)
These contain code for calling the correct execute function depending on whether the instruction is a Memory or DP type. They also have basic DataPaths and conditional execution dependant upon flags.

### Testing (Test.fs)
Here there is code for converting between visUAL output and a DataPath in order to compare the two for visUAL based testing.






