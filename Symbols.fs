module Symbols
    open Helpers
    open Expressions
    open CommonLex

    // type ErrResolve =
    //     | InvalidByteExp of uint32
    //     | SymbolErrors of EvalErr list
    //     | InvalidFillMultiple // When numBytes is not a multiple of valueSize

    let symMap = Map.empty

    /// Resolve all MISC instructions which have unresolved `SymbolExp`s
    /// Any evaluation can fail with an undefined symbol, Error return is
    /// the first symbol which causes this
    // let resolve (syms : SymbolTable) ins = 
    //     /// Take a list of results and transform it to a Result of either the
    //     /// first error in the list or the Ok list if every element is Ok
    //     let lstResUnfold lst =
    //         let folder acc el =
    //             let binder acc' = Result.map (fun x -> x :: acc') el
    //             Result.bind binder acc
    //         List.fold folder (Ok []) lst
    //         |> Result.map List.rev
    //     let validByte x =
    //         match x with
    //         | ExpResolved exp when exp < 256u -> exp |> byte |> ExpResolvedByte |> Ok
    //         | ExpResolved exp -> InvalidByteExp exp |> Error
    //         | _ -> failwithf "Calling validByte on unresolved SymbolExp"
    //     match ins with
    //     | DCD lst -> 
    //         List.map (evalSymExp syms) lst
    //         |> lstResUnfold
    //         |> Result.map DCD
    //         |> Result.mapError SymbolErrors 
    //     | DCB lst -> 
    //         List.map ((evalSymExp syms) >> (Result.mapError SymbolErrors) >> (Result.bind validByte)) lst 
    //         |> lstResUnfold
    //         |> Result.map DCB
    //     | FILL fins ->
    //         let valBind x = 
    //             (evalSymExp syms) fins.value
    //             |> Result.map (fun v -> {x with value = v})
    //         (evalSymExp syms) fins.numBytes
    //         |> Result.map (fun n -> {fins with numBytes = n})
    //         |> Result.bind valBind
    //         |> Result.map FILL
    //         |> Result.mapError SymbolErrors 
    //     | EQU exp -> 
    //         (evalSymExp syms) exp
    //         |> Result.map EQU
    //         |> Result.mapError SymbolErrors 
