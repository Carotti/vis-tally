module MiscTest
    open CommonTop
    open CommonData
    open CommonLex

    open TestTop
    open TestFormats
    open Misc

    open Execution
    open Expressions

    open Expecto

    /// Hacky but used to downcast from the toplevel parse function
    /// to a MISC instruction
    let miscDowncast (ins : Parse<CommonTop.Instr>) =
        match ins.PInstr with
        | IMISC miscIns -> miscIns
        | _ -> failwithf "Invalid downcast to MISC"

    let produceMisc = produceTop resolve miscDowncast

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
    
    /// Generate a DCB instruction from a list of test byte literals
    /// Only actually use the first 52 (first 13 words of memory)
    let sameAsVisualDCB (dataFirst : ByteTestLiteral) (data: ByteTestLiteral list) =
        let startTxt = sprintf "dcbtstlab DCB %s" (byteAppFmt dataFirst)
        formatData startTxt data 51 byteAppFmt
        |> sameResult

    /// Construct a unit test which is run against visUAL
    let unitTestV name ins =
        unitTest name ins <|
            (runVisualGetMem ins |> getDpDataMem) <|
            (runMisc ins |> getDpDataMem)

    /// Check EQU parses and expression evaluates correctly
    let correctEQU (labelIndex : int) (data : TestLiteral) =
        let ins = sprintf "%s EQU %s" <|
                    (indexSymbolArray labelIndex symbolArray) <|
                    (appFmt data)
        produceMisc ins
        |> function
            | EQU (ExpResolved value) when value = (valFmt data) -> true
            | _ -> false

    /// Check EQU parses and resolves correctly
    let unitTestEQU name txt expected =
        let res = 
            match produceMisc txt with
            | EQU (ExpResolved x) -> x
            | _ -> ~~~expected // Anything else make sure test fails
        unitTest name txt expected res

    /// Produce a Unit test for FILL/SPACE instructions
    /// where we want to check memory is in the correct state
    let unitTestFS name txt expected =
        txt
        |> runMisc
        |> getDpDataMem
        |> unitTest name txt expected

    [<Tests>]
    let visualTests =
        testList "Misc Tests Against Visual" [
            testPropertyVis "DCD" sameAsVisualDCD
            testPropertyVis "DCB" sameAsVisualDCB
            testList "DCD Unit Tests" [
                unitTestV "Addition Expression" "tstLab DCD 17 + 12"
                unitTestV "Subtraction Expression +ve" "tstLab DCD 17 - 12"
                unitTestV "Subtraction Expression -ve" "tstLab DCD 12 - 129"
                unitTestV "Multiplication Expression" "tstLab DCD 19 * 47"
                unitTestV "Overflow behaviour" "tstLab DCD 0x3FFFFFFF * 7"
                unitTestV "Multiple Expressions" "tstLab DCD 17 * 3, 44 + 0x3, 0x19 - &194, 0b1111 * &F"
            ]
            testList "DCB Unit Tests" [
                unitTestV "Endianness" "tstLab DCB 0xAA, 0xBB, 0xCC, 0xDD"
                unitTestV "Byte Addition" "tstLab DCB 252 + 2"
                unitTestV "Byte Subtraction" "tstLab DCB 257 - 2"
                unitTestV "Byte Multiplication" "tstLab DCB 51 * 5"
            ]
        ]

    [<Tests>]
    // Can't really test EQU/FILL/SPACE against visUAL since
    // behaviour is different
    let otherTests =
        testList "Misc Non-Visual Tests" [
            testList "EQU" [
                testProperty "EQU resolves" correctEQU
                unitTestEQU "Constant" "tstlab123 EQU 176" 176u
                unitTestEQU "Other label" "other EQU Bar" 19721u
                unitTestEQU "Add Expression" "label EQU 18 + 4" 22u
                unitTestEQU "Multiply Expression" "label EQU 3 * 26" 78u
            ]
            testList "FILL" [
                unitTestFS <|
                    "Unaligned fill amount custom value" <|
                    "FILL 17, 0xAB" <|
                    Map.ofList [
                                (WA 4096u, 2880154539u); (WA 4100u, 2880154539u);
                                (WA 4104u, 2880154539u); (WA 4108u, 2880154539u);
                                (WA 4112u, 171u)
                    ]
                unitTestFS <|
                    "Filling with 0" <|
                    "FILL 0" <|
                    Map.ofList []
                unitTestFS <|
                    "Filling with 0 custom value" <|
                    "FILL 0, 0x7F" <|
                    Map.ofList []
                unitTestFS <|
                    "Filling with 1 custom value" <|
                    "FILL 1, 0x55" <|
                    Map.ofList [WA 4096u, 85u]
                unitTestFS <|
                    "Filling with custom value of 0" <|
                    "FILL 8, 0x0" <|
                    Map.ofList []
                unitTestFS <|
                    "Filling with custom value of a label" <|
                    "FILL 8, rock74" <|
                    Map.ofList [(WA 4096u, 269488144u); (WA 4100u, 269488144u)]
            ]
            testList "SPACE" [
                unitTestFS <|
                    "SPACE of 0" <|
                    "SPACE 0" <|
                    Map.ofList []       
                unitTestFS <|
                    "SPACE of 10" <|
                    "SPACE 10" <|
                    Map.ofList []      
            ]
        ]
