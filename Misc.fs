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

        /// Check if x can be rotated right in a 32 bit word
        let rotateOk x =
            let rec validEncoding' r =
                let rotRight (x : uint32) n = (x >>> n) ||| (x <<< (32 - n))
                match rotRight x r < 256u, r > 30 with
                | _, true -> sprintf "Cannot rotate '%d' within a 32 bit word" x |> Error
                | true, false -> Ok ()
                | false, false -> validEncoding' (r + 2)
            validEncoding' 0

        /// Check if x is small enough to be a byte
        let byteOk x =
            match (x |> byte |> uint32) = x with
            | true -> Ok ()
            | false -> sprintf "Cannot fit '%d' into 1 byte of memory" x |> Error

        /// Returns an list of the evaluated expressions in txt
        /// Validator is a function that will verify whether the 
        /// result of each expression matches some condition
        let rec parseExprList txt validator : Result<uint32 list, string> =
            let exprBinder (exp, rst) =
                match validator exp with
                | Ok () ->
                    match rst with
                    | RegexPrefix "," (_, rst') -> 
                        Result.map (fun lst -> exp :: lst) (parseExprList rst' validator)
                    | "" -> Ok [exp]
                    | x -> sprintf "Unknown expression at '%s'" x |> Error
                | Error err -> Error err
            match txt with
            | Expr ls.SymTab x -> Result.bind exprBinder x
            | _ -> "Bad expression list" |> Error

        /// For constructing DCD/DCB/EQU instructions
        let makeDC ins =
            Ok {
                PInstr = ins; 
                PLabel = None; 
                PSize = 0u; 
                PCond = Cal;
            }

        let parseDCD _suffix _pCond : Result<Parse<Instr>,string> = 
            match ls.Label with
            | Some x -> 
                match parseExprList ls.Operands rotateOk with
                | Ok vals -> DCD {Label = x ; values = vals} |> makeDC; 
                | Error x -> Error x
            | None -> Error "Missing DCD directive label"

        let parseDCB _suffix _pCond : Result<Parse<Instr>,string> = 
            match ls.Label with
            | Some x -> 
                match parseExprList ls.Operands byteOk with
                | Ok vals -> 
                    let bytes = List.map byte vals
                    DCB {Label = x; values = bytes} |> makeDC
                | Error x -> Error x
            | None -> Error "Missing DCB directive label"

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
