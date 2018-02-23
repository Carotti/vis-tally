# Some Highlights
Functions for handling multiple `Result` monads. All in `DP.fs`.
```fsharp

let combineError (res1:Result<'T1,'E>) (res2:Result<'T2,'E>) : Result<'T1 * 'T2, 'E> =
    match res1, res2 with
    | Error e1, _ -> Error e1
    | _, Error e2 -> Error e2
    | Ok rt1, Ok rt2 -> Ok (rt1, rt2)


let combineErrorMapResult (res1:Result<'T1,'E>) (res2:Result<'T2,'E>) (mapf:'T1 -> 'T2 -> 'T3) : Result<'T3,'E> =
    combineError res1 res2
    |> Result.map (fun (r1,r2) -> mapf r1 r2)

let applyResultMapError (res:Result<'T1->'T2,'E>) (arg:Result<'T1,'E>) =
    match arg, res with
    | Ok arg', Ok res' -> res' arg' |> Ok
    | _, Error e -> e |> Error
    | Error e, _ -> e |> Error
```

Higher order function `execute` to handle execution of instructions. In `DPExecution.fs`.

The `List.fold` is used to weave the data path through all the checks for CPSR flags which are passed as a list.
```fsharp
let execute dp func dest op1 op2 suffix flagTests : (Result<DataPath<Instr>,ErrExe>) =
    let result = func op1 op2
    let dp' =
        match dest with
        | Some destReg -> updateReg destReg result dp
        | None -> dp
    match suffix with
    | Some S ->
        flagTests
        |> List.collect id
        |> List.fold (fun flags test -> test flags) (dp'.Fl, op1, op2, result)
        |> fun (f, _op1, _op2, _res) -> f
        |> fun f -> {dp' with Fl = f}
        |> Ok
    | None ->
        dp'
        |> Ok
```
An example of how the `execute` function is used. Also in `DPExecution.fs`.

```Fsharp
match opcode with
   | ADD _ -> execute dp' (fun op1 op2 -> op1 + op2) dest op1 op2 operands.suff [CVCheckAdd; NZCheck]
   | ADC _ -> execute dp' (fun op1 op2 -> op1 + op2) dest (op1+C) op2 operands.suff [CVCheckAdd; NZCheck]
   | SUB _ -> execute dp' (fun op1 op2 -> op1 - op2) dest op1 op2 operands.suff [CVCheckSub; NZCheck]
   | SBC _ -> execute dp' (fun op1 op2 -> op1 - op2) dest op1 (op2 + (Cb |> not |> System.Convert.ToUInt32)) operands.suff [CVCheckSub; NZCheck]
   | RSB _ -> execute dp' (fun op1 op2 -> op1 - op2) dest op2 op1 operands.suff [CVCheckSub; NZCheck]
   | RSC _ -> execute dp' (fun op1 op2 -> op1 - op2) dest op2 (op1 + (Cb |> not |> System.Convert.ToUInt32)) operands.suff [CVCheckSub; NZCheck]
   | AND _ -> execute dp' (fun op1 op2 -> op1 &&& op2) dest op1 op2 operands.suff [NZCheck]
   | ORR _ -> execute dp' (fun op1 op2 -> op1 ||| op2) dest op1 op2 operands.suff [NZCheck]
   | EOR _ -> execute dp' (fun op1 op2 -> op1 ^^^ op2) dest op1 op2 operands.suff [NZCheck]
   | BIC _ -> execute dp' (fun op1 op2 -> op1 &&& (~~~op2)) dest op1 op2 operands.suff [NZCheck]
```



# Contribution to Group
## Flexible Second Operand
I parse into a data domain type model all forms of the flexible second operand. For example for `ADDS r1, r2, r3, LSL #0xf0000002`
```
Ok {PInstr = IDP (DP3S (ADD {rDest = R1;
                             rOp1 = R2;
                             fOp2 = Shift {rOp2 = R3;
                                           sInstr = LSL;
                                           sOp = ConstShift {b = 47uy;
                                                             r = Rot4;};};
                             suff = Some S;}));
    PLabel = None;
    PSize = 4u;
    PCond = Cal;}
```
The modelling is done with a number of DUs with `FlexOp2` at the top level, making it easy to integrate into an existing code base simply by adding in the DUs.
I also execute data processing instructions using this model. Since all possible values are defined within their DUs, it is easy to get information out by series of pattern matching in order to unpack the types. This means that even if my execution code isn't used. The data modelling can be. A bonus of this tight-typing is that `Result` monads can flow through very easily. Furthermore, in order to establish a well defined interface between Doug and Tom's potential `Result` monads I have defined descriptive error types:
```Fsharp
type ErrInstr =
    | ``Invalid literal``                   of string
    | ``Invalid register``                  of string
    | ``Invalid shift``                     of string
    | ``Invalid flexible second operand``   of string
    | ``Invalid suffix``                    of string
    | ``Invalid instruction``               of string
    | ``Syntax error``                      of string
    ```
which allows for the nice error detection mentioned above:
```
~> add r20, r21, r22, rpr #10
Error (ERRIDP (Invalid register "R20 is not a valid register."))

~> add r0, r21, r22, rpr #hello
Error (ERRIDP (Invalid register "R21 is not a valid register."))

~> add r0, r1, r22, rpr #hello
Error
  (ERRIDP
     (Invalid flexible second operand
        "R22, RPR#HELLO is an invalid flexible second operand"))

~> add r0, r1, r2, ror #hello
Error (ERRIDP (Invalid shift "#HELLO is not a valid literal or register."))
```        
## Interfaces
All data processing instructions fit under a top level DU which fans out to two other DUs `DP3S` and `DP2`. `DP3S` for data processing instructions with three operands and an optional `S` suffix, for example `ADD`. `DP2` is for data processing instructions with two operands and no suffix, for example `CMP`. These are, however, made up of the same constituent types:
```Fsharp
type DP3SForm =
    {
        rDest:RName;
        rOp1:RName;
        fOp2:FlexOp2;
        suff:Option<Suffix>;
    }

type DP2Form =
    {
        rOp1:RName;
        fOp2:FlexOp2;
    }
```
and the functions that transform data work on these constituent types, such as `FlexOp2`, rather than `DP3SForm` and `DP2Form`. This means that adding potential `DP2S` (such as `MOV`) instructions from Doug's work is easy since only a top level function (which calls the already existing functions and combines their output) needs to be added.

## Naming Conventions
Modelling things with a data domain types means a lot of _helper_ functions are needed to get things _out of_ or _in to_ a certain format. In order to make my code easy to read and use I have generally stuck to a few naming conventions `cons` followed by something means a constructor function of that _thing_. For example:
```Fsharp
/// Constructs a register name of type `RName` from a register specified as a `string`.
let consReg reg =
    regNames.[reg]
```
# Specification
## Nuances
It was initially unclear which formats of the flexible second operand could affect the CPSR. Originally I thought that logical instructions should affect C only if there is register shift or rotate, not an immediate value. Upon closer inspection of ARM documentation on this, the following excerpt was found:
```
When an Operand2 constant is used with the instructions MOVS, MVNS, ANDS, ORRS, ORNS, EORS, BICS, TEQ or TST, the carry flag is updated to bit[31] of the constant, if the constant is greater than 255 and can be produced by shifting an 8-bit value. These instructions do not affect the carry flag if Operand2 is any other constant.
```
Comparing with VisUAL, the instruction `EORS r1, r2, #0xe0000007` was setting the C flag. We consulted Dr Clarke and agreed that this suggests the specification uses the concept of **shifting** to mean both **shifting** and **rotating**, furthermore, a shift should set C to the last bit shifted out and not necessarily bit 31. We plan to implement this functionality in the group stage. It is **not** currently implemented.

## Testing
### Parser
The parser was tested by first stripping my Computer Architecture 1 coursework (back when I was EIE1) of any instructions that I was not implementing with a simple grep. This code was then passed through and checked. Testing a parser (before writing the execution code) is interesting because it requires a reference parser which in this case I did not have. VisUAL's parsing AFAIK is internal and cannot be obtained from framework provided. At this stage I implemented a REPL for parsing which can be used by entering `p` after running `dotnet run`.
### Execution
Initial execution was tested using property based tests. Something like:
```Fsharp
let visTest3Params src (r0,r1,r2) n c z v =
    let regs = r0 :: r1 :: r2 :: [0u..11u]
    visCompare src regs n c z v

let ADDSpropTest1 =
    let compare src (r0,r1,r2) c z v =
        (visTest3Params src (r0, r1, r2) false c z v)
    testProperty "ADDS, reg, N clear"  <| (compare "ADDS r0, r1, r2")
```
**Note:** in the above code `visCompare` is a function that executes `src`, with registers and flags as passed in, on my implementation and on VisUAL and returns a bool to indicate success or failure.

These worked well because they give a quick indication of whether the general implementation was correct or not. Expecto seems to have an affinity towards [small numbers](https://en.wikipedia.org/wiki/Strong_Law_of_Small_Numbers)... so after some initial property based testing, unit tests against VisUAL were used. Some property based tests have been left in the code as examples.

Unit tests against VisUAL for corner cases provided a good way to test the correctness of implementation.

Finally, some work was done to be able to test a sequence of instructions. Namely the functions `visSequenceTest` and `visCompareSequence`. These are currently not working 100% as expected. The goal is, of course, to be able to run my Computer Architecture 1 coursework.

## Functionality
The instructions I was to implement were:
`ADD` `ADC` `SUB` `SBC` `RSB` `RSC` `AND` `EOR` `BIC` `ORR` `CMP` `CMN` `TST` `TEQ` in all forms.

- Instructions work correctly (using VisUAL as a reference) if not setting the flags (`S` suffix is not used).

- There are also issues with the setting of flags before/after the barrel shifter in the flexible second operand.

**These are currently being addressed.**

- The instruction format `op{cond} {Rd,} Rn, #imm12` is currently not accepted.

# Things learnt
- Data domain modelling using types means one will probably spend longer writing the parsing code than  writing the execution code (a good thing!). This is because unpacking is done quickly through `match` statements. This also provides a strong guarantee that if the instructions reaches execution there will not be a runtime error. This is also interesting since data domain modelling using types allows for some semantic (over the usual syntactic) analysis while parsing: `r30` is caught in the parsing stage!

- Data domain modelling also allows for easier property based testing since. Since invariances about the data are more obvious.

- Active patterns work well with result monads!

# Running things and files
- `DP.fs` contains the parsing code and (parsing helper functions) for data processing instructions.
- `DPExecution.fs` contains the execution code (and execution helper functions) for data processing instructions.
- `DPTests.fs` contains the testing functions (and, you guessed it, the testing helper functions).
- `Tests.fs` contains some tests.

To get things going run `dotnet run` and you will see:
```
Enter...
         'p' for a parsing REPL
         'e' fot an execution REPL
         'v' for a VisUAL comparison REPL
      or 't' to run some cool tests

```
and then the world is your oyster!
