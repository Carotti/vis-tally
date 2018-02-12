//////////////////////////////////////////////////////////////////////////////////////////
//                   Sample (skeleton) instruction implementation modules
//////////////////////////////////////////////////////////////////////////////////////////

module Misc
    open CommonData
    open CommonLex
    open System.Text.RegularExpressions

    // Types for data declaration instructions
    type InstrDCD = {Label : string ; values : uint32 list}
    type InstrDCB = {Label : string ; values : byte list}
    type InstrADR = {Label : string ; reg : RName}

    // instruction type
    type Instr =
    | DCD of InstrDCD
    | DCB of InstrDCB
    // EQU only affects the symbol table during assembly
    // not memory during runtime
    | EQU 

    /// parse error (dummy, but will do)
    type ErrInstr = string

    /// sample specification for set of instructions
    /// very incomplete!
    let MiscSpec = {
        InstrC = MISC
        Roots = ["DCD"]
        Suffixes = [""]
    }

    /// map of all possible opcodes recognised
    let opCodes = opCodeExpand MiscSpec
    /// main function to parse a line of assembler
    /// ls contains the line input
    /// and other state needed to generate output
    /// the result is None if the opcode does not match
    /// otherwise it is Ok Parse or Error (parse error string)
    let parse (ls: LineData) : Result<Parse<Instr>,string> option =
        let (WA la) = ls.LoadAddr

        /// Match the start of txt with pat
        /// Return a tuple of the matched text and the rest
        let (|RegexPrefix|_|) pat txt =
            // Match from start, also don't care about whitespace
            let m = Regex.Match (txt, "^[\\s]*" + pat + "[\\s]*")
            match m.Success with
            | true -> (m.Value, txt.Substring(m.Value.Length)) |> Some
            | false -> None

        let rec (|Expr|_|) expTxt =
            /// Match, parse and evaluate symbols
            let (|LabelExpr|_|) txt =
                match txt with 
                | RegexPrefix "[a-zA-Z][a-zA-Z0-9]*" (var, rst) ->
                    match ls.SymTab with
                    | Some symTab -> 
                        match (Map.containsKey var symTab) with
                        | true -> (symTab.[var], rst) |> Ok |> Some
                        | false -> sprintf "Symbol '%s' not declared" var |> Error |> Some
                    | None -> "No Symbol table exists" |> Error |> Some
                | _ -> None

            /// Match, parse, evaluate and validate literals
            let (|LiteralExpr|_|) txt = 
                let validLiteral (x, rst) =
                    let rec validLiteral' r =
                        let rotRight (y : uint32) n = (y >>> n) ||| (y <<< (32 - n))
                        match (rotRight x r < 256u, r > 30) with
                        | _, true -> sprintf "Invalid Literal '%d'" x |> Error
                        | true, _ -> Ok (x, rst)
                        | false, false -> validLiteral' (r + 2)
                    validLiteral' 0
                let ret = Ok >> (Result.bind validLiteral) >> Some
                match txt with
                | RegexPrefix "[0-9]+" (num, rst) 
                | RegexPrefix "0x[0-9a-fA-F]+" (num, rst) 
                | RegexPrefix "0b[0-1]+" (num, rst) -> (uint32 num, rst) |> ret
                | RegexPrefix "&[0-9a-fA-F]+" (num, rst) -> (uint32 ("0x" + num.[1..]), rst) |> ret
                | _ -> None

            /// Active pattern matching either labels, literals
            /// or a bracketed expression (recursively defined)
            let (|PrimExpr|_|) txt =
                match txt with  
                | LabelExpr x -> Some x
                | LiteralExpr x -> Some x
                | RegexPrefix "\(" (_, rst) ->
                    match rst with
                    | Expr (Ok (exp, rst' : string)) ->
                        match rst'.StartsWith ")" with
                        | true -> Ok (exp, rst'.[1..]) |> Some
                        | false -> sprintf "Unmatched bracket at '%s'" rst' |> Error |> Some
                    | _ -> Error "Unknown bracketed expression" |> Some
                | _ -> None

            /// Higher order active pattern for defining binary operators
            /// NextExpr is the active pattern of the operator with the next
            /// highest precedence. reg is the regex which matches this operator
            /// op is the operation it performs
            let rec (|BinExpr|_|) (|NextExpr|_|) reg op txt =
                match txt with
                | NextExpr (Ok (lVal, rhs)) ->
                    match rhs with
                    | RegexPrefix reg (_, rst) ->
                        match rst with
                        | BinExpr (|NextExpr|_|) reg op x -> Result.map (fun (rVal, rst') -> op lVal rVal, rst') x |> Some
                        | _ -> None
                    | _ -> Ok (lVal, rhs) |> Some
                | _ -> None

            // Define active patterns for the binary operators
            // Order of precedence: Add, Sub, Mul
            let (|MulExpr|_|) = (|BinExpr|_|) (|PrimExpr|_|) "\*" (*)
            let (|SubExpr|_|) = (|BinExpr|_|) (|MulExpr|_|) "\-" (-)
            let (|AddExpr|_|) = (|BinExpr|_|) (|SubExpr|_|) "\+" (+)

            match expTxt with
            | AddExpr x -> Some x
            | _ -> None

        /// Returns an list of the evaluated expressions in txt
        let rec parseExprList txt : Result<uint32 list, string> =
            let exprBinder (exp, rst) =
                match rst with
                | RegexPrefix "," (_, rst') -> Result.map (fun lst -> exp :: lst) (parseExprList rst')
                | "" -> Ok [exp]
                | x -> sprintf "Unknown expression at '%s'" x |> Error
            match txt with
            | Expr x -> Result.bind exprBinder x
            | _ -> "Bad expression list" |> Error

        let parseDCD _suffix pCond : Result<Parse<Instr>,string> = 
            match ls.Label with
            | Some x -> 
                match parseExprList ls.Operands with
                | Ok vals ->
                    Ok { 
                        PInstr = DCD {Label = x ; values = vals}; 
                        PLabel = ls.Label |> Option.map (fun lab -> lab, la) ; 
                        PSize = 4u; 
                        PCond = pCond 
                    }
                | Error x -> Error x
            | None -> Error "Missing DCD directive label"

        // Map roots to the functions which parse them
        let parseFuncs = 
            Map.ofList [
                "DCD", parseDCD;
            ] 
        
        let parse' (_instrC, (root,suffix,pCond)) =
            parseFuncs.[root] suffix pCond

        Map.tryFind ls.OpCode opCodes // lookup opcode to see if it is known
        |> Option.map parse' // if unknown keep none, if known parse it.


    /// Parse Active Pattern used by top-level code
    let (|IMatch|_|) = parse
