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

        /// Match, parse and evaluate literals and symbols
        let (|PrimExpr|_|) txt =
            match txt with  
            | RegexPrefix "[0-9]+" (num, rst) -> (uint32 num, rst) |> Ok |> Some
            | RegexPrefix "[a-zA-Z][a-zA-Z0-9]*" (var, rst) ->
                match ls.SymTab with
                | Some symTab -> 
                    match (Map.containsKey var symTab) with
                    | true -> (symTab.[var], rst) |> Ok |> Some
                    | false -> sprintf "Symbol '%s' not declared" var |> Error |> Some
                | None -> "No Symbol table exists" |> Error |> Some
            | _ -> None

        /// Returns an list of the evaluated expressions in txt
        let rec parseExprList txt : Result<uint32 list, string> =
            let exprBinder (exp, rst) =
                match rst with
                | RegexPrefix "," (_, rst') -> Result.map (fun lst -> exp :: lst) (parseExprList rst')
                | "" -> Ok [exp]
                | x -> sprintf "Unknown expression at '%s'" x |> Error
            match txt with
            | PrimExpr x -> Result.bind exprBinder x
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
