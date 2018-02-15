

module Memory
    open CommonData
    open CommonLex
    open Expecto

    type OffsetType =
        | ImmOffset of uint32
        | Empty

    [<Struct>]
    type Address = {addrReg: RName; offset: OffsetType}
    
    type PostIndex =
        | N of uint32
        | Empty

    [<Struct>]
    type InstrMem = {valReg: RName; addr: Address; postOffset: PostIndex}

    type Instr = 
        | LDR of InstrMem

    /// parse error (dummy, but will do)
    type ErrInstr = string

    let memSpec = {
        InstrC = MEM
        Roots = ["LDR";"STR";"STM";"LDM"]
        Suffixes = [""; "B"]
    }

    /// map of all possible opcodes recognised
    let opCodes = opCodeExpand memSpec

    /// main function to parse a line of assembler
    /// ls contains the line input
    /// and other state needed to generate output
    /// the result is None if the opcode does not match
    /// otherwise it is Ok Parse or Error (parse error string)
    let parse (ls: LineData) : Result<Parse<Instr>,string> option =
        let parseLoad suffix pCond : Result<Parse<Instr>,string> = 
            Ok { 
                PInstr={MemDummy=()};
                PLabel = None ; 
                PSize = 4u; 
                PCond = pCond 
              }

    
        let listOfInstr = 
            Map.ofList [
                "LDR", parseLoad;
            ]

        let parse' (instrC, (root,suffix,pCond)) =
            listOfInstr.[root] suffix pCond

        Map.tryFind ls.OpCode opCodes
        |> Option.map parse'



    /// Parse Active Pattern used by top-level code
    let (|IMatch|_|)  = parse


