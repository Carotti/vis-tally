
let a = 13
let b = 147

let myFormatter x =
    match x with
    | 13 -> "Unlucky!"
    | y -> string y

printfn "%s" (myFormatter a)
printfn "%s" (myFormatter b)