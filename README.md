<<<<<<< HEAD
# maccth diary
- 10/02/18: Nothing too important to report. I found a team. Nice people. Douglas, a halfling bard, from the realm of Hurstpoint. I must say, he baffles me. He spends his time singing, and wandering the land. He does walk in a funny way. Feet angled like a sextant. Probably because he wanders too much. He left us early. He said he had some business to clear up. Probably some dark stuff he doesn't want to tell us about. I'll watch him closely. But you see, you can always trust a man with angled flappers. That's one thing my father taught me. The other, a gnome from the Brocken. He speaks fast and generally holds a confused face. He seems to have good intentions. He said he's off on a big trip to Amerika if he makes it out of this alive. We probably won't. We camped out at his hut in the old fort of general H. Ammersmith and delegated tasks. Team moral is on a high. I'll keep an eye on the bard, but trust the gnome.

- 11/02/18: The gnome and I trekked to the safe haven of South Kensingbert. We spent the morning planning. We generally worked alone, and I guess we will continue to do so for the next three or so weeks. A good tactic is to spend the first three weeks of war fighting alone. No sign of the bard. He also did some planning, and had it sent by Gittad, a pigeon we have invested in. The bard's planning generally looks quite similar to what the gnome produced. Interesting, right? In the afternoon, the gnome went to see Heimdall and Magsonian. No one is sure how useful Heimdall ever is, but I trust the gnome is working towards the betterment of our chances as a group in the war to come.

- 12/02/18: The war started yesterday. Horrendous. Absolutely horrendous. I was working on getting the literal parsing cannons up and running to fire at the flexible second operands. I was surrounded on all sides. flexOp2s everywhere. Can you imagine the scenes. I was knocked out by an ADDSEQ and taken into the enemy camp. Luckily, and by some tremendously large miracle, my unconscious body made the wise decision to grab a (then not working) literal parsing cannon. So when I regained consciousness in small cage deep inside the flexOp2 camp, I had this wonderful cannon with me. Silly little flexOp2s. The worst part was that this all happened right in front of the gnome, who did very little to help. In fact, as the whole shit show was unfolding, he pulled down his trouser  and just waved his recursive active pattern in my face. I decided that my best chance of getting out of the flexOp2 camp would be to get the cannon working. Lo and behold the bard turns up! He suggests that instead of working on the cannon we should go the cinema and figure out how to get out of here another day. That is one thing I must say about the flexOp2s, they do treat their prisoners of war well.

- 13/02/18: I ask the bard how he got here. He said that he just couldn’t be bothered to do anything, so when the flexOp2s ambushed him, he just decided that it was in the interest of his cultural knowledge of the land to go the flexOp2 camp. It was a part of the land he had never been to. I started to get the feeling that the bard has had a very sheltered life thus far. He enjoyed the film. I had already seen it. We ate turtles for dinner and went to bed.

- 14/02/18: Deep in the enemy camp, I woke up. No bard. Just me and the broken cannon. I got to work immediately. Within a few hours I had a working monad cannon. Hmmmm. Wasn’t this supposed to be an active pattern cannon? I thought so too. But here I was. In the middle of the enemy camp. Beggars can’t be choosers. I pressed on.  I wonder where the hell the bard has gone? No matter. I began sneaking around. I don’t know how it got to that stage. I stood there, face to face with an ADD. The hexadecimal literal glared at my monad cannon. We all appreciated the distinct lack of optional values. The air was thick. The tension was very raw and very real. The kind of tension you could cut though with an anonymous function. The ADD knew I only had a monad cannon. He lit a cohiba cigar, smiling mercilessly at me. We made eye contact. For a moment we could see deep into each other’s soul. He hadn’t always been an ADD instruction. He grew up in Portland, Oregon with a loving family. But then he went off the rails. Started hanging out with the wrong crowd. Hell I think if I asked he would let me go. I wonder what he saw in me. I knew I had to go for it. Suddenly, as all hope began to fade from the world, and all colour began to dry up we both began pattern matching as fast possible. He dodged my first move, went straight for the don’t care, and died. Poor old lad. Laid before me was a choice. I could carry on my escape out of the flexOp2 camp, or, I could Barry’s body back to Portland, Oregon. I decided I should sleep on the decision, I went back to my cage and slept.
=======
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

Success
```
=> lsl r0, r1, #4
Ok {PInstr = IDP (Shift (LSL {Rd = R0;
                              Op1 = Rs R1;
                              Op2 = Some (N 4u);
                              suff = None;}));
    PLabel = None;
    PSize = 4u;
    PCond = Cal;}
```

Fail
```
=> lsl r4, r2, #7, #6
Error (ERRIDP "splitOps did not match with 'op1, op2' or 'op1, op2, op3'")
```

Note: In the parser there are no FlexOp2 types or a proper Literal type as these were worked on by other members of the team, and thus seemed fruitless writing them myself.

### Execution (DPExecution.fs)

The execution of my data processing instructions is found in the file `DPExecution.fs`. The main function `executeDP` is called if `condExe` (A function Tom Carotti wrote first for his code) matches to true.

The instructions are matched and then their relevant execution functions are called, with each of them checking for flags. The Overflow flag check has not been implemented due to not being required for my instructions, however, another team member has implemented the check.

Naturally, as FlexOp2 and Literal were not implemented in the parser the execution of `MOV` may be some what rogue as apposed to the desired execution, however this is fixed on the parsers end not execution once we combine our work.

In the official ARM specification `ASR` has a shift length from 1 to 32, `LSL` from 0 to 31, `LSR` 1 to 32 and `ROR` 1 to 31. I believe though these are not implemented in visual and thus I didn't treat them as a priority, in the final group version they will be added. In `Rs` for the shifts I am currently not limiting the shift amount to the least significant byte (0-255) as this can be easily added on later.

All other functionality of the instruction I implemented is present and correct.

```
=> mov r1, #0xf6
(R0 : 0x0)
(R1 : 0xf6)
(R2 : 0x0)
(R3 : 0x0)
(R4 : 0x0)
(R5 : 0x0)
(R6 : 0x0)
(R7 : 0x0)
(R8 : 0x0)
(R9 : 0x0)
(R10 : 0x0)
(R11 : 0x0)
(R12 : 0x0)
(R13 : 0x0)
(R14 : 0x0)
(R15 : 0x4)
{N = false;
 C = false;
 Z = false;
 V = false;}
```

### Testing (DPTests.fs)

The testing of my data processing instructions is found in the file `DPTests.fs`. All of my implemented shift instructions are tested against visUAL using unit tests.

Singular instructions can be run against visUAL with the initialState of each being able to be configured by the tester. For now they are either the defaultConfig or all zeros. These individual unit tests check all the different types of Literals such as binary, hex, dec and registers as well as all the shift instructions both with and without their suffixes.

A test list can also be created. A recursive function then individually runs each of the instructions in the test list and passes on the created DataPath to the next instruction thus the visUAL output of a larger program can be easily compared against my implementation. This I believe will be most useful for testing in the later phase of the project when large programs with all instructions can be run.

<<<<<<< HEAD
>>>>>>> Half way through readme
=======
## Memory Instructions

### Parsing (Memory.fs)

The parsing of my memory instructions is found in the file `Memory.fs`. I have two records for handling the two types of memory instruction, these are `InstrMemSingle` and `InstrMemMult`, which deal with LDR, STR and LDM, STM respectively. The former also accepts the suffix `B` for the storage/loading of Bytes. The latter handles all the {addr_modes} `IA`, `IB`, `DA`, `DB`, `FD`, `ED`, `FA`, `EA` as suffixes and parses them into the record.

`LDM` and `STM` are of the following form `op{addr_mode}{cond} Rn{!}, reglist`

There are two parsing functions for each type of instrucion (single, multiple). I shall discuss single first. These instructions can take a few different forms e.g. `ldr r0, [r1]`, `ldr r0, [r1], #4`, `ldr r0, [r1], r2`, `ldr r0, [r1, #4]`, `ldr r0, [r1, r2]`, `ldr r0, [r1, #4]!`, `ldr r0, [r1, r2]!` and the same for `STR` and with the suffix `B`. All of these forms parse correctly. Unprivileged loads/stores `op{type}T{cond} Rt, [Rn {, #offset}]` are not being parsed at this current time as well as register offset such as `op{type}{cond} Rt, [Rn, Rm {, LSL #n}]` as Chris has been working on FlexOp2 as previous mentioned. Additionally, two word storing is not being parsed e.g. `opD{cond} Rt, Rt2, [Rn {, #offset}]`

Multiple load and store instructions as mentioned use a different parsing function. Thse instructions can also take different forms e.g. `ldm r0, {r1, r2, r3}`,  `ldm r0, {r1 - r3}`, `ldm r0!, {r1, r2, r3}`,  `ldm r0!, {r1 - r3}`. These also parse without error. However, changes will need to occur as although the `!` parses the resulting record has no indication that a bang was present thus in execution the address cannot be updated.

Success
```
=> ldm r0, {r2-r7}
Ok {PInstr = IMEM (Mem (LDM {Rn = R0;
                             rList = RegList [R2; R3; R4; R5; R6; R7];
                             suff = None;}));
    PLabel = None;
    PSize = 4u;
    PCond = Cal;}
```

Fail
```
=> ldm r0, {r4-r6} 9
Error (ERRIMEM "Registers probably not valid")
```


### Execution (MemExecution.fs)

The execution of my memory instructions is found in the file `MemExecution.fs`. This is very similar in structure to the `DPExecution.fs` file. From my testing I believe all implementation of parsed single memory instructions is functional. Words can be loaded and stored as well as Bytes. Pre and post indexing works as well as the `!` for both. 

`LDM` and `STM` both are mostly functional with them storing/loading multiple words at a time. As highlighted currently there is no implementation of updating the address when a writeback suffix (`!`) is present. All the suffixes produce a correct offset list so that memory is stored/loaded in the correct order.

```
=> ldm r1, {r3-r6}
(R0 : 0x4)
(R1 : 0x100)
(R2 : 0x0)
(R3 : 0x4)
(R4 : 0x4)
(R5 : 0x4)
(R6 : 0x4)
(R7 : 0x0)
(R8 : 0x0)
(R9 : 0x0)
(R10 : 0x0)
(R11 : 0x0)
(R12 : 0x0)
(R13 : 0x0)
(R14 : 0x0)
(R15 : 0x34)
{N = false;
 C = false;
 Z = false;
 V = false;}
(WA 256u, DataLoc 4u)
(WA 260u, DataLoc 4u)
(WA 264u, DataLoc 4u)
(WA 268u, DataLoc 4u)
```

### Testing (MemTests.fs)

A framework has been written very similar to the `DP` testing. It was a nightmare to get working and some tests pass and others do not this however is not due to the execution but trying to get the testing right as Preludes and Postludes had to be altered. Learnt a lot in this section though. 

`LDR` works with pre and post indexing for these tests. `STR` works with pre and post indexing however it has to have pre as the base address is set to 0x1000 as well as the registers. Note though that the correct values do appear in the output of my functions, the visUAL test framework will require much work for the group project.

`LDM` and `STM` sadly fail the tests due the post having `LDMIA R13, {R0-R12}` in the post. Which overwrites any `LDM` run for example. This can be fixed with more work though.

## Other

The other files that are used in the project, but are not DP or Mem specific. 

### All round (Helpers.fs)

Find functions for setting memory and registers as well as matching regexes and splitting strings and many more! A few trivial property based tests were written to test some core functions.

### Execution (ExecutionTop.fs and Execution.fs)

These contain code for calling the correct execute function depending on whether the instruction is a Memory or DP type. They also have basic DataPaths and conditional execution dependant upon flags.

### Testing (Test.fs)

Here there is code for converting between visUAL output and a DataPath in order to compare the two for visUAL based testing. There are many useful functions for transforming VisOutputs into DataPaths. Also my own RunVisual functions to try to get testing working for memory instructions.






>>>>>>> Maybe finished the readme?
