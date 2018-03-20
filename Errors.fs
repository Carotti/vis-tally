module Errors
    open ErrorMessages

    type ErrorBase =
        {
            errorTxt : string;
            errorMessage : string;
        }

    type ErrExe =
        | NotInstrMem of uint32 // Address where there is no instruction
        | ``Run time error`` of ErrorBase
        | ``Run time warning`` of ErrorBase
        | EXIT // Used to exit execution of the simulation

    let makeError txt message =
        {
            errorTxt = txt;
            errorMessage = message;
        }     

    /// A function to combine results or forward errors.
    let combineError (res1:Result<'T1,'E>) (res2:Result<'T2,'E>) : Result<'T1 * 'T2, 'E> =
        match res1, res2 with
        | Error e1, _ -> Error e1
        | _, Error e2 -> Error e2
        | Ok rt1, Ok rt2 -> Ok (rt1, rt2)

    /// A function that combines two results by applying a function on them as a pair, or forwards errors.
    let combineErrorMapResult (res1:Result<'T1,'E>) (res2:Result<'T2,'E>) (mapf:'T1 -> 'T2 -> 'T3) : Result<'T3,'E> =
        combineError res1 res2
        |> Result.map (fun (r1,r2) -> mapf r1 r2)
    
    /// A function that applies a possibly erroneous function to a possibly erroneous argument, or forwards errors.
    let applyResultMapError (res:Result<'T1->'T2,'E>) (arg:Result<'T1,'E>) =
        match arg, res with
        | Ok arg', Ok res' -> res' arg' |> Ok
        | _, Error e -> e |> Error
        | Error e, _ -> e |> Error

    let mapErrorApplyResult (arg:Result<'T1,'E>) (res:Result<'T1->'T2,'E>) =
        match arg, res with
        | Ok arg', Ok res' -> res' arg' |> Ok
        | _, Error e -> e |> Error
        | Error e, _ -> e |> Error

    let condenseResultList transform (lst: Result<'a,'b> list) : Result<'a list,'b> =
        let rec condenser' inlst outlst = 
            match inlst with
            | head :: tail ->
                match head with
                | Ok res -> condenser' tail ((transform res) :: outlst)
                | Error e -> e |> Error
            | [] -> List.rev outlst |> Ok
        condenser' lst [] 
