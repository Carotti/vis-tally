module DP
    open CommonData
    open CommonLex

    [<Struct>]
    type RVal =
        | Rot0     | Rot2     | Rot4     | Rot6
        | Rot8     | Rot10    | Rot12    | Rot14  
        | Rot16    | Rot18    | Rot20    | Rot22 
        | Rot24    | Rot26    | Rot28    | Rot30

    let RNums =
        Map.ofList [
            (Rot0, 0);   (Rot2, 2);      
            (Rot4, 4);   (Rot6, 6);
            (Rot8, 8);   (Rot10, 10);    
            (Rot12, 12); (Rot14, 14);  
            (Rot16, 16); (Rot18, 18);    
            (Rot20, 20); (Rot22, 22); 
            (Rot24, 24); (Rot26, 26);    
            (Rot28, 28); (Rot30, 30);
        ]
    
    let RVals =
        Map.ofList [
            (0, Rot0);   (2, Rot2);      
            (4, Rot4);   (6, Rot6);
            (8, Rot8);   (10, Rot10);    
            (12, Rot12); (14, Rot14);  
            (16, Rot16); (18, Rot18);    
            (20, Rot20); (22, Rot22); 
            (24, Rot24); (26, Rot26);    
            (28, Rot28); (30, Rot30);
    ]
    
    /// Literal type for allowed literals.
    ///  `K` is the underlying byte.
    ///  `R` is the rotation that is applied to `K`.
    type Literal =
        {
            K: byte;
            R: RVal;
        }
        
    /// Possible shift operands for the shift instructions `SIns`.
    type SOp =
        | Const of uint32
        | Regs  of RName
   
    /// Shift instructions that require operands and are compatible with the
    ///  flexiable second operand. `RRX` is not included since it does not
    ///  require any further operands.
    type SInstr =
        | LSL
        | LSR
        | ASR
        | ROR

    // Flexible shift sub-instruction format within the flexiable second operand.
    type FS2Form = {rOp2:RName; sInstr:SInstr; sOp:SOp}

    type FlexOp2 =
        | Lit       of Literal
        | Reg       of RName
        | Shifted   of FS2Form
        | RRX       of RName

    /// Instruction format for three-operand data processing instructions. 
    type DP3Form = {rDest:RName; rOp1:RName; op2:FlexOp2} 

    type Instr =
        | ADD of DP3Form
       
    /// parse error (dummy, but will do)
    type ErrInstr = string

    /// **sample** specification for set of instructions
    /// very incomplete!
    let dPSpec = {
        InstrC = DP
        Roots = ["ADD";"SUB"]
        Suffixes = [""; "S"]
    }

    /// map of all possible opcodes recognised
    let opCodes = opCodeExpand dPSpec

    /// main function to parse a line of assembler
    /// ls contains the line input
    /// and other state needed to generate output
    /// the result is None if the opcode does not match
    /// otherwise it is Ok Parse or Error (parse error string)
    let parse (ls: LineData) : Result<Parse<Instr>,string> option =
        let (WA la) = ls.LoadAddr

        let parseADD suffix pCond =
            Ok {
                // Example values just to see if it type checks
                // TODO: parse operands to get rOp1 and op2
                // TODO: check op2 to see if it is compatible with FlexOp2
                // TODO: create instruction in format to 'run'
                PInstr  = ADD ( {rDest = R10; rOp1 = R12; op2 = Reg (R12)} );
                PLabel  = ls.Label |> Option.map (fun lab -> lab, la);
                PSize   = 4u;
                PCond   = pCond
            }

        let parseFuncs =
            Map.ofList [
                "ADD", parseADD;
            ]

        let parse' (_instrC, (root,suffix,pCond)) =
            parseFuncs.[root] suffix pCond
            
        Map.tryFind ls.OpCode opCodes // lookup opcode to see if it is known
        |> Option.map parse' // if unknown keep none, if known parse it.


    /// Parse Active Pattern used by top-level code
    let (|IMatch|_|) = parse
