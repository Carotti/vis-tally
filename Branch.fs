//////////////////////////////////////////////////////////////////////////////////////////
//                   Sample (skeleton) instruction implementation modules
//////////////////////////////////////////////////////////////////////////////////////////

module Branch
    open CommonData
    open CommonLex
    open Expressions
    open Execution

    // Symbols become uint32 when they are resolved
    type Symbol =
        | SymUnresolved of string
        | SymResolved of uint32
        
    // Type returned after parsing
    type Instr =
        | B of Symbol
        | BL of Symbol
        | END

    type ErrRunTime =
        | EXIT // Used to exit execution of the simulation

    type ErrInstr = 
        | NoLabel

    /// Resolve the symbols for an instruction which requires it
    let resolveBranch ins (syms : SymbolTable) =
        let lookup which sym =
            match syms.ContainsKey sym with
            | true -> which (SymResolved syms.[sym]) |> Ok
            | false -> sprintf "Symbol '%s' doesn't exist" sym |> Error
        match ins with
        | B (SymUnresolved sym) -> lookup B sym
        | BL (SymUnresolved sym) -> lookup BL sym
        | _ -> Ok ins // Symbol is already resolved

    // Branch instructions have no suffixes
    let branchSpec = {
        InstrC = BRANCH
        Roots = ["B";"BL";"END"]
        Suffixes = [""]
    }

    /// Execute a Branch instruction
    let executeBranch dp (ins : Parse<Instr>) =
        let nxt = dp.Regs.[R15] + 4u // Address of the next instruction
        match condExecute ins dp with
        | false -> 
            dp
            |> updateReg R15 nxt
            |> Ok
        | true ->
            match ins.PInstr with
            | B (SymUnresolved _)
            | BL (SymUnresolved _) -> 
                failwithf "Trying to execute an unresolved label"
            | B (SymResolved addr) -> 
                dp 
                |> updateReg R15 addr
                |> Ok
            | BL (SymResolved addr) ->
                dp
                |> updateReg R15 addr
                |> updateReg R14 nxt
                |> Ok
            | END ->
                EXIT |> Error
                
    /// map of all possible opcodes recognised
    let opCodes = opCodeExpand branchSpec

    let parse (ls: LineData) : Result<Parse<Instr>,ErrInstr> option =
        let parse' (_instrC, (root,_suffix,pCond)) =
            let (WA la) = ls.LoadAddr // address this instruction is loaded into memory
            match ls.Operands with
            | LabelExpr (l, "") ->
                Ok { 
                    PInstr = 
                        match root with
                        | "B" -> B (SymUnresolved l)
                        | "BL" -> BL (SymUnresolved l)
                        | "END" -> END
                        | _ -> failwithf "Unexpected root in Misc.parse"
                    PLabel = ls.Label |> Option.map (fun lab -> lab, la) ; 
                    PSize = 4u; 
                    PCond = pCond 
                }
            | _ -> NoLabel |> Error

        Map.tryFind ls.OpCode opCodes // lookup opcode to see if it is known
        |> Option.map parse' // if unknown keep none, if known parse it.

    /// Parse Active Pattern used by top-level code
    let (|IMatch|_|) = parse

    