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
    type FILLVal = {value : Expression ; valueSize : FILLValSize}
    type FILLInstr = {numBytes : Expression ; fillWith : FILLVal Option}

    let uintToFillSize = Map.ofList [1u, One; 2u, Two; 4u, Four]

    /// instruction (dummy: must change)
    type Instr =
    | DCD of Expression list
    | DCB of Expression list
    | FILL of FILLInstr
    | EQU of Expression

    /// parse error (dummy, but will do)
    type ErrInstr =
    | ExprErr of string
    | MiscErr of string

    /// These opCodes do not have conditions or suffixes
    let opCodes = ["DCD";"DCB";"EQU";"FILL"]

    let parseExpr txt =
        match txt with
        | Expr (exp, "") -> Ok exp
        | _ -> 
            sprintf "Invalid expression '%s'" txt
            |> MiscErr
            |> Error

    let rec parseExprList txt =
        match txt with
        | Expr (exp, rst) ->
            match rst with
            | RegexPrefix "," (_, rst') -> 
                Result.map (fun lst -> exp :: lst) (parseExprList rst')
            | "" -> Ok [exp]
            | x -> 
                sprintf "Unknown expression at '%s'" x 
                |> MiscErr 
                |> Error
        | x -> 
            sprintf "Bad expression list at '%s'" x 
            |> MiscErr 
            |> Error

    let parse (ls: LineData) : Result<Parse<Instr>,ErrInstr> option =
        let (WA la) = ls.LoadAddr // address this instruction is loaded into memory

        let labelBinder f = 
            match ls.Label with
            | Some lab -> f lab
            | None ->
                sprintf "Expected a label"
                |> MiscErr |> Error

        let parseData which =
            let parseData' lab =
                Result.bind (fun lst ->
                    Ok {
                        PInstr = which lst;
                        PLabel = Some (lab, la);
                        PSize = 
                            (match which lst with
                            | DCD _ -> 4
                            | DCB _ -> 1
                            | _ -> failwithf "Called parseData on not DCD/DCB")
                             * (List.length lst) |> uint32;
                        PCond = Cal;
                    }
                ) (parseExprList ls.Operands)
            labelBinder parseData'

        let parseEQU () =
            let parseEQU' lab =
                Result.bind (fun exp ->
                    Ok {
                        PInstr = EQU exp;
                        PLabel = Some (lab, la);
                        PSize = 0u;
                        PCond = Cal;
                    }
                ) (parseExpr ls.Operands)
            labelBinder parseEQU'

        let parseFILL () =
            match ls.Operands with
            | Expr (num, rst) -> 
                match rst with
                | "" ->
                    FILL {numBytes = num; fillWith = None}
                | RegexPrefix "," (_, Expr (v, rst')) ->
                    match rst' with
                    | "" ->
                        FILL {numBytes = num; fillWith = Some {value = v; valueSize = One}}
                    | RegexPrefix "," (_, RegexPrefix "[124]" (vs, "")) ->
                        let size = uintToFillSize.[removeWs vs |> uint32]
                        FILL {numBytes = num; fillWith = Some {value = v; valueSize = size}}

        match ls.OpCode with
        | "DCD" -> parseData DCD |> Some
        | "DCB" -> parseData DCB |> Some
        | "EQU" -> parseEQU () |> Some
        | "FILL" -> None
        | _ -> None

    /// Parse Active Pattern used by top-level code
    let (|IMatch|_|) = parse
