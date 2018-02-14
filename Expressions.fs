module Expressions
    open System.Text.RegularExpressions
    open Expecto
    open System.Threading.Tasks

    /// Match the start of txt with pat
    /// Return a tuple of the matched text and the rest
    let (|RegexPrefix|_|) pat txt =
        // Match from start, also don't care about whitespace
        let m = Regex.Match (txt, "^[\\s]*" + pat + "[\\s]*")
        match m.Success with
        | true -> (m.Value, txt.Substring(m.Value.Length)) |> Some
        | false -> None

    /// Active pattern for matching mathematical expressions
    let rec (|Expr|_|) st expTxt =
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
            match txt with
            | RegexPrefix "0x[0-9a-fA-F]+" (num, rst) 
            | RegexPrefix "0b[0-1]+" (num, rst)
            | RegexPrefix "[0-9]+" (num, rst) -> (uint32 num, rst) |> Ok |> Some
            | RegexPrefix "&[0-9a-fA-F]+" (num, rst) -> (uint32 ("0x" + num.[1..]), rst) |> Ok |> Some
            | _ -> None

        /// Active pattern matching either labels, literals
        /// or a bracketed expression (recursively defined)
        let (|PrimExpr|_|) txt =
            match txt with  
            | LabelExpr x -> Some x
            | LiteralExpr x -> Some x
            | RegexPrefix "\(" (_, Expr st (Ok (exp, rst : string)) ) ->
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

    /// DUT of possible literal format representations
    type LiteralFormat =
        | Decimal
        | LowerHex0 // Lowercase prefixed with 0x
        | UpperHex0 // Uppercase prefixed with 0x
        | LowerHexA // Lowercase prefixed with &
        | UpperHexA // Uppercase prefixed with &
        | Binary

    /// Format a uint32 into the binary format
    let binFormatter x =
        let rec bin (a : uint32) =
            let bit = string (a % 2u)
            match a with 
            | 0u | 1u -> bit
            | _ -> bin (a / 2u) + bit
        sprintf "0b%s" (bin x)

    /// Map DUT of literal formats to functions which do formatting
    let litFormatters = Map.ofList [
                            Decimal, sprintf "%u"
                            LowerHex0, sprintf "0x%x"
                            UpperHex0, sprintf "0x%X"
                            LowerHexA, sprintf "&%x"
                            UpperHexA, sprintf "&%X"
                            Binary, binFormatter
                        ]

    /// Record for any kind of test literal
    type TestLiteral = {value : uint32 ; fmt : LiteralFormat}

    /// Apply the format of a TestLiteral to its value
    let appFmt x = litFormatters.[x.fmt] x.value

    [<Tests>]
    let exprPropertyTests = 

        /// Attempt to parse txt with Expr
        /// Check res matches the evaluated expression
        let expEqual syms txt res =
            match txt with
            | Expr syms (Ok (ans, "")) when ans = res -> true
            | _ -> false

        /// Check a formatted literal evaluates to itself
        let litIsSame lit =
            expEqual None (appFmt lit) lit.value

        /// Check a formatted binary operation evaluates to its result
        let binOpIsSame op f lit1 lit2 =
            expEqual None ((appFmt lit1) + op + (appFmt lit2)) (f lit1.value lit2.value)

        testList "Expression Parsing" [
            testProperty "Literals are the same" litIsSame
            testList "Literal Binary Operators" [
                testProperty "Addition" <| binOpIsSame "+" (+)
                testProperty "Subtraction" <| binOpIsSame "-" (-)
                testProperty "Multiplication" <| binOpIsSame "*" (*)
            ]
        ]


