module ExpressionTest
    open Expecto
    
    open Expressions
    open TestFormats

    type BinaryOperator = {op : string ; f : (uint32 -> uint32 -> uint32)}
    let add = {op = "+" ; f = (+)}
    let subtract = {op = "-" ; f = (-)}
    let multiply = {op = "*" ; f = (*)}

    [<Tests>]
    let exprTests = 
        /// Attempt to parse txt with Expr
        /// Check exp matches the evaluated expression
        let expEqual syms exp txt =
            match txt with
            | Expr (ans, "") -> 
                match eval syms ans with
                | Ok res when res = exp -> true
                | _ -> false
            | _ -> false

        /// Check a formatted literal evaluates to itself
        let literal lit =
            expEqual ts (valFmt lit) (appFmt lit)

        /// Check a formatted binary operation evaluates to its result
        let binOp o lit1 lit2 =
            ((appFmt lit1) + o.op + (appFmt lit2))
            |> expEqual ts (o.f (valFmt lit1) (valFmt lit2))

        /// Check a formatted expression with 2 operators evaluates correctly
        /// If first is true, op1 should have higher precedence than op2
        let precedence o1 o2 first lit1 lit2 lit3 = 
            let res =
                match first with
                | true -> (o2.f (o1.f (valFmt lit1) (valFmt lit2)) (valFmt lit3))
                | false -> (o1.f (valFmt lit1) (o2.f (valFmt lit2) (valFmt lit3)))
            ((appFmt lit1) + o1.op + (appFmt lit2) + o2.op + (appFmt lit3))
            |> expEqual ts res

        /// Check any literal nested in a lot of brackets still evaluates
        /// correctly
        let bracketNest lit =
            "((((((((" + (appFmt lit) + "))))))))"
            |> expEqual ts (valFmt lit)

        /// Unit Test constructor
        let unitTest name syms exp txt =
            let ans = 
                match txt with
                | Expr (ans', "") ->
                    match eval syms ans' with
                    | Ok res -> Some res
                    | _ -> None
                | _ -> None
            testCase name <| fun () ->
                Expect.equal ans (Some exp) txt

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
                unitTest "13" Map.empty 5u " &5"
                unitTest "14" Map.empty 0u "6 - 2 - 4"
                unitTest "15" Map.empty 0u "28 - 1 - 2 - 3 - 4 - 5 - 6 - 7"
            ]
            testList "Unit Symbols" [
                unitTest "1" ts 192u "aa"
                unitTest "2" ts 173u "JJ"
                unitTest "3" ts 199u "aa + &7"
                unitTest "4" ts 199u "aa+&7"
                unitTest "5" ts 199u "&7 + aa"
                unitTest "6" ts 199u "&7+aa"
                unitTest "7" ts 199u "&7\t+\taa"
                unitTest "8" ts 199u "aa\t+\t&7"
                unitTest "9" ts 384u "aa + aa"
                unitTest "10" ts 33224u "8 + JJ * aa"
                unitTest "11" ts 0u "n0thing * bigNum"
                unitTest "12" ts 17123u "(moo)"
                unitTest "13" ts 17123u "((((moo))))"
                unitTest "14" ts 192u "\taa"
                unitTest "15" ts 192u "aa\t"
                unitTest "16" ts 192u " aa"
                unitTest "17" ts 192u "aa "
                unitTest "18" ts 192u "\taa "
            ]
        ]


