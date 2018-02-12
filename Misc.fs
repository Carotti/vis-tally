//////////////////////////////////////////////////////////////////////////////////////////
//                   Sample (skeleton) instruction implementation modules
//////////////////////////////////////////////////////////////////////////////////////////

module Misc
    open CommonData
    open CommonLex
    open System.Text.RegularExpressions
    open Expecto.Flip
    open System.Text.RegularExpressions
    open System

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

        let (|PrimExpr|_|) txt =
            match txt with  
            | RegexPrefix "[0-9]+" (num, rst) -> (uint32 num, rst) |> Ok |> Some
            | RegexPrefix "[a-zA-Z][a-zA-Z0-9]*" (var, rst) ->
                match ls.SymTab with
                | Some symTab ->
                    match (Map.containsKey var symTab) with
                    | true -> (symTab.[var], rst) |> Ok |> Some
                    | false -> "Symbol not declared" |> Error |> Some
                | None -> "No Symbol table exists" |> Error |> Some
            | _ -> None

        let (|Expr|_|) txt =
            match txt with 
            | PrimExpr (Ok (num, rst)) -> (num, rst) |> Ok |> Some
            | PrimExpr (Error x) -> x |> Error |> Some
            | _ -> None

        // Returns a list of expressions comma seperated in txt
        let rec parseExprList txt : uint32 list =
            match txt with
            | Expr (exp, rst) ->
                match rst with
                | RegexPrefix "," (_, rst') -> exp :: (parseExprList rst')
                | "" -> [exp]
                | _ -> []
            | _ -> []

        let parseDCD _suffix pCond : Result<Parse<Instr>,string> = 
            match ls.Label with
            | Some x -> 
                Ok { 
                    PInstr = DCD {Label = x ; values = parseExprList ls.Operands}; 
                    PLabel = ls.Label |> Option.map (fun lab -> lab, la) ; 
                    PSize = 4u; 
                    PCond = pCond 
                }
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
