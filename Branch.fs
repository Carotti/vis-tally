//////////////////////////////////////////////////////////////////////////////////////////
//                   Sample (skeleton) instruction implementation modules
//////////////////////////////////////////////////////////////////////////////////////////

module Branch
    open CommonData
    open CommonLex
    open Expressions

    type BranchType = 
        | B
        | BL

    type Instr =  {which : BranchType ; label : string}

    /// parse error (dummy, but will do)
    type ErrInstr = string

    // Branch instructions have no suffixes
    let branchSpec = {
        InstrC = BRANCH
        Roots = ["B";"BL"]
        Suffixes = [""]
    }

    let branchTypeMap = Map.ofList ["B", B; "BL", BL]

    /// map of all possible opcodes recognised
    let opCodes = opCodeExpand branchSpec

    let parse (ls: LineData) : Result<Parse<Instr>,string> option =
        let parse' (_instrC, (root,_suffix,pCond)) =
            let (WA la) = ls.LoadAddr // address this instruction is loaded into memory
            match ls.Operands with
            | LabelExpr (l, "") ->
                Ok { 
                    PInstr = {which = branchTypeMap.[root]; label = l}; 
                    PLabel = ls.Label |> Option.map (fun lab -> lab, la) ; 
                    PSize = 4u; 
                    PCond = pCond 
                }
            | x -> sprintf "Expected a label at '%s'" x |> Error

        Map.tryFind ls.OpCode opCodes // lookup opcode to see if it is known
        |> Option.map parse' // if unknown keep none, if known parse it.

    /// Parse Active Pattern used by top-level code
    let (|IMatch|_|) = parse
