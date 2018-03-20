# Doug (King) and Maccth (Sultan) Done
- Combining all instruction types for parsing
- Spread parsing errors to all instruction types
- Combining all instruction types for execution
- Spread execution errors to all instruction types
- Converted shift and MOV/MVN instructions to universal DP type and added support for FlexOp2
- Execution for all DP
- Dealing with a list of instructions (as a string list)
- Correctly dealing with empty lines
- Integrating line number to instruction list
- Adding label strings to a symbol table prior to execution
- Adding program counter for non-MISC instructions to symbol table
- Misc instructions executing
- Storing instructions in memory
- Adding multiple DCDs

# Doug (King) and Maccth (Sultan)
- Ask Nippy about `Currently assume that valueSize is always 1` in MiscExecution.fs
- Adding multiple DCDs to memory and ST
- Maintaining line number to currently executing instruction
- Correctly executing a sequence of instructions
- Spreading error monads for execution
- Debugging DP execution (add tests)
- Fix byte in MEM (add tests)
- Create headless mode
- Terminal REPL
- Marrying the GUI
- Removing code redunduncy
- Make helper documentation