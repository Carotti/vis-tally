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
        let ins = produceMisc txt
        execute ins (initialDp ()) assumedMemBase

    /// Generate a DCD instruction from a list of test literals
    /// Only actually use the first 13 due to test framework limitations
    /// Accept one, since need at least one element
    let sameAsVisualDCD (dataFirst : TestLiteral) (data : TestLiteral list) labelIndex =
        let startTxt = sprintf "%s DCD %s" (indexSymbolArray labelIndex) (appFmt dataFirst)
        let txt =
            Seq.truncate 12 data
            |> Seq.toList
            |> List.fold (fun x y -> sprintf "%s, %s" x (appFmt y)) startTxt
        (runMisc txt |> getDpDataMem) = (runVisualGetMem txt |> getDpDataMem)

    let unitTest name ins =
        testCase name <| fun () ->
        Expect.equal (runMisc ins |> getDpDataMem) (runVisualGetMem ins |> getDpDataMem) ins

    [<Tests>]
    let miscTests =
        testList "Misc Tests Against Visual" [
            testPropertyVis "DCD literals" sameAsVisualDCD
            testList "DCD Unit Tests" [
                unitTest "Addition Expression" "tstLab DCD 17 + 12"
                unitTest "Subtraction Expression +ve" "tstLab DCD 17 - 12"
                unitTest "Subtraction Expression -ve" "tstLab DCD 12 - 129"
                unitTest "Multiplication Expression" "tstLab DCD 19 * 47"
                unitTest "Overflow behaviour" "tstLab DCD 0x3FFFFFFF * 7"
                unitTest "Multiple Expressions" "tstLab DCD 17 * 3, 44 + 0x3, 0x19 - &194, 0b1111 * &F"
            ]
        ]