//////////////////////////////////////////////////////////////////////////////////////////
//                   Sample (skeleton) instruction implementation modules
//////////////////////////////////////////////////////////////////////////////////////////

module Misc
    open CommonData
    open CommonLex
    open Expressions

    type DCDInstr = {label : string ; values : uint32}
    type DCBInstr = {label : string ; values : byte}

    type FILLValSize = One | Two | Four
    type FILLVal = {value : uint32 ; valueSize : FILLValSize Option}
    type FILLInstr = {label : string Option; value : FILLVal Option}

    /// instruction (dummy: must change)
    type Instr =
    | DCD of DCDInstr
    | DCB of DCBInstr
    | FILL of FILLInstr
    | EQU // No information about EQU is needed

    /// parse error (dummy, but will do)
    type ErrInstr = string

    /// These opCodes do not have conditions or suffixes
    let opCodes = ["DCD";"DCB";"EQU";"FILL"]

    let parse (ls: LineData) : Result<Parse<Instr>,string> option =
        let (WA la) = ls.LoadAddr // address this instruction is loaded into memory
        match ls.OpCode with
        | "DCD" -> None
        | "DCB" -> None
        | "EQU" -> None
        | "FILL" -> None
        | _ -> None

    /// Parse Active Pattern used by top-level code
    let (|IMatch|_|) = parse
