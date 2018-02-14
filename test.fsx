let rec bin (a : uint32) =
    let bit = string (a % 2u)
    match a with 
    | 0u | 1u -> bit
    | _ -> bin (a / 2u) + bit

printfn "%A" (bin 2u)