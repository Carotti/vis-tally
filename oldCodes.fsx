        let parse' (instrC, (root,suffix,pCond)) =
            let (WA la) = ls.LoadAddr // address this instruction is loaded into memory
            this does the real work of parsing
            dummy return for now
            Ok { 
                Normal (non-error) return from result monad
                This is the instruction determined from opcode, suffix and parsing
                the operands. Not done in the sample.
                Note the record type returned must be written by the module author.
                PInstr={DPDummy=()}; 


                This is normally the line label as contained in
                ls together with the label's value which is normally
                ls.LoadAddr. Some type conversion is needed since the
                label value is a number and not necessarily a word address
                it does not have to be div by 4, though it usually is
                PLabel = ls.Label |> Option.map (fun lab -> lab, la) ; 


                this is the number of bytes taken by the instruction
                word loaded into memory. For arm instructions it is always 4 bytes. 
                For data definition DCD etc it is variable.
                 For EQU (which does not affect memory) it is 0
                PSize = 4u; 

                the instruction condition is detected in the opcode and opCodeExpand                 
                has already calculated condition already in the opcode map.
                this part never changes
                PCond = pCond 
            }