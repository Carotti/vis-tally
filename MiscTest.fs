module MiscTest
    open CommonTop
    open CommonData
    open CommonLex

    open TestTop
    open Misc

    open Execution

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