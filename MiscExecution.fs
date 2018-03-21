 module MiscExecution
    open Misc
    open Execution
    open Expressions
    open CommonData
    open CommonLex
    open CommonTop
    open Errors

    /// Execute a MISC instruction against the datapath
    /// mem is where to start placing in memory
    /// Return new mem which is where the next instruction
    /// would begin placing in memory
    // let executeMisc (dp, mem) ins : (Result<DataPath<CommonTop.Instr>,ErrExe>) =

    let executeMisc ins mem dp : (Result<DataPath<Parse<CommonTop.Instr>>*uint32,ErrExe>) =
        let expectResolved exp =
            match exp with
            | ExpResolved data ->
                data
                |> Ok
            | _ ->
                ("NEED SOMETHING HERE", " Trying to execute unresolved data.")
                ||> makeError 
                |> ``Run time error``
                |> Error

        let executeDCD lst =
            let foldDCD dpMem exp =
                match dpMem with
                | Ok (dp', mem') ->
                    match exp with
                    | ExpResolved data ->
                        updateMemData data (alignAddress mem') dp'
                        |> Result.map (function dp' -> (dp', mem' + 4u))
                    | _ ->
                        ("NEED SOMETHING HERE", " Trying to execute unresolved DCD instruction.")
                        ||> makeError 
                        |> ``Run time error``
                        |> Error
                | Error e -> Error e

            List.fold foldDCD (Ok (dp, mem)) lst

        let executeDCB lst = 
            let foldDCB dpMem exp =
                match dpMem with
                | Ok (dp', mem') ->
                    match exp with
                    | ExpResolvedByte data ->
                        updateMemByte data mem' dp'
                        |> Result.map (function dp' -> (dp', mem' + 1u))
                    | _ ->
                        ("NEED SOMETHING HERE", " Trying to execute unresolved byte DCB instructon.")
                        ||> makeError 
                        |> ``Run time error``
                        |> Error
                | Error e -> Error e

            List.fold foldDCB (Ok (dp, mem)) lst

        let executeFILL fIns =
            
            let rec doFillByte numBytes fillVal mem' (dp': DataPath<Parse<CommonTop.Instr>>) =
                match mem' = mem + numBytes with
                | false ->
                    Result.bind (doFillByte numBytes fillVal (mem' + 1u)) (updateMemByte (byte fillVal) mem' dp')
                | true ->
                    (dp', mem') |> Ok

            let numBytes = expectResolved fIns.numBytes
            let fillVal = expectResolved fIns.value
            
            let partialFillByte = combineErrorMapResult numBytes fillVal doFillByte 
            partialFillByte
            |> mapErrorApplyResult (mem |> Ok)
            |> mapErrorApplyResult (dp |> Ok)
            |> Result.bind (id)
            
        let executeADR aIns = 
            match aIns.exp with
                | ExpResolved x -> (setReg aIns.reg x dp, mem) |> Ok
                | _ -> failwithf "Trying to execute unresolved ADR Instruction"

        match ins with
        | DCD lst -> executeDCD lst
        | DCB lst -> executeDCB lst
        | FILL fIns -> executeFILL fIns
        | ADR aIns -> executeADR aIns
        | EQU _ -> failwithf "Can't execute EQU"