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
            | RegexPrefix "0x[0-9a-fA-F]+" (num, rst) 
            | RegexPrefix "0b[0-1]+" (num, rst)
            | RegexPrefix "[0-9]+" (num, rst) -> (uint32 num, rst) |> ret
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

    type LitFormat = {name : string ; fmt : (uint32 -> string)}

    [<Tests>]
    let exprPropertyTests = 
        /// Attempt to parse txt with Expr
        /// Either return uint32 result else None
        let okExprParse syms txt = 
            match txt with
            | Expr false syms (Ok (ans, "")) -> Some ans
            | _ -> None

        /// Format a uint32 into the binary format
        let binFormatter x =
            let rec bin (a : uint32) =
                let bit = string (a % 2u)
                match a with 
                | 0u | 1u -> bit
                | _ -> bin (a / 2u) + bit
            sprintf "0b%s" (bin x)
        
        let litFormats = [
            {name = "Decimal"; fmt = sprintf "%u"}
            {name = "Lowercase 0x Hexadecimal"; fmt = sprintf "0x%x"}
            {name = "Uppercase 0x Hexadecimal"; fmt = sprintf "0x%X"}
            {name = "Lowercase & Hecadecimal"; fmt = sprintf "&%x"}
            {name = "Uppercase & Hexadecimal"; fmt = sprintf "&%X"}
            {name = "Binary"; fmt = binFormatter}
        ]

        /// Check that all formats of a literal parse to be the same value
        /// as the literal itself
        let testLiteral =
            let testLitFmt fmt a =
                okExprParse None (fmt a) = Some a
            litFormats
                |> List.map (fun x -> testProperty x.name (testLitFmt x.fmt))

        /// Test all possible combinations of number representations
        /// for a particular binary operator that the parsed result is
        /// the same as the operation itself
        let testBinaryOp opName op f =
            let testBinFmt fmt1 fmt2 a1 a2 =
                okExprParse None ((fmt1 a1) + op + (fmt2 a2)) = Some (f a1 a2)
            let makePropTest (x, y) =
                testProperty (x.name + " by " + y.name) (testBinFmt x.fmt y.fmt)
            litFormats
                |> List.allPairs litFormats
                |> List.map makePropTest
                |> testList opName

        testList "Expression Parsing" [
            testList "Literals" testLiteral
            testList "Literal Binary Operators" [
                testBinaryOp "Addition" "+" (+)
                testBinaryOp "Subtraction" "-" (-)
                testBinaryOp "Multiplication" "*" (*)
            ]
        ]


