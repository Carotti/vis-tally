module Helpers
//     open CommonData
//     open CommonLex


//     let regsExist rLst = 
//         rLst 
//         |> List.fold (fun b r -> b && (Map.containsKey r regNames)) true
    
//     let checkValid opList =
//         match opList with
//         | [dest; op1; _] when (regsExist [dest; op1]) -> true // ASR, LSL, LSR ROR
//         | [dest; op1] when (regsExist [dest; op1]) -> true // RRX
//         | _ -> false

//     let splitOps =                          
//         let nospace = ls.Operands.Replace(" ", "")                                    
//         nospace.Split([|','|])              
//         |> Array.map (fun r -> r.ToUpper())    
//         |> List.ofArray