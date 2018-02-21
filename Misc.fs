//////////////////////////////////////////////////////////////////////////////////////////
//                   Sample (skeleton) instruction implementation modules
//////////////////////////////////////////////////////////////////////////////////////////

module Misc
    open CommonData
    open CommonLex
    open Expressions
    open Execution

    type SymbolExp =
        | ExpUnresolved of Expression
        | ExpResolved of uint32
        | ExpResolvedByte of byte // For DCB

    type FILLVal = {value : SymbolExp ; valueSize : int}
    type FILLInstr = {numBytes : SymbolExp ; fillWith : FILLVal Option}

    type Instr =
        | DCD of SymbolExp list
        | DCB of SymbolExp list
        | FILL of FILLInstr
        | EQU of SymbolExp

    /// Errors which can occur during parsing
    type ErrInstr =
        | InvalidExp of string
        | InvalidExpList of string
        | InvalidFillSize of string
        | InvalidFillValue of string
        | InvalidFillNum of string
        | InvalidFillExp of string
        | EmptyFillExp
        | LabelRequired

    /// Errors which can occur during resolving of an expression
    type ErrResolve =
        | InvalidByteExp of uint32
        | SymbolErrors of EvalErr list

    /// Resolve all MISC instructions which have unresolved `SymbolExp`s
    /// Any evaluation can fail with an undefined symbol, Error return is
    /// the first symbol which causes this
    let resolve (syms : SymbolTable) ins = 
        let evalSymExp exp =
            match exp with
            | ExpUnresolved x -> 
                Result.map ExpResolved (eval syms x)
                |> Result.mapError SymbolErrors
            | _ -> exp |> Ok
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
            List.map evalSymExp lst
            |> lstResUnfold
            |> Result.map DCD
        | DCB lst -> 
            List.map (evalSymExp >> (Result.bind validByte)) lst 
            |> lstResUnfold
            |> Result.map DCB
        | FILL fins ->
            let fillMap f =
                match f.fillWith with
                | Some fv -> 
                    let fillValMap x = FILL {f with fillWith = Some {fv with value = x}}
                    Result.map fillValMap (evalSymExp fv.value)
                | None -> FILL f |> Ok
            evalSymExp fins.numBytes
            |> Result.map (fun x -> {fins with numBytes = x}) 
            |>  Result.bind fillMap
        | EQU exp -> 
            evalSymExp exp
            |> Result.map EQU

    /// Execute a MISC instruction against the datapath
    /// mem is where to start placing in memory
    let execute ins dp mem =
        let executeDCD lst =
            let foldDCD (dp', mem') exp =
                match exp with
                | ExpResolved data -> (updateMemData data (alignAddress mem') dp', mem' + 4u)
                | _ -> failwithf "Trying to execute unresolved DCD instruction"
            List.fold foldDCD (dp, mem) lst |> fst

        // let executeDCB lst = 
        //     let foldDCB (dp' mem') exp =
        //         match exp with
        //         | ExpResolvedByte data -> 
        //         | _ -> failwithf "Trying to execute unresolved byte DCB instructon"

        match ins with
        | DCD lst -> executeDCD lst
        //| DCB lst -> executeDCB lst
            

    let parseExpr txt =
        match txt with
        | Expr (exp, "") -> exp |> ExpUnresolved |> Ok
        | _ -> InvalidExp txt |> Error

    let rec parseExprList txt =
        match txt with
        | Expr (exp, rst) ->
            match rst with
            | RegexPrefix "," (_, rst') -> 
                Result.map (fun lst -> (ExpUnresolved exp) :: lst) (parseExprList rst')
            | "" -> Ok [ExpUnresolved exp]
            | _ -> InvalidExp txt |> Error
        | _ -> InvalidExpList txt |> Error

    let parse (ls: LineData) : Result<Parse<Instr>,ErrInstr> option =
        let (WA la) = ls.LoadAddr // address this instruction is loaded into memory

        let labelBinder f = 
            match ls.Label with
            | Some lab -> f lab
            | None -> LabelRequired |> Error

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
                let fillMap (num, fillval) =
                    let fillValBind (v, vs) =
                        Some {value = ExpUnresolved v; valueSize = vs}
                    FILL {
                        numBytes = ExpUnresolved num;
                        fillWith = Option.bind fillValBind fillval;
                    }
                match ls.Operands.Split([|','|]) |> Array.toList with
                | [Expr (num, "") ; Expr (v, "") ; RegexPrefix "[124]" (vs, "")] -> 
                    Ok (num, Some (v, int vs))
                | [Expr (num, "") ; Expr (v, "")] -> 
                    Ok (num, Some (v, 1))
                | [Expr (num, "")] -> 
                    Ok (num, None)
                | [Expr (_, "") ; Expr (_, "") ; inv] -> 
                    InvalidFillSize inv |> Error
                | [Expr (_, "") ; inv ; _ ]
                | [Expr (_, "") ; inv] -> 
                    InvalidFillValue inv |> Error
                | [inv ; _ ; _ ]
                | [inv ; _ ]
                | [inv] -> 
                    InvalidFillNum inv |> Error
                | _ -> InvalidFillExp ls.Operands |> Error
                |> Result.map fillMap
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