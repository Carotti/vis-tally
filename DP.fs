//////////////////////////////////////////////////////////////////////////////////////////
//                   Sample (skeleton) instruction implementation modules
//////////////////////////////////////////////////////////////////////////////////////////

module DP
    open CommonData
    open CommonLex
    open System.Text.RegularExpressions


    ////////////////////////////////////////////////////////////////////////////////
    // maccth helper functions, TODO: delete 
    let qp thing = thing |> printfn "%A"
    let qpl lst = lst |> List.map (qp)
    // maccth helper functions, TODO: delete 
    ////////////////////////////////////////////////////////////////////////////////

    /// Possible rotation values for the `Literal` type in the flexiable
    ///  second operand.
    [<Struct>]
    type RotVal =
        | Rot0     | Rot2     | Rot4     | Rot6
        | Rot8     | Rot10    | Rot12    | Rot14  
        | Rot16    | Rot18    | Rot20    | Rot22 
        | Rot24    | Rot26    | Rot28    | Rot30

    /// Map for converting from the `RVal` type to the underlying numerical
    ///  value used for the rotation in the `Literal` of the flexiable second
    ///  operand.
    let RotNums =
        Map.ofList [
            (Rot0, 0);   (Rot2, 2);     (Rot4, 4);   (Rot6, 6);
            (Rot8, 8);   (Rot10, 10);   (Rot12, 12); (Rot14, 14);  
            (Rot16, 16); (Rot18, 18);   (Rot20, 20); (Rot22, 22); 
            (Rot24, 24); (Rot26, 26);   (Rot28, 28); (Rot30, 30);
        ]
    
    /// Map for converting from a numerical value to the `RVal` used to
    ///  represent the rotation in the `Literal` of the flexiable second
    ///  operand.
    let RotVals =
        Map.ofList [
            (0, Rot0);   (2, Rot2);     (4, Rot4);   (6, Rot6);
            (8, Rot8);   (10, Rot10);   (12, Rot12); (14, Rot14);  
            (16, Rot16); (18, Rot18);   (20, Rot20); (22, Rot22); 
            (24, Rot24); (26, Rot26);   (28, Rot28); (30, Rot30);
        ]

    /// Literal type for allowed literals.
    ///  `K` is the underlying byte.
    ///  `R` is the rotation that is applied to `K`.
    type Literal =
        {
            b: byte;
            r: RotVal;
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
        | Shift     of FS2Form
        | RRX       of RName

    /// Instruction format for three-operand data processing instructions. 
    type DP3Form = {rDest:RName; rOp1:RName; fOp2:FlexOp2} 

    type Instr =
        | ADD of DP3Form
       
    /// parse error (dummy, but will do)
    type ErrInstr = string

    let DPSpec = {
        InstrC = DP
        Roots = ["ADD";"SUB"]
        Suffixes = [""; "S"]
    }



    let consDP3 rDest' rOp1' fOp2' =
        {rDest = regNames.[rDest']; rOp1 = regNames.[rOp1']; fOp2 = fOp2'}
    
    let consLit b' r' =
        {b = b'; r = RotVals.[r']}
        
                                                   

    /// map of all possible opcodes recognised
    let opCodes = opCodeExpand DPSpec

    /// main function to parse a line of assembler
    /// ls contains the line input
    /// and other state needed to generate output
    /// the result is None if the opcode does not match
    /// otherwise it is Ok Parse or Error (parse error string)
    let parse (ld: LineData) : Result<Parse<Instr>,string> option =
    
        let checkReg regStr = Map.containsKey regStr regNames
        let checkRegs regLst = regLst |> List.fold (fun s r -> s && (checkReg r)) true

        let checkLiteral (lit:uint32) =
            let rotMask n = (0xFFu >>> n) ||| (0xFFu <<< 32 - n)
            [0..2..30] 
            |> List.map (fun r -> rotMask r, r)
            |> List.filter (fun (mask, _r) -> (mask &&& lit) = lit)
            |> function
            | hd :: _tl ->
                let rotB = fst hd |> (&&&) lit
                let B = (rotB <<< snd hd) ||| (rotB >>> 32 - snd hd) |> byte
                Ok (B, snd hd)
            | [] -> Error "Not a valid literal. Rotation problems."

        let (|ParseRegex|_|) regex txt =
            let m = Regex.Match(txt, "^" + regex + "[\\s]*" + "$")
            match m.Success with
            | true -> Some (m.Groups.[1].Value)
            | false -> None

        let (|LitMatch|_|) txt =
            match txt with
            | ParseRegex "#&([0-9a-fA-F]+)" num -> 
                (uint32 ("0x" + num)) |> Some |> Option.map (checkLiteral)
            | ParseRegex "#(0B[0-1]+)" num
            | ParseRegex "#(0X[0-9a-fA-F]+)" num
            | ParseRegex "#([0-9]+)" num ->
                num |> uint32 |> Some |> Option.map (checkLiteral)
            | _ ->
                // "Not a valid literal. Literal expression problems." |> qp 
                None

        let (WA la) = ld.LoadAddr

        let parseADD suffix cond =

            let operands =
                ld.Operands.Split([|','|])
                |> Array.toList
                |> List.map (fun op -> op.ToUpper())
                |> function
                | [rDest'; rOp1'; op2'] when (checkRegs [rDest'; rOp1']) ->
                    match op2' with
                    | LitMatch (litVal) ->
                        Result.map (fun litVal -> { rDest = regNames.[rDest'];
                                                    rOp1 = regNames.[rOp1'];
                                                    fOp2 = Lit({b = (fst litVal); r = (RotVals.[litVal |> snd])})
                                                    }) litVal
                    | _ -> Error "Not a valid instruction. Or maybe just one that I haven't implemented yet. Who knows?"
                    // this will grow as the other forms of flexOp2 are implemented
                | _ -> Error "Syntax error. Instruction format is incorrect."
            
            // ("the operands are", operands)
            // |> qp

            // type DP3Form = {rDest:RName; rOp1:RName; op2:FlexOp2} 
            
            let makeAdd ops =
                Ok {
                    // Example values just to see if it type checks
                    // TODO: parse operands to get rOp1 and op2
                    // TODO: check op2 to see if it is compatible with FlexOp2
                    // TODO: create instruction in format to 'run'
                    PInstr  = ADD(ops);
                    PLabel  = ld.Label |> Option.map (fun lab -> lab, la);
                    PSize   = 4u;
                    PCond   = cond
                }


            Result.bind makeAdd operands
            

        let parseFuncs =
            Map.ofList [
                "ADD", parseADD;
            ]

        let parse' (_instrC, (root, suffix, cond)) =
            parseFuncs.[root] suffix cond

        // Optional value comes from here!
        // Error returned if opcode IS an opcode (.tryFind does not return None)
        //  but there is a problem elsewhere, once parse' has been called since
        //  this will only be called if .tryMap does not return a None.
        //  for example LITERAL VALUE IS NOT OKAY!!!!
        Map.tryFind ld.OpCode opCodes // lookup opcode to see if it is known
        |> Option.map parse' // if unknown keep none, if known parse it.


    /// Parse Active Pattern used by top-level code
    let (|IMatch|_|) = parse