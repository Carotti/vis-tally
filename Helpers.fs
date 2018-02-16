module Helpers
    open CommonData
    open CommonLex
    open System.Text.RegularExpressions

    let (|ParseRegex|_|) (regex: string) (str: string) =
       let m = Regex("^" + regex + "[\\s]*" + "$").Match(str)
       if m.Success
       then Some (m.Groups.[1].Value)
       else None

    let regValid r =
        Map.containsKey r regNames

    let regsValid rLst = 
        rLst 
        |> List.fold (fun b r -> b && (regValid r)) true

    let uppercase (x: string) = x.ToUpper()

    let splitAny (str: string) char =
        let nospace = str.Replace(" ", "")                                    
        nospace.Split([|char|])              
        |> Array.map uppercase    
        |> List.ofArray

    
//     let checkValid opList =
//         match opList with
//         | [dest; op1; _] when (regsExist [dest; op1]) -> true // ASR, LSL, LSR ROR
//         | [dest; op1] when (regsExist [dest; op1]) -> true // RRX
//         | _ -> false
