<<<<<<< HEAD
# maccth diary
- 10/02/18: Nothing too important to report. I found a team. Nice people. Douglas, a halfling bard, from the realm of Hurstpoint. I must say, he baffles me. He spends his time singing, and wandering the land. He does walk in a funny way. Feet angled like a sextant. Probably because he wanders too much. He left us early. He said he had some business to clear up. Probably some dark stuff he doesn't want to tell us about. I'll watch him closely. But you see, you can always trust a man with angled flappers. That's one thing my father taught me. The other, a gnome from the Brocken. He speaks fast and generally holds a confused face. He seems to have good intentions. He said he's off on a big trip to Amerika if he makes it out of this alive. We probably won't. We camped out at his hut in the old fort of general H. Ammersmith and delegated tasks. Team moral is on a high. I'll keep an eye on the bard, but trust the gnome.

- 11/02/18: The gnome and I trekked to the safe haven of South Kensingbert. We spent the morning planning. We generally worked alone, and I guess we will continue to do so for the next three or so weeks. A good tactic is to spend the first three weeks of war fighting alone. No sign of the bard. He also did some planning, and had it sent by Gittad, a pigeon we have invested in. The bard's planning generally looks quite similar to what the gnome produced. Interesting, right? In the afternoon, the gnome went to see Heimdall and Magsonian. No one is sure how useful Heimdall ever is, but I trust the gnome is working towards the betterment of our chances as a group in the war to come.

- 12/02/18: The war started yesterday. Horrendous. Absolutely horrendous. I was working on getting the literal parsing cannons up and running to fire at the flexible second operands. I was surrounded on all sides. flexOp2s everywhere. Can you imagine the scenes. I was knocked out by an ADDSEQ and taken into the enemy camp. Luckily, and by some tremendously large miracle, my unconscious body made the wise decision to grab a (then not working) literal parsing cannon. So when I regained consciousness in small cage deep inside the flexOp2 camp, I had this wonderful cannon with me. Silly little flexOp2s. The worst part was that this all happened right in front of the gnome, who did very little to help. In fact, as the whole shit show was unfolding, he pulled down his trouser  and just waved his recursive active pattern in my face. I decided that my best chance of getting out of the flexOp2 camp would be to get the cannon working. Lo and behold the bard turns up! He suggests that instead of working on the cannon we should go the cinema and figure out how to get out of here another day. That is one thing I must say about the flexOp2s, they do treat their prisoners of war well.

- 13/02/18: I ask the bard how he got here. He said that he just couldn’t be bothered to do anything, so when the flexOp2s ambushed him, he just decided that it was in the interest of his cultural knowledge of the land to go the flexOp2 camp. It was a part of the land he had never been to. I started to get the feeling that the bard has had a very sheltered life thus far. He enjoyed the film. I had already seen it. We ate turtles for dinner and went to bed.

- 14/02/18: Deep in the enemy camp, I woke up. No bard. Just me and the broken cannon. I got to work immediately. Within a few hours I had a working monad cannon. Hmmmm. Wasn’t this supposed to be an active pattern cannon? I thought so too. But here I was. In the middle of the enemy camp. Beggars can’t be choosers. I pressed on.  I wonder where the hell the bard has gone? No matter. I began sneaking around. I don’t know how it got to that stage. I stood there, face to face with an ADD. The hexadecimal literal glared at my monad cannon. We all appreciated the distinct lack of optional values. The air was thick. The tension was very raw and very real. The kind of tension you could cut though with an anonymous function. The ADD knew I only had a monad cannon. He lit a cohiba cigar, smiling mercilessly at me. We made eye contact. For a moment we could see deep into each other’s soul. He hadn’t always been an ADD instruction. He grew up in Portland, Oregon with a loving family. But then he went off the rails. Started hanging out with the wrong crowd. Hell I think if I asked he would let me go. I wonder what he saw in me. I knew I had to go for it. Suddenly, as all hope began to fade from the world, and all colour began to dry up we both began pattern matching as fast possible. He dodged my first move, went straight for the don’t care, and died. Poor old lad. Laid before me was a choice. I could carry on my escape out of the flexOp2 camp, or, I could Barry’s body back to Portland, Oregon. I decided I should sleep on the decision, I went back to my cage and slept.
=======
# Specification

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

>>>>>>> Half way through readme
