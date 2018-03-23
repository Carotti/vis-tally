//////////////////////////////////////////////////////////////////////////////////////////
//                   Branch Instruction Implementation Module
//////////////////////////////////////////////////////////////////////////////////////////

module Branch
    open CommonData
    open CommonLex
    open Expressions
    open Errors
    
    type BranchInstr =
        | B of SymbolExp
        | BL of SymbolExp
        | END

        // Type returned after parsing
    type Instr =
        | Branch of BranchInstr

    /// Resolve the symbols for an instruction which requires it
    let resolvePInstr (syms : SymbolTable) ins =
        match ins with
        | B exp -> evalSymExp syms exp |> Result.map B
        | BL exp -> evalSymExp syms exp |> Result.map BL
        | _ -> Ok ins // Symbol is already resolved

    let resolve (syms : SymbolTable) ins =
        resolvePInstr syms ins

    // Branch instructions have no suffixes
    let branchSpec = {
        InstrC = BRANCH
        Roots = ["B";"BL";"END"]
        Suffixes = [""]
    }

    /// map of all possible opcodes recognised
    let opCodes = opCodeExpand branchSpec

    let parse (ls: LineData) : Result<Parse<Instr>,ErrParse> option =

        let bindB t =
            match ls.Operands with
            | Expr (exp, "") -> Ok exp
            | _ -> 
                (ls.Operands, " is an invalid expression.")
                ||> makeError
                |> ``Invalid expression``
                |> Error
            |> Result.map (ExpUnresolved >> t)

        let parse' (_instrC, ((root : string),_suffix,pCond)) =
            let (WA la) = ls.LoadAddr // address this instruction is loaded into memory
            match root.ToUpper () with
            | "B" -> bindB B
            | "BL" -> bindB BL
            | "END" -> Ok END
            | _ -> failwithf "Unexpected root in Misc.parse"
            |> Result.map (fun ins ->
                { 
                    PInstr = ins |> Branch
                    PLabel = ls.Label |> Option.map (fun lab -> lab, la) ; 
                    PSize = 4u; 
                    PCond = pCond 
                })

        Map.tryFind (ls.OpCode.ToUpper ()) opCodes // lookup opcode to see if it is known
        |> Option.map parse' // if unknown keep none, if known parse it.

    /// Parse Active Pattern used by top-level code
    let (|IMatch|_|) = parse

    