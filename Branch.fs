//////////////////////////////////////////////////////////////////////////////////////////
//                   Branch Instruction Implementation Module
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
        | NotInstrMem of uint32 // Address where there is no instruction
        | EXIT // Used to exit execution of the simulation

    type ErrInstr = 
        | NoLabel

    /// Resolve the symbols for an instruction which requires it
    let resolvePInstr (syms : SymbolTable) ins =
        let lookup which sym =
            match syms.ContainsKey sym with
            | true -> which (SymResolved syms.[sym]) |> Ok
            | false -> sprintf "Symbol '%s' doesn't exist" sym |> Error
        match ins with
        | B (SymUnresolved sym) -> lookup B sym
        | BL (SymUnresolved sym) -> lookup BL sym
        | _ -> Ok ins // Symbol is already resolved

    let resolve (syms : SymbolTable) ins =
        resolvePInstr syms ins.PInstr
        |> Result.map (fun x -> {ins with PInstr = x})

    // Branch instructions have no suffixes
    let branchSpec = {
        InstrC = BRANCH
        Roots = ["B";"BL";"END"]
        Suffixes = [""]
    }

    /// Execute a Branch instruction
    let execute dp ins =
        let nxt = dp.Regs.[R15] + 4u // Address of the next instruction
        match condExecute ins dp with
        | false -> 
            dp
            |> updateReg nxt R15
            |> Ok
        | true ->
            match ins.PInstr with
            | B (SymUnresolved _)
            | BL (SymUnresolved _) -> 
                failwithf "Trying to execute an unresolved label"
            | B (SymResolved addr) -> 
                dp 
                |> updateReg addr R15
                |> Ok
            | BL (SymResolved addr) ->
                dp
                |> updateReg addr R15
                |> updateReg nxt R14
                |> Ok
            | END ->
                EXIT |> Error
                
    /// map of all possible opcodes recognised
    let opCodes = opCodeExpand branchSpec

    let parse (ls: LineData) : Result<Parse<Instr>,ErrInstr> option =

        let bindB t =
            match ls.Operands with
            | LabelExpr (l, "") -> Ok l
            | _ -> NoLabel |> Error
            |> Result.map (SymUnresolved >> t)

        let parse' (_instrC, ((root : string),_suffix,pCond)) =
            let (WA la) = ls.LoadAddr // address this instruction is loaded into memory
            match root.ToUpper () with
            | "B" -> bindB B
            | "BL" -> bindB BL
            | "END" -> Ok END
            | _ -> failwithf "Unexpected root in Misc.parse"
            |> Result.map (fun ins ->
                { 
                    PInstr = ins
                    PLabel = ls.Label |> Option.map (fun lab -> lab, la) ; 
                    PSize = 4u; 
                    PCond = pCond 
                })

        Map.tryFind (ls.OpCode.ToUpper ()) opCodes // lookup opcode to see if it is known
        |> Option.map parse' // if unknown keep none, if known parse it.

    /// Parse Active Pattern used by top-level code
    let (|IMatch|_|) = parse

    