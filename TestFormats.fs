module TestFormats

    // Symbol table used by all tests, AND in visUAL prelude
    let ts = Map.ofList [
                    "aa", 192u
                    "moo", 17123u
                    "JJ", 173u
                    "fOO", 402u
                    "Bar", 19721u
                    "z1", 139216u
                    "rock74", 16u
                    "Nice1", 0xF0F0F0F0u
                    "Nice2", 0x0F0F0F0Fu
                    "bigNum", 0xFFFFFFFFu
                    "n0thing", 0u
                ]

    let symbolArray =
        Map.toList ts
        |> List.map (fun (x, _) -> x)
        |> List.toArray

    let indexSymbolArray l =
        symbolArray.[(abs l) % (Array.length symbolArray)]

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
        let rec bin a =
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

    type TestLiteralConstant = {value : uint32 ; fmt : LiteralFormat}
    type TestLiteral = 
        | Lit of TestLiteralConstant
        | Label of int // Used to index into the symbol array

    /// Apply the format of a TestLiteral to its value
    let appFmt (x : TestLiteral) = 
        match x with
        | Lit l -> litFormatters.[l.fmt] l.value
        | Label l -> indexSymbolArray l

    let valFmt (x : TestLiteral) =
        match x with
        | Lit l -> l.value
        | Label l -> ts.[indexSymbolArray l]