`B` and `BL` both accept a label as the only argument, as expected. They can also accept an expression e.g. `label + 3`, However they do not support `#` in front of literals for an explicit branch, instead the hash is omitted.

Memory is little endian

Symbols can be multiple forward dependent on each other. Note that filling by an amount which depends on the value of the fill results in undefined behaviour.

The `FILL` value size is always assumed to be 1 and cannot be specified.

# Order of operations
Every line is parsed, if any parse errors exist, these are displayed
Instructions are placed in memory.

A memory offset base is calculated by rounding up from instruction memory to the nearest hexadecimal hundred (`0x100` etc). From here, the data directives are placed