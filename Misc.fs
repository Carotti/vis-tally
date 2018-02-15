//////////////////////////////////////////////////////////////////////////////////////////
//                   Sample (skeleton) instruction implementation modules
//////////////////////////////////////////////////////////////////////////////////////////

module Misc
    open CommonData
    open CommonLex
    open Expressions

    // Both DCD and DCB don't have their expressions 
    // evaluated until simulation

    type FILLValSize = One | Two | Four
    type FILLVal = {value : uint32 ; valueSize : FILLValSize Option}

    /// instruction (dummy: must change)
    type Instr =
    | DCD of Expression list
    | DCB of Expression list
    | FILL of FILLVal option
    | EQU // No information about EQU is needed

    /// parse error (dummy, but will do)
    type ErrInstr =
    | ExprErr of string
    | MiscErr of string

    /// These opCodes do not have conditions or suffixes
    let opCodes = ["DCD";"DCB";"EQU";"FILL"]

    let parse (ls: LineData) : Result<Parse<Instr>,ErrInstr> option =
        let (WA la) = ls.LoadAddr // address this instruction is loaded into memory

        let rec parseExprList txt =
            let exprBinder (exp, rst) =
                match rst with
                | RegexPrefix "," (_, rst') -> Result.map (fun lst -> exp :: lst) (parseExprList rst')
                | "" -> Ok [exp]
                | x -> sprintf "Unknown expression at '%s'" x |> Error
            match txt with
            | Expr x -> Result.bind exprBinder x
            | x -> sprintf "Bad expression list at '%s'" x |> Error

        match ls.OpCode with
        | "DCD" -> None
        | "DCB" -> None
        | "EQU" -> None
        | "FILL" -> None
        | _ -> None

    /// Parse Active Pattern used by top-level code
    let (|IMatch|_|) = parse
