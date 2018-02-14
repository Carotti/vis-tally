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

    // *** Everything below here is just used for testing the expression module ***

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

    type TestLiteral = {value : uint32 ; fmt : LiteralFormat}

    /// Apply the format of a TestLiteral to its value
    let appFmt x = litFormatters.[x.fmt] x.value

    type BinaryOperator = {op : string ; f : (uint32 -> uint32 -> uint32)}
    let add = {op = "+" ; f = (+)}
    let subtract = {op = "-" ; f = (-)}
    let multiply = {op = "*" ; f = (*)}

    [<Tests>]
    let exprPropertyTests = 
        /// Attempt to parse txt with Expr
        /// Check res matches the evaluated expression
        let expEqual syms res txt =
            match txt with
            | Expr syms (Ok (ans, "")) when ans = res -> true
            | _ -> false

        /// Check a formatted literal evaluates to itself
        let literal lit =
            expEqual None lit.value (appFmt lit)

        /// Check a formatted binary operation evaluates to its result
        let binOp o lit1 lit2 =
            ((appFmt lit1) + o.op + (appFmt lit2))
            |> expEqual None (o.f lit1.value lit2.value)

        /// Check a formatted expression with 2 operators evaluates correctly
        /// If first is true, op1 should have higher precedence than op2
        let precedence o1 o2 first lit1 lit2 lit3 = 
            let res =
                match first with
                | true -> (o2.f (o1.f lit1.value lit2.value) lit3.value)
                | false -> (o1.f lit1.value (o2.f lit2.value lit3.value))
            ((appFmt lit1) + o1.op + (appFmt lit2) + o2.op + (appFmt lit3))
            |> expEqual None res

        /// Check any literal nested in a lot of brackets still evaluates
        /// correctly
        let bracketNest lit =
            "((((((((" + (appFmt lit) + "))))))))"
            |> expEqual None lit.value

        /// Simple check for any uint32 mapped to symbol 'testSymbol123'
        let symbol x = 
            let table = Map.ofList [
                            "testSymbol123", x
                        ]
            expEqual (Some table) x "testSymbol123"

        /// Unit Test constructor
        let unitTest name syms res txt =
            let expected = 
                match txt with
                | Expr syms (Ok (ans, "")) -> Some ans
                | _ -> None
            testCase name <| fun () ->
                Expect.equal (Some res) expected txt

        testList "Expression Parsing" [
            testProperty "Literals are the same" literal
            testList "Binary Operators" [
                testProperty "Addition" 
                    <| binOp add
                testProperty "Subtraction" 
                    <| binOp subtract
                testProperty "Multiplication" 
                    <| binOp multiply
            ]
            testList "Operator Precedence" [
                testProperty "Multiplication then Addition" 
                    <| precedence multiply add true
                testProperty "Multiplication then Subtraction"
                    <| precedence multiply subtract true
                testProperty "Addition then Multiplication"
                    <| precedence add multiply false
                testProperty "Subtraction then Multiplication"
                    <| precedence subtract multiply false
                testProperty "Addition then Subtraction"
                    <| precedence add subtract // Don't care about precedence here
            ]
            testProperty "Literal in Nested Brackets"
                <| bracketNest
            testProperty "Symbol"
                <| symbol
            testList "Unit Literals" [
                unitTest "Unit1" None 24u       "(6 + 2) * 3"
                unitTest "Unit2" None 16u       "27 - (9 + 2)"
                unitTest "Unit3" None 7u        "25 - (2 * 9)"
                unitTest "Unit4" None 35u       "(6 - 2) * (3 + 4) + 7"
                unitTest "Unit5" None 1473u     "((0b111 * 0xff) - ((16 + 0xA)*(12)))"
            ]
        ]


