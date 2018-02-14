//////////////////////////////////////////////////////////////////////////////////////////
//                   Sample (skeleton) instruction implementation modules
//////////////////////////////////////////////////////////////////////////////////////////

module Misc
    open CommonData
    open CommonLex
    open Expressions

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

        let validEncoding x =
            let rec validEncoding' r =
                let rotRight (x : uint32) n = (x >>> n) ||| (x <<< (32 - n))
                match rotRight x r < 256u, r > 30 with
                | _, true -> false
                | true, false -> true
                | false, false -> validEncoding' (r + 2)
            validEncoding' 0

        /// Returns an list of the evaluated expressions in txt
        let rec parseExprList txt : Result<uint32 list, string> =
            let exprBinder (exp, rst) =
                match validEncoding exp with
                | true ->
                    match rst with
                    | RegexPrefix "," (_, rst') -> 
                        Result.map (fun lst -> exp :: lst) (parseExprList rst')
                    | "" -> Ok [exp]
                    | x -> sprintf "Unknown expression at '%s'" x |> Error
                | false -> sprintf "Invalid encoded expression evaluates to '%d'" exp |> Error
            match txt with
            | Expr ls.SymTab x -> Result.bind exprBinder x
            | _ -> "Bad expression list" |> Error

        let parseDCD _suffix pCond : Result<Parse<Instr>,string> = 
            match ls.Label with
            | Some x -> 
                match parseExprList ls.Operands with
                | Ok vals ->
                    Ok { 
                        PInstr = DCD {Label = x ; values = vals}; 
                        PLabel = None ; 
                        PSize = 0u; 
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
