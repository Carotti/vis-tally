
let qp thing = thing |> printfn "%A"
open System

let qpl lst = lst |> List.map (qp)

1u <<< 31
|> qp

1 <<< 32
|> qp

1 <<< 33
|> qp