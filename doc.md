`FILL` instruction accepts both an expression for the number of bytes to fill and the value to put in them. At present, the value size is hard-coded to 1 and cannot be input. Might be added later.

`SPACE` can also be used to fill with zeroed memory.

`EQU` execution doesn't actually do anything, since this will happen during the symbol resolution phase. It is tested by checking that the expression it is set to is evaluated correctly. The symbol binding will be done during group stage.

Anywhere where there is a mathematical expression, as in visUAL it is supported except that now operator precedence between `+` `-` and `*` is correct. All expressions can also be bracketed. Literal formats are the same as specified by visUAL.

`DCB` is Little Endian, the same as visUAL. 