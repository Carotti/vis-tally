module Expressions
    open System.Text.RegularExpressions
    open Expecto

    /// Match the start of txt with pat
    /// Return a tuple of the matched text and the rest
    let (|RegexPrefix|_|) pat txt =
        // Match from start, ignore whitespace
        let m = Regex.Match (txt, "^[\\s]*" + pat + "[\\s]*")
        match m.Success with
        | true -> (m.Value, txt.Substring(m.Value.Length)) |> Some
        | false -> None

    /// Active pattern for matching labels
    /// Also removes any whitespace from around the label
    let (|LabelExpr|_|) txt =
        match txt with 
        | RegexPrefix "[a-zA-Z][a-zA-Z0-9]*" (var, rst) -> 
            // Remove whitespace from the label
            (Regex.Replace(var, "[\\s]*", ""), rst) |> Some
        | _ -> None

    type Expression =
        | Add of Expression * Expression
        | Sub of Expression * Expression
        | Mul of Expression * Expression
        | Label of string
        | Literal of uint32

    let rec eval syms exp =
        let doBinary op x y = 
            match (eval syms x), (eval syms y) with
            | Ok resX, Ok resY -> op resX resY |> Ok
            | Error a, Error b -> a + b |> Error
            | Error a, _ -> Error a
            | _, Error b -> Error b
        match exp with
        | Add (x, y) -> doBinary (+) x y
        | Sub (x, y) -> doBinary (-) x y
        | Mul (x, y) -> doBinary (*) x y
        | Literal x -> x |> Ok
        | Label x ->
            match (Map.containsKey x syms) with
                | true -> syms.[x] |> Ok
                | false -> sprintf "Symbol '%s' not declared" x |> Error

    /// Active pattern for matching expressions
    /// Returns an Expression AST
    let rec (|Expr|_|) expTxt =
        /// Match, parse and evaluate symbols
        let (|LabelExprEval|_|) txt =
            match txt with 
            | LabelExpr (varName, rst) -> (Label varName, rst) |> Ok |> Some
            | _ -> None

        /// Match, parse, evaluate and validate literals
        let (|LiteralExpr|_|) txt = 
            match txt with
            | RegexPrefix "0x[0-9a-fA-F]+" (num, rst) 
            | RegexPrefix "0b[0-1]+" (num, rst)
            | RegexPrefix "[0-9]+" (num, rst) -> 
                (uint32 num |> Literal, rst) |> Ok |> Some
            | RegexPrefix "&[0-9a-fA-F]+" (num, rst) -> 
                // This one is a bit hacky, fix this at some point..
                (uint32 ("0x" + num.[1..]) |> Literal, rst) |> Ok |> Some
            | _ -> None

        /// Active pattern matching either labels, literals
        /// or a bracketed expression (recursively defined)
        let (|PrimExpr|_|) txt =
            match txt with  
            | LabelExprEval x -> Some x
            | LiteralExpr x -> Some x
            | RegexPrefix "\(" (_, Expr (Ok (exp, rst)) ) ->
                match rst with
                | RegexPrefix "\)" (_, rst') -> Ok (exp, rst') |> Some
                | _ -> sprintf "Unmatched bracket at '%s'" rst |> Error |> Some
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
                    -> Result.map (fun (rVal, rst') -> op (lVal, rVal), rst') x |> Some
                // Can't nest this AP because its the
                // "pass-through" to the next operator
                | _ -> Ok (lVal, rhs) |> Some
            | _ -> None

        // Define active patterns for the binary operators
        // Order of precedence: Add, Sub, Mul
        let (|MulExpr|_|) = (|BinExpr|_|) (|PrimExpr|_|) "\*" Mul
        let (|SubExpr|_|) = (|BinExpr|_|) (|MulExpr|_|) "\-" Sub
        let (|AddExpr|_|) = (|BinExpr|_|) (|SubExpr|_|) "\+" Add

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
        /// Check exp matches the evaluated expression
        let expEqual syms exp txt =
            match txt with
            | Expr (Ok (ans, "")) -> 
                match eval syms ans with
                | Ok res when res = exp -> true
                | _ -> false
            | _ -> false

        /// Check a formatted literal evaluates to itself
        let literal lit =
            expEqual Map.empty lit.value (appFmt lit)

        /// Check a formatted binary operation evaluates to its result
        let binOp o lit1 lit2 =
            ((appFmt lit1) + o.op + (appFmt lit2))
            |> expEqual Map.empty (o.f lit1.value lit2.value)

        /// Check a formatted expression with 2 operators evaluates correctly
        /// If first is true, op1 should have higher precedence than op2
        let precedence o1 o2 first lit1 lit2 lit3 = 
            let res =
                match first with
                | true -> (o2.f (o1.f lit1.value lit2.value) lit3.value)
                | false -> (o1.f lit1.value (o2.f lit2.value lit3.value))
            ((appFmt lit1) + o1.op + (appFmt lit2) + o2.op + (appFmt lit3))
            |> expEqual Map.empty res

        /// Check any literal nested in a lot of brackets still evaluates
        /// correctly
        let bracketNest lit =
            "((((((((" + (appFmt lit) + "))))))))"
            |> expEqual Map.empty lit.value

        /// Simple check for any uint32 mapped to symbol 'testSymbol123'
        let symbol x = 
            let table = Map.ofList [
                            "testSymbol123", x
                        ]
            expEqual table x "testSymbol123"

        /// Unit Test constructor
        let unitTest name syms exp txt =
            let ans = 
                match txt with
                | Expr (Ok (ans', "")) ->
                    match eval syms ans' with
                    | Ok res -> Some res
                    | _ -> None
                | _ -> None
            testCase name <| fun () ->
                Expect.equal ans (Some exp) txt

        /// Example symbol table used for unit tests
        let ts = Map.ofList [
                        "a", 192u
                        "moo", 17123u
                        "J", 173u
                        "fOO", 402u
                        "Bar", 19721u
                        "z1", 139216u
                        "rock74", 16u
                        "Nice1", 0xF0F0F0F0u
                        "Nice2", 0x0F0F0F0Fu
                        "bigNum", 0xFFFFFFFFu
                        "n0thing", 0u
                    ]

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
                unitTest "1" Map.empty 24u "(6 + 2) * 3"
                unitTest "2" Map.empty 16u "27 - (9 + 2)"
                unitTest "3" Map.empty 7u  "25 - (2 * 9)"
                unitTest "4" Map.empty 35u "(6 - 2) * (3 + 4) + 7"
                unitTest "5" Map.empty 1473u "((0b111 * 0xff) - ((16 + 0xA)*(12)))"
                unitTest "6" Map.empty 0u "0xffFFffFF - 0b11111111111111111111111111111111"
                unitTest "7" Map.empty 16u "0xFFFFFFFF + 17"
                unitTest "8" Map.empty 0xFFFFFFFFu "0xF0F0F0F0 + 0x0F0F0F0F"
                unitTest "9" Map.empty 17u "( 17 )"
                unitTest "10" Map.empty 27u "(\t27\t)\t"
                unitTest "11" Map.empty 19u "19 "
                unitTest "12" Map.empty 21u "\t21\t"
            ]
            testList "Unit Symbols" [
                unitTest "1" ts 192u "a"
                unitTest "2" ts 173u "J"
                unitTest "3" ts 199u "a + &7"
                unitTest "4" ts 199u "a+&7"
                unitTest "5" ts 199u "&7 + a"
                unitTest "6" ts 199u "&7+a"
                unitTest "7" ts 199u "&7\t+\ta"
                unitTest "8" ts 199u "a\t+\t&7"
                unitTest "9" ts 384u "a + a"
                unitTest "10" ts 33224u "8 + J * a"
                unitTest "11" ts 0u "n0thing * bigNum"
                unitTest "12" ts 17123u "(moo)"
                unitTest "13" ts 17123u "((((moo))))"
                unitTest "14" ts 192u "\ta"
                unitTest "15" ts 192u "a\t"
                unitTest "16" ts 192u " a"
                unitTest "17" ts 192u "a "
                unitTest "18" ts 192u "\ta "
            ]
        ]


