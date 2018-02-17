//////////////////////////////////////////////////////////////////////////////////////////
//                   Sample (skeleton) instruction implementation modules
//////////////////////////////////////////////////////////////////////////////////////////

module DP
    open CommonData
    open CommonLex
    open System.Text.RegularExpressions
    open FsCheck

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
        | ConstShift    of Literal
        | RegShift      of RName
   
    /// Shift instructions that require operands and are compatible with the
    ///  flexiable second operand. `RRX` is not included since it does not
    ///  require any further operands.
    type SInstr =
        | LSL
        | LSR
        | ASR
        | ROR
    
    let sInstrs =
        Map.ofList [
            ("LSL", LSL);
            ("LSR", LSR);
            ("ASR", ASR);
            ("ROR", ROR);     
        ]
     
    // Flexible shift sub-instruction format within the flexiable second operand.
    type FS2Form = {rOp2:RName; sInstr:SInstr; sOp:SOp}

    type FlexOp2 =
        | Lit       of Literal
        | Reg       of RName
        | Shift     of FS2Form
        | RRX       of RName

    /// Instruction format for three-operand data processing instructions. 
    type DP3Form = {rDest:RName; rOp1:RName; fOp2:FlexOp2} 

    /// Instruction format for the first two operands of the hree-operand data
    ///  processing instructions. 
    type DP2Form = {rDest:RName; rOp1: RName}
 
    type Instr =
        | ADD of DP3Form
       
    /// parse error (dummy, but will do)
    type ErrInstr = string

    let DPSpec = {
        InstrC = DP
        Roots = ["ADD";"SUB"]
        Suffixes = [""; "S"]
    }

    /// Constructs a register name of type `RName` from a register specified as a `string`.
    let consReg reg =
        regNames.[reg]

    /// Constructs an operand record of type `DP3Form` from registers specified as strings.
    let consDP3 rDest' rOp1' fOp2' =
        {rDest = regNames.[rDest']; rOp1 = regNames.[rOp1']; fOp2 = fOp2'}

    /// Constructs an operand record of type `DP2Form` from registers specified as strings.
    let consDP2 rDest' rOp1' =
        {rDest = regNames.[rDest']; rOp1 = regNames.[rOp1']}

    /// Joins a `DP2Form` and a `FlexOp2` to create a `DP3Form`.
    let joinDP dp2 fOp2' =
        {rDest = dp2.rDest; rOp1 = dp2.rOp1; fOp2 = fOp2'}

    /// Creates a `DP2Form` from `rDest'` and `rOp1'`. Joins the `DP2Form` to a
    ///  `FlexOp2` to create a `DP3Form`.
    let partialDP rDest' rOp1' fOp2' =
        let dp2 = consDP2 rDest' rOp1'
        joinDP dp2 fOp2'
    
     /// Constructs a literal record of type `Literal` from a rotation values
     ///  specified as an `int`.
    let consLit (b', r') =
        {b = b'; r = RotVals.[r']}

     /// Constructs a literal record of type `FlexOp2` from a rotation values
     ///  specified as an `int`.
    let consLitOp (b', r') =
        Lit (consLit (b', r'))
    
    /// Constructs a shift sub-instruction for the flexible second operand of
    ///  type `SInstr` from a shift instruction specified as a `string`
    let consSInstr instr =
        sInstrs.[instr]

    let consFS2 rOp2' sInstr' sOp'=
        {
            rOp2 = consReg(rOp2');
            sInstr = sInstr';
            sOp = sOp';
        }
    

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

        let checkLiteral lit =
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
            let m = Regex.Match(txt, "^[\\s]*" + regex + "[\\s]*" + "$")
            match m.Success with
            | true -> Some (m.Groups.[1].Value)
            | false -> None

        let (|LitMatch|_|) txt =
            match txt with
            | ParseRegex "#&([0-9a-fA-F]+)" num -> 
                (uint32 ("0x" + num)) |> checkLiteral |> Some
            | ParseRegex "#(0B[0-1]+)" num
            | ParseRegex "#(0X[0-9a-fA-F]+)" num
            | ParseRegex "#([0-9]+)" num ->
                num |> uint32 |> checkLiteral |> Some
            | _ ->
                // "Not a valid literal. Literal expression problems." |> qp
                None
        
        let (|RegMatch|_|) txt =
            match txt with
            | ParseRegex "([R][\d][0-5]?)" reg ->
                reg |> consReg |> Ok |> Some
            | _ ->
                "Not a valid register. Register problems." |> Error |> Some
               
        let (|RrxMatch|_|) reg txt =
            match txt with
            | ParseRegex "(RRX)" _ ->
                reg |> consReg |> Ok |> Some
            | _ ->
                None

        let (|ShiftInstr|_|) txt =
            match txt with
            | ParseRegex "(LSL)" _ 
            | ParseRegex "(LSR)" _ 
            | ParseRegex "(ASR)" _ 
            | ParseRegex "(ROR)" _ -> txt |> consSInstr |> Some
            | _ -> None

        let (|ShiftMatch|_|) (rOp2:string) (txt:string) =
            let instr = txt.[0..2]
            let oprnds = txt.[3..]
            match instr with
            | ShiftInstr sInstr ->
                let partialFS2 = consFS2 rOp2 sInstr
                match oprnds with
                | LitMatch litVal ->
                    litVal
                    |> Result.map(consLit)
                    |> Result.map(ConstShift)
                    |> Result.map(partialFS2)
                    |> Some
                | RegMatch rOp3 ->
                    rOp3
                    |> Result.map(RegShift)
                    |> Result.map(partialFS2)
                    |> Some
                | _ ->
                    "Not a valid flexible second operand shift instruction." |> Error |> Some
            | _ ->
                None  

        let operands =
            ld.Operands.Split([|','|])
            |> Array.toList
            |> List.map (fun op -> op.ToUpper())
            |> function
            | [rDest'; rOp1'; op2'] when (checkRegs [rDest'; rOp1']) ->
                let dp2 = consDP3 rDest' rOp1'
                match op2' with
                | LitMatch litVal ->
                    litVal |> Result.map (consLitOp >> dp2)
                | RegMatch reg ->
                    reg |> Result.map (Reg >> dp2)
                | _ -> Error "Not a valid instruction. Or maybe just one that I haven't implemented yet. Who knows?"
            | [rDest'; rOp1'; rOp2'; extn] when (checkRegs [rDest'; rOp1'; rOp2']) ->
                let dp2 = consDP3 rDest' rOp1'
                match extn with
                | RrxMatch rOp2' reg ->
                    reg |> Result.map (RRX >> dp2)
                | ShiftMatch rOp2' shift ->
                    shift |> Result.map(Shift >> dp2)
                | _ ->
                    Error "Not a valid instruction. Or maybe just one that I haven't implemented yet. Who knows?"
            | _ -> Error "Syntax error. Instruction format is incorrect."

        let (WA la) = ld.LoadAddr

        let parseADD suffix cond =
            let makeAdd ops =
                Ok {
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