//////////////////////////////////////////////////////////////////////////////////////////
//                   Sample (skeleton) instruction implementation modules
//////////////////////////////////////////////////////////////////////////////////////////

module Misc
    open CommonData
    open CommonLex
    open Expressions

    // Both DCD and DCB don't have their expressions 
    // evaluated until simulation
    type DataInstr = {label : string ; values : Expression list}

    type FILLValSize = One | Two | Four
    type FILLVal = {value : uint32 ; valueSize : FILLValSize Option}
    type FILLInstr = {label : string Option; value : FILLVal Option}

    /// instruction (dummy: must change)
    type Instr =
    | DCD of DataInstr
    | DCB of DataInstr
    | FILL of FILLInstr
    | EQU // No information about EQU is needed

    /// parse error (dummy, but will do)
    type ErrInstr = string

    /// These opCodes do not have conditions or suffixes
    let opCodes = ["DCD";"DCB";"EQU";"FILL"]

            /// Check if x can be rotated right in a 32 bit word
    let rotateOk x =
        let rec rotateOk' r =
            let rotRight (x : uint32) n = (x >>> n) ||| (x <<< (32 - n))
            match rotRight x r < 256u, r > 30 with
            | _, true -> false
            | true, false -> true
            | false, false -> rotateOk' (r + 2)
        rotateOk' 0

    let parse (ls: LineData) : Result<Parse<Instr>,string> option =
        let (WA la) = ls.LoadAddr // address this instruction is loaded into memory
        
        /// Check if x can be rotated right in a 32 bit word
        let rotateOk x =
            let rec validEncoding' r =
                let rotRight (x : uint32) n = (x >>> n) ||| (x <<< (32 - n))
                match rotRight x r < 256u, r > 30 with
                | _, true -> sprintf "Cannot rotate '%d' within a 32 bit word" x |> Error
                | true, false -> Ok ()
                | false, false -> validEncoding' (r + 2)
            validEncoding' 0

        /// Check if x is small enough to be a byte
        let byteOk x =
            match (x |> byte |> uint32) = x with
            | true -> Ok ()
            | false -> sprintf "Cannot fit '%d' into 1 byte of memory" x |> Error

        match ls.OpCode with
        | "DCD" -> None
        | "DCB" -> None
        | "EQU" -> None
        | "FILL" -> None
        | _ -> None

    /// Parse Active Pattern used by top-level code
    let (|IMatch|_|) = parse
