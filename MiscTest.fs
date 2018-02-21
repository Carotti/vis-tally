module MiscTest
    open CommonTop
    open CommonData
    open CommonLex

    open TestTop
    open TestFormats
    open Misc

    open Execution

    open Expecto

    /// Hacky but used to downcast from the toplevel parse function
    /// to a MISC instruction
    let miscDowncast (ins : Parse<CommonTop.Instr>) =
        match ins.PInstr with
        | IMISC miscIns -> miscIns
        | _ -> failwithf "Invalid downcast"

    let produceMisc txt = 
        // Don't care about the word address for these instructions
        let ins = parseLine (Some ts) (WA 0u) txt
        match ins with 
        | Ok top ->
            match Misc.resolve ts (miscDowncast top) with
            | Ok miscIns -> miscIns
            | _ -> failwithf "Invalid symbol for MISC"
        | _ -> failwithf "Invalid MISC text"

    let runMisc txt : DataPath<CommonTop.Instr> = 
        execute (initialDp (), assumedMemBase) (produceMisc txt) |> fst

    // Run multiple misc instructions, seperated by newlines
    let runMultipleMisc (txt : string) : DataPath<CommonTop.Instr> =
        txt.Split([|'\n'|])
        |> Array.toList
        |> List.map produceMisc
        |> List.fold execute (initialDp (), assumedMemBase)
        |> fst

    let formatData startTxt (lst : 'a list) num (formatter : 'a -> string) =
        Seq.truncate num lst
        |> Seq.toList
        |> List.fold (fun x y -> sprintf "%s, %s" x (formatter y)) startTxt

    let sameResult txt =
        (runMisc txt |> getDpDataMem) = (runVisualGetMem txt |> getDpDataMem)

    /// Generate a DCD instruction from a list of test literals
    /// Only actually use the first 13 due to test framework limitations
    /// Accept one, since need at least one element
    let sameAsVisualDCD (dataFirst : TestLiteral) (data : TestLiteral list) =
        let startTxt = sprintf "dcdtstlab DCD %s" (appFmt dataFirst)
        formatData startTxt data 12 appFmt// Only the first 13 words of memory        
        |> sameResult
        
    let sameAsVisualDCB (dataFirst : ByteTestLiteral) (data: ByteTestLiteral list) =
        let startTxt = sprintf "dcbtstlab DCB %s" (byteAppFmt dataFirst)
        formatData startTxt data 51 byteAppFmt
        |> sameResult

    let unitTest name ins =
        testCase name <| fun () ->
        Expect.equal (runMisc ins |> getDpDataMem) (runVisualGetMem ins |> getDpDataMem) ins

    // Since comparison ignored zeroed memory, only way to test vs Visual FILL
    // is with an unaligned DCB then a FILL


    [<Tests>]
    let visualTests =
        testList "Misc Tests Against Visual" [
            testPropertyVis "DCD" sameAsVisualDCD
            testPropertyVis "DCB" sameAsVisualDCB
            testList "DCD Unit Tests" [
                unitTest "Addition Expression" "tstLab DCD 17 + 12"
                unitTest "Subtraction Expression +ve" "tstLab DCD 17 - 12"
                unitTest "Subtraction Expression -ve" "tstLab DCD 12 - 129"
                unitTest "Multiplication Expression" "tstLab DCD 19 * 47"
                unitTest "Overflow behaviour" "tstLab DCD 0x3FFFFFFF * 7"
                unitTest "Multiple Expressions" "tstLab DCD 17 * 3, 44 + 0x3, 0x19 - &194, 0b1111 * &F"
            ]
            testList "DCB Unit Tests" [
                unitTest "Endianness" "tstLab DCB 0xAA, 0xBB, 0xCC, 0xDD"
                unitTest "Byte Addition" "tstLab DCB 252 + 2"
                unitTest "Byte Subtraction" "tstLab DCB 257 - 2"
                unitTest "Byte Multiplication" "tstLab DCB 51 * 5"
            ]
        ]

    /// Check EQU parses and expression evaluates correctly
    let correctEQU (labelIndex : int) (data : TestLiteral) =
        let ins = sprintf "%s EQU %s" <|
                    (indexSymbolArray labelIndex symbolArray) <|
                    (appFmt data)
        produceMisc ins
        |> resolve ts
        |> function
            | Ok (EQU (ExpResolved value)) when value = (valFmt data) -> true
            | _ -> false

    [<Tests>]
    // Can't really test EQU/FILL/SPACE against visUAL since
    // behaviour is different
    let otherTests =
        testList "EQU" [
            testProperty "EQU resolves" correctEQU
        ]