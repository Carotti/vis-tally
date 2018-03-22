
////////////////////////////////////////////////////////////////////////////////////
//      Code defined at top level after the instruction processing modules
////////////////////////////////////////////////////////////////////////////////////
module CommonTop
    open CommonLex
    open CommonData

    open Errors
    open ErrorMessages
    open Branch

    /// allows different modules to return different instruction types
    type Instr =
        | IMEM of Memory.Instr
        | IDP of DP.Instr
        | IMISC of Misc.Instr
        | IBRANCH of Branch.Instr
        | EMPTY
    
    /// allows different modules to return different error info
    /// by default all return string so this is not needed
    type ErrInstr =
        | ERRIMEM of ErrParse
        | ERRIDP of ErrParse
        | ERRMISC of ErrParse
        | ERRBRANCH of ErrParse
        | ERRTOPLEVEL of ErrParse
            
    let Blank = {
        PCond = Cal;
        PInstr = EMPTY;
        PLabel = None;
        PSize = 0u;
    }

    /// Note that Instr in Mem and DP modules is NOT same as Instr in this module
    /// Instr here is all possible isntruction values combines with a D.U.
    /// that tags the Instruction class
    /// Similarly ErrParse
    /// Similarly IMatch here is combination of module IMatches
    let IMatch (ld: LineData) : Result<Parse<Instr>,ErrInstr> option =
        let pConv fr fe p = pResultInstrMap fr fe p |> Some
        match ld with
        | Memory.IMatch pa -> pConv IMEM ERRIMEM pa
        | DP.IMatch pa -> pConv IDP ERRIDP pa
        | Misc.IMatch pa -> pConv IMISC ERRMISC pa
        | Branch.IMatch pa -> pConv IBRANCH ERRBRANCH pa
        | _ -> None
    
    

    type CondInstr = Condition * Instr

    let parseLine (symtab: SymbolTable option) (loadAddr: WAddr) (asmLine:string) =
        let checkBlankLine = function
            | "" -> true
            | _ -> false
        /// put parameters into a LineData record
        let makeLineData opcode operands = {
            OpCode=opcode
            Operands=String.concat "" operands
            Label=None
            LoadAddr = loadAddr
            SymTab = symtab
        }
        /// remove comments from string
        let removeComment (txt:string) =
            txt.Split(';')
            |> function 
                | [|x|] -> x 
                | [||] -> "" 
                | lineWithComment -> lineWithComment.[0]
        /// split line on whitespace into an array
        let splitIntoWords ( line:string ) =
            line.Split( ([||] : char array), 
                System.StringSplitOptions.RemoveEmptyEntries)
        /// try to parse 1st word, or 2nd word, as opcode
        /// If 2nd word is opcode 1st word must be label
        let matchLine words =
            let pNoLabel =
                match words with
                | opc :: operands -> 
                    makeLineData opc operands 
                    |> IMatch
                | _ -> None
            match pNoLabel, words with
            | Some pa, _ -> pa
            | None, label :: opc :: operands -> 
                match { makeLineData opc operands 
                        with Label=Some label} 
                      |> IMatch with
                | None -> 
                    (opc, notImplementedInsEM)
                    ||> makeError
                    |> ``Unimplemented instruction``
                    |> ERRTOPLEVEL
                    |> Error
                | Some pa -> pa
            | None, [label] -> {Blank with PLabel = Some (label, 0u)} |> Ok
            | None, [] -> Blank |> Ok
            // | _ ->
            //     (List.reduce (+) words, notImplementedInsEM)
            //     ||> makeError
            //     |> ``Unimplemented instruction``
            //     |> ERRTOPLEVEL
            //     |> Error

        asmLine
        |> removeComment
        |> splitIntoWords
        |> Array.toList
        |> matchLine