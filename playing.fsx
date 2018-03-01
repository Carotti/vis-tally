
let qp thing = thing |> printfn "%A"
open System

let qpl lst = lst |> List.map (qp)



let inline (>>>>) shift num = (>>>) num shift    
let inline (<<<<) shift num = (<<<) num shift
let getBit n (value:uint32) =
    value
    |> (<<<<) (31-n)
    |> (>>>>) (31)
 

// getBit 4 0b01010u |> qp
// getBit 3 0b01010u |> qp
// getBit 2 0b01010u |> qp
// getBit 1 0b01010u |> qp
// getBit 0 0b01010u |> qp
// getBit 0 0b00001u |> qp

let a = 0xffffffffffffffffUL
a |> printfn "%x"
a |> uint32 |> printfn "%x"

