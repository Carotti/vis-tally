`FILL` instruction accepts both an expression for the number of bytes to fill and the value to put in them. At present, valueSize is hard-coded to 1 and cannot be input. Might be added later.

`EQU` execution doesn't actually do anything, since this will happen during the symbol resolution phase.

Anywhere where there is a mathematical expression, as in visUAL it is supported except that now operator precedence between `+` `-` and `*` is preserved. All expressions can also be bracketed. Literal formats are the same as specified by visUAL