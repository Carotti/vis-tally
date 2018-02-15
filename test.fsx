open System
open System.Text.RegularExpressions
open System.Linq
open System.Linq

let qp item = printfn "%A" item

let (|ParseRegex|_|) regex str =
   let m = Regex("^" + regex + "[\\s]*" + "$").Match(str)
   if m.Success
   then Some (m.Groups.[1].Value)
   else None

let (|LiteralMatch|_|) str =
    let uintOption = uint32 >> Some
    match str with 
    | ParseRegex "#(0[xX][0-9a-fA-F]+)" hex -> hex |> uintOption
    | ParseRegex "#(0&[0-9a-fA-F]+)" hex -> hex |> uintOption
    | ParseRegex "#(0[bB][0-1]+)" bin -> bin |> uintOption
    | ParseRegex "#([0-9]+)" dec -> dec |> uintOption
    | ParseRegex "([rR][0-9]+)" reg -> reg |> Some
    | _ -> None // Literal was not valid

match "#0b234" with
| LiteralMatch out -> out |> printfn "%A"
| _ -> "nope" |> printfn "%A"


let str = "r0, r1"
                        
let nospace = str.Replace(" ", "")                                    
nospace.Split([|','|])              
|> Array.map (fun r -> r.ToUpper())    
|> List.ofArray |> qp
