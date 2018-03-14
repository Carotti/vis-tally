 module MiscExecution
    open Misc
    open Execution
    open Expressions
    open CommonData
    open Errors

    /// Execute a MISC instruction against the datapath
    /// mem is where to start placing in memory
    /// Return new mem which is where the next instruction
    /// would begin placing in memory
    // let executeMisc (dp, mem) ins : (Result<DataPath<CommonTop.Instr>,ErrExe>) =
    let executeMisc ins mem  dp : (Result<DataPath<CommonTop.Instr>,ErrExe>) =
        let expectResolved exp =
            match exp with
            | ExpResolved data -> data
            | _ -> failwithf "Trying to execute unresolved data"

        let executeDCD lst =
            let foldDCD (dp', mem') exp =
                match exp with
                | ExpResolved data -> (updateMemData data (alignAddress mem') dp', mem' + 4u)
                | _ -> failwithf "Trying to execute unresolved DCD instruction"
            List.fold foldDCD (dp, mem) lst

        let executeDCB lst = 
            let foldDCB (dp', mem') exp =
                match exp with
                | ExpResolvedByte data -> (updateMemByte data mem' dp', mem' + 1u)
                | _ -> failwithf "Trying to execute unresolved byte DCB instructon"
            List.fold foldDCB (dp, mem) lst

        let executeFILL fIns =
            let numBytes = expectResolved fIns.numBytes
            let fillVal = expectResolved fIns.value
            // Currently assume that valueSize is always 1
            let rec doFillByte mem' dp' =
                match mem' = mem + numBytes with
                | false -> doFillByte (mem' + 1u) (updateMemByte (byte fillVal) mem' dp')
                | true -> (dp', mem')
            doFillByte mem dp

        match ins with
        | DCD lst -> executeDCD lst |> fst |> Ok
        | DCB lst -> executeDCB lst |> fst |> Ok
        | FILL fIns -> executeFILL fIns |> fst |> Ok
        | EQU _ -> failwithf "Can't execute EQU"