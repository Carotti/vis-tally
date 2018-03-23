# Misc instruction notes

`FILL` instruction accepts both an expression for the number of bytes to fill and the value to put in them. At present, the value size is hard-coded to 1 and cannot be input. Might be added later.

`SPACE` can also be used to fill with zeroed memory, unlike Visual where `FILL` does this, but without accepting a value to be filled.

`FILL`, `SPACE`, `EQU`, `DCB` and `DCD` all require labels (`FILL` and `SPACE` wouldn't usually, but we require it for sanity)

`FILL` and `SPACE` are currently broken due to variable sizes possibly not being knowne when they are placed, but they do work on their own.

`DCB` is Little Endian, the same as visUAL.
`DCD` should also behave the same way as visUAL.

If during resolution, a `DCB` expression evaluates to a value larger than `255` then an error is thrown, as visUAL does.

`DCD` values which "overflow" are allowed, as they are in visUAL, in that they are modulo `2^32`.

`B` and `BL` both accept a label as the only argument, as expected. They can also accept an expression e.g. `label + 3`, However they do not support `#` in front of literals for an explicit branch, instead the hash is omitted.

Memory is little endian

Symbols can be multiple forward dependent on each other. Note that filling by an amount which depends on the value of the fill results in undefined behaviour.

The `FILL` value size is always assumed to be 1 and cannot be specified.

# Memory instruction notes

`LDR` and `STR` instruction correctly loads/stores a word from/at a given address, with the suffix `B` it will load/store a byte. It is executing correctly with no offsets, pre-offsets, post-offsets and both. These offsets can be either a number or register. Unprivileged loads/stores `op{type}T{cond} Rt, [Rn {, #offset}]`, Register FlexOp2 `op{type}{cond} Rt, [Rn, Rm {, LSL #n}]` and two word memory instructions `opD{cond} Rt, Rt2, [Rn {, #offset}]` are not being parsed.

`LDM` and `STM` accept all suffixes `IA`, `IB`, `DA`, `DB`, `FD`, `ED`, `FA`, `EA` and execute them correctly. All forms of these instructions are parsed, `op{addr_mode}{cond} Rn{!}, reglist` however, the writeback suffix `!` does not update the address register during execution so is not functional.

# Order of operations
Every line is parsed, if any parse errors exist, these are displayed
Instructions are placed in memory.

A memory offset base is calculated by rounding up from instruction memory to the nearest hexadecimal hundred (`0x100` etc). From here, the data directives are placed in memory based on their size, although the values placed are not resolved yet.

At this point, only `EQU` instructions could create new labels, so these are attempted to be resolved. This is done by going through all unresolved `EQU`s, trying to resolve them. If the symbol table doesn't change after a pass and there are still unresolved `EQU`s then these unresolved `EQU`s are impossible to resolve and an error is returned. At this point, a full symbol table is build so the directives are evaluated and placed against the table. Errors can occur here if the directives cannot be resolved. Finally, we go back throught the instructions in memory, looking for `ADR`, `B` and `BL` instructions which can also depend on labels. These are then resolved against the complete symbol table, possibly returning an error.

Throughout, line numbers are kept track of for both parsing and symbol resolution allowing the line to be specified where an error occurred.

# Testing

Module level testing is done independently on seperate branches due to operating system issues. Corner cases are tested using hand-crafted unit tests running on the integrated modules in `Test.fs`.

