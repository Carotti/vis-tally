//////////////////////////////////////////////////////////////////////////////////////////
//                   Sample (skeleton) instruction implementation modules
//////////////////////////////////////////////////////////////////////////////////////////

module Misc
    open CommonData
    open CommonLex
    open Expressions
    open Expecto

    // Both DCD and DCB don't have their expressions 
    // evaluated until simulation

    type FILLVal = {value : Expression ; valueSize : int}
    type FILLInstr = {numBytes : Expression ; fillWith : FILLVal Option}

    /// instruction (dummy: must change)
    type Instr =
    | DCD of Expression list
    | DCB of Expression list
    | FILL of FILLInstr
    | EQU of Expression

    /// parse error (dummy, but will do)
    type ErrInstr = string

    /// These opCodes do not have conditions or suffixes
    let opCodes = ["DCD";"DCB";"EQU";"FILL"]

    let parseExpr txt =
        match txt with
        | Expr (exp, "") -> Ok exp
        | _ -> sprintf "Invalid expression '%s'" txt |> Error

    let rec parseExprList txt =
        match txt with
        | Expr (exp, rst) ->
            match rst with
            | RegexPrefix "," (_, rst') -> 
                Result.map (fun lst -> exp :: lst) (parseExprList rst')
            | "" -> Ok [exp]
            | _ -> sprintf "Invalid Expression '%s'" txt |> Error
        | _ -> sprintf "Bad expression list '%s'" txt |> Error

    let parse (ls: LineData) : Result<Parse<Instr>,ErrInstr> option =
        let (WA la) = ls.LoadAddr // address this instruction is loaded into memory

        let labelBinder f = 
            match ls.Label with
            | Some lab -> f lab
            | None -> sprintf "Expected a label for %s instruction" ls.OpCode |> Error

        let parseDCD () =
            let parseDCD' lab =
                Result.bind (fun lst ->
                    Ok {
                        PInstr = DCD lst;
                        PLabel = Some (lab, la);
                        PSize = 4 * (List.length lst) |> uint32;
                        PCond = Cal;
                    }
                ) (parseExprList ls.Operands)
            labelBinder parseDCD'

        let parseDCB () =
            let parseDCB' lab =
                Result.bind (fun lst ->
                    Ok {
                        PInstr = DCB lst;
                        PLabel = Some (lab, la);
                        PSize = (List.length lst) |> uint32;
                        PCond = Cal;
                    }
                ) (parseExprList ls.Operands)
            labelBinder parseDCB'

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
            let parseFILL' = 
                match ls.Operands with
                | Expr (num, rst) -> 
                    match rst with
                    | "" ->
                        FILL {numBytes = num; fillWith = None} |> Ok
                    | RegexPrefix "," (_, Expr (v, rst')) ->
                        match rst' with
                        | "" ->
                            FILL {numBytes = num; fillWith = Some {value = v; valueSize = 1}} |> Ok
                        | RegexPrefix "," (_, RegexPrefix "[124]" (vs, "")) ->
                            FILL {numBytes = num; fillWith = Some {value = v; valueSize = int vs}} |> Ok
                        | _ -> sprintf "Invalid fill value size '%s'" rst' |> Error
                    | _ -> sprintf "Invalid fill value expression '%s'" rst |> Error
                | _ -> sprintf "Invalid fill expression '%s'" ls.Operands |> Error
            Result.map (fun ins ->
                {
                    PInstr = ins;
                    PLabel = ls.Label |> Option.map (fun lab -> lab, la);
                    PSize = 4u;
                    PCond = Cal;
                }
            ) parseFILL'

        match ls.OpCode with
        | "DCD" -> parseDCD () |> Some
        | "DCB" -> parseDCB () |> Some
        | "EQU" -> parseEQU () |> Some
        | "FILL" -> parseFILL () |> Some
        | _ -> None

    /// Parse Active Pattern used by top-level code
    let (|IMatch|_|) = parse
