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
                    "branchTarget", 4444u
                ]
    let symbolArrayOf table =
        Map.toList table
        |> List.map (fun (x, _) -> x)
        |> List.toArray

    /// All Symbols
    let symbolArray = symbolArrayOf ts

    /// Symbols which are valid bytes
    let byteSymbolArray =
        Map.filter (fun _ v -> v < 256u) ts
        |> symbolArrayOf

    let indexSymbolArray l (s : string []) =
        s.[(abs l) % (Array.length s)]

     /// DUT of possible literal format representations
    type LiteralFormat =
        | Decimal
        | LowerHex01 // Lowercase prefixed with 0x
        | UpperHex01 // Uppercase prefixed with 0x
        | LowerHex02 // Lowercase prefixed with 0x
        | UpperHex02 // Uppercase prefixed with 0x
        | LowerHexA // Lowercase prefixed with &
        | UpperHexA // Uppercase prefixed with &
        | Binary1
        | Binary2

    /// Format a uint32 into the binary format
    let binFormatter fmt x =
        let rec bin a =
            let bit = string (a % 2u)
            match a with 
            | 0u | 1u -> bit
            | _ -> bin (a / 2u) + bit
        sprintf fmt (bin x)

    /// Map DUT of literal formats to functions which do formatting
    let litFormatters = Map.ofList [
                            Decimal, sprintf "%u"
                            LowerHex01, sprintf "0x%x"
                            UpperHex01, sprintf "0x%X"
                            LowerHex02, sprintf "0X%x"
                            UpperHex02, sprintf "0X%X"
                            LowerHexA, sprintf "&%x"
                            UpperHexA, sprintf "&%X"
                            Binary1, binFormatter "0b%s"
                            Binary2, binFormatter "0B%s"
                        ]

    type TestLiteralConstant = {value : uint32 ; fmt : LiteralFormat}
    type TestLiteral = 
        | Lit of TestLiteralConstant
        | Label of int // Used to index into the symbol array

    /// Apply the format of a TestLiteral to its value
    let appFmt (x : TestLiteral) = 
        match x with
        | Lit l -> litFormatters.[l.fmt] l.value
        | Label l -> indexSymbolArray l symbolArray

    let valFmt (x : TestLiteral) =
        match x with
        | Lit l -> l.value
        | Label l -> ts.[indexSymbolArray l symbolArray]
    
    type ByteTestLiteralConstant = {value : byte ; fmt : LiteralFormat}
    type ByteTestLiteral =
        | ByteLit of ByteTestLiteralConstant
        | ByteLabel of int

    let byteAppFmt (x : ByteTestLiteral) =
        match x with
        | ByteLit l -> litFormatters.[l.fmt] (uint32 l.value)
        | ByteLabel l -> indexSymbolArray l byteSymbolArray