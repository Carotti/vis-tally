//////////////////////////////////////////////////////////////////////////////////////////
//                   Sample (skeleton) instruction implementation modules
//////////////////////////////////////////////////////////////////////////////////////////

module Misc
    open CommonData
    open CommonLex
    open Expressions

    open Errors
    open DP

    // Don't support valueSize yet, always set to 1
    type FILLInstr = {numBytes : SymbolExp ; value : SymbolExp ; valueSize : int}

    type ADRInstr = {reg : RName ; exp : SymbolExp}

    type MiscInstr =
        | DCD of SymbolExp list
        | DCB of SymbolExp list
        | FILL of FILLInstr
        | EQU of SymbolExp
        | ADR of ADRInstr

    type Instr =
        | Misc of MiscInstr
        
    /// Errors which can occur during parsing
    // type ErrInstr =
    //     | InvalidExp of string
    //     | InvalidExpList of string
    //     | InvalidFill of string
    //     | LabelRequired
    /// Error types for parsing.


    /// Errors which can occur during resolving of an expression
    type ErrResolve =
        | InvalidByteExp of uint32
        | SymbolErrors of EvalErr list
        | InvalidFillMultiple // When numBytes is not a multiple of valueSize

    /// Resolve all MISC instructions which have unresolved `SymbolExp`s
    /// Any evaluation can fail with an undefined symbol, Error return is
    /// the first symbol which causes this
    /// Also return the resolved value
    let resolve (syms : SymbolTable) ins = 
        /// Take a list of results and transform it to a Result of either the
        /// first error in the list or the Ok list if every element is Ok
        let lstResUnfold lst =
            let folder acc el =
                let binder acc' = Result.map (fun x -> x :: acc') el
                Result.bind binder acc
            List.fold folder (Ok []) lst
            |> Result.map List.rev
        let validByte x =
            match x with
            | ExpResolved exp when exp < 256u -> exp |> byte |> ExpResolvedByte |> Ok
            | ExpResolved exp -> InvalidByteExp exp |> Error
            | _ -> failwithf "Calling validByte on unresolved SymbolExp"
        match ins with
        | DCD lst -> 
            List.map (evalSymExp syms) lst
            |> lstResUnfold
            |> Result.map DCD
            |> Result.mapError SymbolErrors 
        | DCB lst -> 
            List.map ((evalSymExp syms) >> (Result.mapError SymbolErrors) >> (Result.bind validByte)) lst 
            |> lstResUnfold
            |> Result.map DCB
        | FILL fins ->
            let valBind x = 
                (evalSymExp syms) fins.value
                |> Result.map (fun v -> {x with value = v})
            (evalSymExp syms) fins.numBytes
            |> Result.map (fun n -> {fins with numBytes = n})
            |> Result.bind valBind
            |> Result.map FILL
            |> Result.mapError SymbolErrors 
        | EQU exp -> 
            (evalSymExp syms) exp
            |> Result.map EQU
            |> Result.mapError SymbolErrors 
        | ADR {reg = r ; exp = e} ->
            (evalSymExp syms) e
            |> Result.map (fun x -> {reg = r ; exp = x})
            |> Result.map ADR
            |> Result.mapError SymbolErrors
    
    let parseExpr txt =
        match txt with
        | Expr (exp, "") -> exp |> ExpUnresolved |> Ok
        | _ -> 
            (txt, " is an invalid expression.")
            ||> makeError
            |> ``Invalid expression``
            |> Error

    let (|ADROp|_|) txt = 
        match txt with
        | RegexPrefix "ADR" (_, cond) ->
            let uCond = cond.ToUpper()
            match Map.containsKey uCond condMap with
            | true -> Some condMap.[uCond]
            | false -> None
        | _ -> None

    let commaSplit (x : string) =  x.Split([|','|]) |> Array.toList

    let rec parseExprList txt =
        match txt with
        | Expr (exp, rst) ->
            match rst with
            | RegexPrefix "," (_, rst') -> 
                Result.map (fun lst -> (ExpUnresolved exp) :: lst) (parseExprList rst')
            | "" -> Ok [ExpUnresolved exp]
            | _ -> 
                (txt, " is an invalid expression.")
                ||> makeError
                |> ``Invalid expression``
                |> Error
        | _ -> 
            (txt, " is an invalid expression list.")
            ||> makeError
            |> ``Invalid expression list``
            |> Error

    let parse (ls: LineData) : Result<Parse<Instr>,ErrParse> option =
        let (WA la) = ls.LoadAddr // address this instruction is loaded into memory

        let labelBinder f = 
            match ls.Label with
            | Some lab -> f lab
            | None -> 
                (ls.OpCode + " " + ls.Operands, " requires a label.")
                ||> makeError
                |> ``Label required``
                |> Error

        let parseDCD () =
            let parseDCD' lab =
                Result.bind (fun lst ->
                    Ok {
                        PInstr = DCD lst |> Misc;
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
                        PInstr = DCB lst |> Misc;
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
                        PInstr = EQU exp |> Misc;
                        PLabel = Some (lab, la);
                        PSize = 0u;
                        PCond = Cal;
                    }
                ) (parseExpr ls.Operands) 
            labelBinder parseEQU'

        let fillMap parseFunc = 
            Result.map (fun ins ->
                {
                    PInstr = ins;
                    PLabel = ls.Label |> Option.map (fun lab -> lab, la);
                    PSize = 0u;
                    PCond = Cal;
                }
            ) parseFunc

        let fillPack (num, fval, vsize) =
            FILL {
                numBytes = ExpUnresolved num;
                value = ExpUnresolved fval;
                valueSize = vsize;
            }

        let parseFILL () =
            let parseFILL' = 
                match commaSplit ls.Operands with
                | [Expr (num, "") ; Expr (v, "")] -> 
                    Ok (num, v, 1)
                | [Expr (num, "")] -> 
                    Ok (num, Literal 0u, 1)
                | [Expr (_, "") ; inv ; _ ]
                | [Expr (_, "") ; inv] -> 
                    (inv, " is an invalid fill.")
                    ||> makeError
                    |> ``Invalid fill``
                    |> Error
                | [inv ; _ ; _ ]
                | [inv ; _ ]
                | [inv] -> 
                    (inv, " is an invalid expression.")
                    ||> makeError
                    |> ``Invalid expression``
                    |> Error
                | _ -> 
                    (ls.Operands, " is an invalid fill.")
                    ||> makeError
                    |> ``Invalid fill``
                    |> Error
                |> Result.map fillPack |> Result.map Misc
            fillMap parseFILL'

        let parseSPACE () =
            let parseSPACE' =
                parseExpr ls.Operands
                |> Result.map (fun num -> 
                    FILL {
                        numBytes = num
                        value = ExpUnresolved (Literal 0u)
                        valueSize = 1
                    }) |> Result.map Misc
            fillMap parseSPACE'

        let parseADR cond = 
            let parseOperands =
                match commaSplit ls.Operands with
                | [reg ; s] ->
                    let uReg = reg.ToUpper()
                    match Map.tryFind uReg regNames with
                    | Some r -> 
                        parseExpr s |> Result.map (fun x ->
                            {reg = r ; exp = x}
                        )
                    | None -> (reg, "is an invalid register.")
                            ||> makeError
                            |> ``Invalid register``
                            |> Error
                    
                | _ -> (ls.Operands, "are invalid operands.")
                        ||> makeError
                        |> ``Invalid instruction``
                        |> Error
            parseOperands |> Result.map (ADR >> Misc) |> Result.map (fun x ->
                {
                    PInstr = x;
                    PLabel = ls.Label |> Option.map (fun lab -> lab, la);
                    PSize = 4u;
                    PCond = cond;
                }
            )

        match ls.OpCode.ToUpper() with
        | "DCD" -> parseDCD () |> Some
        | "DCB" -> parseDCB () |> Some
        | "EQU" -> parseEQU () |> Some
        | "FILL" -> parseFILL () |> Some
        | "SPACE" -> parseSPACE () |> Some
        | ADROp cond -> parseADR cond |> Some
        | _ -> None

    /// Parse Active Pattern used by top-level code
    let (|IMatch|_|) = parse