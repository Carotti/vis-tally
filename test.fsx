open System
open System.Text.RegularExpressions

let (|ParseRegex|_|) regex str =
   let m = Regex(regex).Match(str)
   if m.Success
   then Some (m.Groups.[1].Value)
   else None


let (|LiteralMatch|_|) str =
    let uintOption = uint32 >> Some
    match str with 
    | ParseRegex "^#(0[xX][0-9a-fA-F]+)$" hex -> hex |> uintOption
    | ParseRegex "^#(0&[0-9a-fA-F]+)$" hex -> hex |> uintOption
    | ParseRegex "^#(0[bB][0-1]+)$" bin -> bin |> uintOption
    | ParseRegex "^#([0-9]+)$" dec -> dec |> uintOption
    | _ -> None

match "#0b234" with
| LiteralMatch out -> out |> printfn "%A"
| _ -> "nope" |> printfn "%A"