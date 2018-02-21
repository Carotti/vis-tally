module Expressions
    open System.Text.RegularExpressions

    /// Match the start of txt with pat
    /// Return a tuple of the matched text and the rest
    let (|RegexPrefix|_|) pat txt =
        // Match from start, ignore whitespace
        let m = Regex.Match (txt, "^[\\s]*" + pat + "[\\s]*")
        match m.Success with
        | true -> (m.Value, txt.Substring(m.Value.Length)) |> Some
        | false -> None

    /// Remove all whitespace from a matched string
    let removeWs txt = Regex.Replace (txt, "[\\s]*", "")

    /// Active pattern for matching labels
    /// Also removes any whitespace from around the label
    let (|LabelExpr|_|) txt =
        match txt with 
        | RegexPrefix "[a-zA-Z][a-zA-Z0-9]+" (var, rst) -> 
            // Remove whitespace from the label
            (removeWs var, rst) |> Some
        | _ -> None

    type Expression =
        | BinOp of (uint32->uint32->uint32) * Expression * Expression
        | Label of string
        | Literal of uint32

    type EvalErr =
        | SymbolUndeclared of string

    /// Evaluate exp against the symbol table syms
    /// Returns a list of all errors or the result
    let rec eval syms exp =
        let doBinary op x y = 
            match (eval syms x), (eval syms y) with
            | Ok resX, Ok resY -> op resX resY |> Ok
            | Error a, Error b -> List.concat [a ; b] |> Error
            | Error a, _ -> Error a
            | _, Error b -> Error b
        match exp with
        | BinOp (op, x, y) -> doBinary op x y
        | Literal x -> x |> Ok
        | Label x ->
            match (Map.containsKey x syms) with
                | true -> syms.[x] |> Ok
                | false -> [SymbolUndeclared x] |> Error

    /// Active pattern for matching expressions
    /// Returns an Expression AST
    let rec (|Expr|_|) expTxt =
        let (|LiteralExpr|_|) txt = 
            match txt with
            | RegexPrefix "0[xX][0-9a-fA-F]+" (num, rst) 
            | RegexPrefix "0[bB][0-1]+" (num, rst)
            | RegexPrefix "[0-9]+" (num, rst) -> 
                (uint32 num |> Literal, rst) |> Some
            | RegexPrefix "&[0-9a-fA-F]+" (num, rst) -> 
                ("0x" + (removeWs num).[1..] |> uint32 |> Literal, rst) |> Some
            | _ -> None

        /// Active pattern matching either labels, literals
        /// or a bracketed expression (recursively defined)
        let (|PrimExpr|_|) txt =
            match txt with  
            | LabelExpr (lab, rst) -> (Label lab, rst) |> Some
            | LiteralExpr x -> Some x
            | RegexPrefix "\(" (_, Expr (exp, RegexPrefix "\)" (_, rst)) ) -> (exp, rst) |> Some
            | _ -> None

        /// Higher order active pattern for defining binary operators
        /// NextExpr is the active pattern of the operator with the next
        /// highest precedence. reg is the regex which matches this operator
        /// op is the operation it performs
        let rec (|BinExpr|_|) (|NextExpr|_|) reg op txt =
            match txt with
            | NextExpr (lVal, rhs) ->
                match rhs with
                | RegexPrefix reg (_, BinExpr (|NextExpr|_|) reg op (rVal, rst'))
                    -> (BinOp (op, lVal, rVal), rst') |> Some
                // Can't nest this AP because its the
                // "pass-through" to the next operator
                | _ -> (lVal, rhs) |> Some
            | _ -> None

        // Define active patterns for the binary operators
        // Order of precedence: Add, Sub, Mul
        let (|MulExpr|_|) = (|BinExpr|_|) (|PrimExpr|_|) "\*" (*)
        let (|SubExpr|_|) = (|BinExpr|_|) (|MulExpr|_|) "\-" (-)
        let (|AddExpr|_|) = (|BinExpr|_|) (|SubExpr|_|) "\+" (+)

        match expTxt with
        | AddExpr x -> Some x
        | _ -> None

    