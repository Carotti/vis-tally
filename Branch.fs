//////////////////////////////////////////////////////////////////////////////////////////
//                   Sample (skeleton) instruction implementation modules
//////////////////////////////////////////////////////////////////////////////////////////

module Branch
    open CommonData
    open CommonLex
    open Expressions
    open Execution

    type Instr =
        | B of string
        | BL of string
        | END

    type ErrRunTime =
        | Err of string
        | EXIT // Used to exit execution of the simulation

    /// parse error (dummy, but will do)
    type ErrInstr = 
        | ParseError of string
        | RuntimeError of ErrRunTime

    // Branch instructions have no suffixes
    let branchSpec = {
        InstrC = BRANCH
        Roots = ["B";"BL";"END"]
        Suffixes = [""]
    }

    /// Execute a Branch instruction
    let execute dp (ins : Parse<Instr>) (syms : SymbolTable) =
        let nxt = dp.Regs.[R15] + 4u // Address of the next instruction
        match condExecute ins dp with
        | false -> 
            dp
            |> updateReg R15 nxt
            |> Ok
        | true ->
            match ins.PInstr with
            | B label -> 
                dp 
                |> updateReg R15 syms.[label] 
                |> Ok
            | BL label ->
                dp
                |> updateReg R15 syms.[label]
                |> updateReg R14 5u
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
                        | "B" -> B l
                        | "BL" -> BL l
                        | "END" -> END
                        | _ -> failwithf "Unexpected root in Misc.parse"
                    PLabel = ls.Label |> Option.map (fun lab -> lab, la) ; 
                    PSize = 4u; 
                    PCond = pCond 
                }
            | _ -> sprintf "Expected a label at '%s'" ls.Operands |> ParseError |> Error

        Map.tryFind ls.OpCode opCodes // lookup opcode to see if it is known
        |> Option.map parse' // if unknown keep none, if known parse it.

    /// Parse Active Pattern used by top-level code
    let (|IMatch|_|) = parse
