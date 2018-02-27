# Feedback

## Notes

cond execution can best be handled just once in top-level code - all module code will be for instructions executing without conditions. I know my preparse stuff, passing everything to indiv modules and back, is confusing. This is not a big deal, but replicated code is to be avoided, and introducing a strong dependency here on a common module is painful when it can be done just once at top level.

There is a trick for the carry/overflow stuff. Subtract a b is actually addList [a (invertBits b) 1] where a 64 bit addition will allow signed and unsigned overflow to be identified if the 32 bit signed and unsigned values are compared with 64 bit. Sort of foolproof if done like this and v complex otherwise.




## Success

This is an insightful and very well documented module, well tested (and therefore mostly working).



## Ambition

This is a lot of code implemented: and it is very well documented. I'm confident it will be very useful to group. Well done.


## Testing

You have an excellent test framework however your actual tests call visUnitTest with repeated parameters. You should have subfunctions for this to reduce the unpleasant boilerplate. Such local subfunctions make things easier. I agree global subfunctions would not because of the overhead in going to the global definition.

Just one example:

```
            visUnitTest "ORRS 13" "ORRS r0, r1, r2"              [10u;11u;12u;13u]   false false false false
            visUnitTest "ORRS 14" "ORRS R0, R1, R2, LSL R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "ORRS 15" "ORRS R0, R1, R2, LSR R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "ORRS 16" "ORRS R0, R1, R2, ASR R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "ORRS 17" "ORRS R0, R1, R2, ROR R3"      [10u;11u;12u;13u]   false false false false
            visUnitTest "ORRS 18" "ORRS R0, R1, R2, RRX "        [10u;11u;12u;13u]   false false false false
```

this should be implemented using something like:
```
let mVUT opc argL =
    argL
    |> List.indexed
    |> List.map (fun (i, args) -> 
        visUnitTest (sprintf "%s %d" opc i) (opc + " " + args) [s..s+4] false false false false
```
You can probably re-use argl many times, but even if not this is a great reduction in unpleasant boilerplate.

## Quality

Excellent types
Outstanding use of error monad processing
excellent understanding of FP programming (though some lack of functional abstraction, see testing)



