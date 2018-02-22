# Specification

All opCodes are forced to upper case before checking which allows instructions to have any mixture of upper and lower case letters in them. 

## Misc Instructions (Misc.fs)

`FILL` instruction accepts both an expression for the number of bytes to fill and the value to put in them. At present, the value size is hard-coded to 1 and cannot be input. Might be added later.

`SPACE` can also be used to fill with zeroed memory, unlike Visual where `FILL` does this, but without accepting a value to be filled.

`FILL` and `SPACE` optionally accept a label, `EQU`, `DCB` and `DCD` require it.

All `MISC` instruction have `PSize` set to `0u` and `PCond` to `Cal` just for consistent meaning. These are not instructions placed in memory, so have no size and they cannot have conditions so are always executed.

`EQU` execution doesn't actually do anything, since this will happen during the symbol resolution phase. The symbol binding will be done during group stage. Somehow a symbol table will need to be built from all instructions which have labels.

`DCB` is Little Endian, the same as visUAL.
`DCD` should also behave the same way as visUAL.

If during resolution, a `DCB` expression evaluates to a value larger than `255` then an error is thrown, as visUAL does.

`DCD` values which "overflow" are allowed, as they are in visUAL, in that they are modulo `2^32`.

## Branch Instructions (Branch.fs)

`END` is supported by returning an EXIT runtime Error. This will be caught in the global execution function

`B` and `BL` supported with all condition codes.

## Symbol Resolution

Since `B`, `BL`, `EQU`, `DCB`, `DCD`, `SPACE` and `FILL` all require a label of some sort which may not necessarily be known, both `MISC` instructions and `BRANCH` instructions have a `resolve` function which can resolve these labels against a currently known symbol table. In the caseof `EQU`, `DCB`, `DCD`, `SPACE` and `FILL`, these may also be expressions which are also evaluated in `resolve`. `B` and `BL` simply match the labels which they branch to. This means that these instructions only have to be parsed once, but there will be a seperate symbol resolution phase, the semantics of which are undecided at present (multi-pass or dependency detection!?). This means the flow for using this module is `parse` -> `resolve` -> `execute`. Trying to execute any `MISC` instruction which hasn't been resolved currently causes a failure, so it is important to do `resolution` and check that it has occured correctly on an instruction before executing it. 

## Expressions

Anywhere where there is a mathematical expression, as in visUAL it is supported.

The `Expression` module can be used to match epxressions and parse them into the `Expression` AST for later execution. This is tested in `ExpressionTest` and tests a number of literals in varying generated formats given in `TestFormat`. This allows for randomly generating a literal and its format. A number of other tests ensure operator precedence is correct, bracketing works correctly as well as a few unit tests for sanity. This module should be reasonably standalone and rightly so, since it is potentially used by a number of classes of instructions.

Currently, the operators `+` `-` and `*` are supported, but this is *very* easily expanded to new operators with desired precedence.

## Execution

The `Execution` module contains some helper functions for manipulating datapaths as well as the function `condExecute` which determines whether an instruction should be executed based on its condition code and the current state of the flags in the datapath. `updateMemByte` can update byte addressed memory (This also means that WA is byte addressed, so valid word addresses are always divisible by 4).`updateMemByte` can only be called on address which are either empty or data locations, this is because it needs to do some shfiting and masking to modify a particular byte. Perhaps better later on to catch this error as user trying to do something fishy with instruction memory.

`execute` for the misc instructions accepts a tuple of `(mem, dp)` and a `ins` which is to be executed. What this does is performs the memory directive at location `mem` in `dp` and then will return a new `(mem, dp)` with mem the next location where data can be put into memory for when successive directives are used. No error can occur here. `DCD` will start placing at the next aligned address after `mem`, if `mem` itself is not aligned but `DCB` will start placing byte by byte at `mem`.

`execute` for `Branch` just accepts a datapath and a branch instruction and will execute the instruction against the datapath. At present the only error that can occur is the `EXIT` error indicating that an `END` instruction has been condition true executed.

# Testing 

`B`, `BL` and `END` are tested using carefully constructed unit tests and various condition codes in `BranchTest`. `makeBTests` will test a number of different flag settings and whether the instrction is expected to be executed based on the condition code and these flags. It also works out based on the operand the expected output of condition true and false execution. For `B`, condition true is PC set to `4444u` (the label `branchTarget` in ts). For BL it is the same, but with the link register set to `4u`, since this would be the next instruction after the branch. This led to some interesting discoveries. The importance of carefully manually typed unit tests led to the discovery that I had accidentally set the c flag to the v flag in one of the test production functions causing it to fail. Because `condExecute` is so easy to reason about from these tests, I am confident in its behaviour and therefore reusability for the rest of the instructions.

`DCB` and `DCD` are tested directly against visUAL using the functions `sameAsVisualDCB` and `sameAsVisualDCD` which run the same instructions against visual. The visUAL prelude will contain the test symbol table `ts` defined in `TestFormats`. This is because one format option for randomly generated expressions are symbols. These are resolved when executing my version, and `EQU` is used to define them in VisUAL. A random list of these expressions are generated, which could be literals of any format or labels. Only up to 13 words of memory are generated since that is all that can be received from the test framework at the output of visUAL. The only things that are compared after testing these directive instructions is the first 13 words of memory from the assumed memory base. Another issue in this testing was that getting the memory out of the visUAL framwork will set the memory locations to zero, whereas in my data path, they would simply be unset. This meant I had to write a function to ignore the zeroed memory before comparing it.

`FILL`, `SPACE` and `EQU` are tested with their own unit test constructors ensuring that the output of executing them after resolution is as expected. `EQU` is expected to be a `ExpResolved` of `uint32` with the value equal to the value of the evaluated right hand side. This led to the discovery that I had accidentally forced `SPACE` and `FILL` to accept a label in the same way that `DCB`, `DCD` and `EQU` do. I modified the code and fixed this.
