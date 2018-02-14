module Expressions
    open System.Text.RegularExpressions
    open Expecto    

    /// Match the start of txt with pat
    /// Return a tuple of the matched text and the rest
    let (|RegexPrefix|_|) pat txt =
        // Match from start, also don't care about whitespace
        let m = Regex.Match (txt, "^[\\s]*" + pat + "[\\s]*")
        match m.Success with
        | true -> (m.Value, txt.Substring(m.Value.Length)) |> Some
        | false -> None

    /// Active pattern for matching mathematical expressions
    /// restricted indicates whether or not literals are restricted
    /// to those which can be reprented by 1 byte RORed an even no
    let rec (|Expr|_|) restricted st expTxt =
        /// Match, parse and evaluate symbols
        let (|LabelExpr|_|) txt =
            match txt with 
            | RegexPrefix "[a-zA-Z][a-zA-Z0-9]*" (var, rst) ->
                match st with
                | Some symTab -> 
                    match (Map.containsKey var symTab) with
                    | true -> (symTab.[var], rst) |> Ok |> Some
                    | false -> sprintf "Symbol '%s' not declared" var |> Error |> Some
                | None -> "No Symbol table exists" |> Error |> Some
            | _ -> None

        /// Match, parse, evaluate and validate literals
        let (|LiteralExpr|_|) txt = 
            let validLiteral (x, rst) =
                let rec validLiteral' r =
                    let rotRight (y : uint32) n = (y >>> n) ||| (y <<< (32 - n))
                    match (rotRight x r < 256u, r > 30) with
                    | _, true -> sprintf "Invalid Literal '%d'" x |> Error
                    | true, _ -> Ok (x, rst)
                    | false, false -> validLiteral' (r + 2)
                validLiteral' 0
            let ret =
                match restricted with
                | true -> Ok >> (Result.bind validLiteral) >> Some
                | false-> Ok >> Some
            match txt with
            | RegexPrefix "[0-9]+" (num, rst) 
            | RegexPrefix "0x[0-9a-fA-F]+" (num, rst) 
            | RegexPrefix "0b[0-1]+" (num, rst) -> (uint32 num, rst) |> ret
            | RegexPrefix "&[0-9a-fA-F]+" (num, rst) -> (uint32 ("0x" + num.[1..]), rst) |> ret
            | _ -> None

        /// Active pattern matching either labels, literals
        /// or a bracketed expression (recursively defined)
        let (|PrimExpr|_|) txt =
            match txt with  
            | LabelExpr x -> Some x
            | LiteralExpr x -> Some x
            | RegexPrefix "\(" (_, Expr restricted st (Ok (exp, rst : string)) ) ->
                match rst.StartsWith ")" with
                | true -> Ok (exp, rst.[1..]) |> Some
                | false -> sprintf "Unmatched bracket at '%s'" rst |> Error |> Some
            | _ -> None

        /// Higher order active pattern for defining binary operators
        /// NextExpr is the active pattern of the operator with the next
        /// highest precedence. reg is the regex which matches this operator
        /// op is the operation it performs
        let rec (|BinExpr|_|) (|NextExpr|_|) reg op txt =
            match txt with
            | NextExpr (Ok (lVal, rhs)) ->
                match rhs with
                | RegexPrefix reg (_, BinExpr (|NextExpr|_|) reg op x)
                    -> Result.map (fun (rVal, rst') -> op lVal rVal, rst') x |> Some
                // Can't nest this AP because its the
                // "pass-through" to the next operator
                | _ -> Ok (lVal, rhs) |> Some
            | _ -> None

        // Define active patterns for the binary operators
        // Order of precedence: Add, Sub, Mul
        let (|MulExpr|_|) = (|BinExpr|_|) (|PrimExpr|_|) "\*" (*)
        let (|SubExpr|_|) = (|BinExpr|_|) (|MulExpr|_|) "\-" (-)
        let (|AddExpr|_|) = (|BinExpr|_|) (|SubExpr|_|) "\+" (+)

        match expTxt with
        | AddExpr x -> Some x
        | _ -> None

    let okExprParse txt syms = 
        match txt with
        | Expr false syms (Ok (ans, "")) -> Some ans
        | _ -> None

    [<Tests>]
    let exprPropertyTests = 
      testProperty "Basic addition" <|
         fun (x : uint32, y : uint32) -> okExprParse (sprintf "%u + %u" x y) None = Some (x + y)