open System
open System.Text.RegularExpressions

let qp item = printfn "%A" item



// let (|LiteralMatch|_|) str =
//     let uintOption = uint32 >> Some
//     match str with 
//     | ParseRegex "#(0[xX][0-9a-fA-F]+)" hex -> hex |> uintOption
//     | ParseRegex "#(0&[0-9a-fA-F]+)" hex -> hex |> uintOption
//     | ParseRegex "#(0[bB][0-1]+)" bin -> bin |> uintOption
//     | ParseRegex "#([0-9]+)" dec -> dec |> uintOption
//     | ParseRegex "([rR][0-9]+)" reg -> reg |> Some
//     | _ -> None // Literal was not valid

// match "#0b234" with
// | LiteralMatch out -> out |> printfn "%A"
// | _ -> "nope" |> printfn "%A"


// LDR ops
let str1 = "r0, [r1, #4]" //["R0"; "[R1"; "#4]"] -> R0  [R1  #4]
let str2 = "r0, [r1]" //["R0"; "[R1]"] -> R0  [R1]
let str3 = "r0, [r1], #4" //["R0"; "[R1]"; "#4"] -> R0  [R1]  #4
let str4 = "r0, [r1, #4]!" //["R0"; "[R1"; "#4]!"] -> R0  [R1  #4]!

let str5 = "r0, [r1, r2, lsl r3]"
                        
let nospace = str5.Replace(" ", "")                                    
nospace.Split([|','|])              
|> Array.map (fun r -> r.ToUpper())    
|> List.ofArray |> qp

let (|ParseRegex|_|) regex str =
   let m = Regex("^" + regex + "[\\s]*" + "$").Match(str)
   if m.Success
   then Some (m.Groups.[1].Value)
   else None

let (|MemMatch|_|) str =
    match str with 
    | ParseRegex "\[([rR][0-9]{1,2})\]" pre -> pre |> Some
    | ParseRegex "\[([rR][0-9]{1,2})" pre -> pre |> Some
    | ParseRegex "([rR][0-9]{1,2})\]" pre -> pre |> Some
    | _ -> "poop" |> Some

match "r12]" with
| MemMatch out -> out |> qp
| _ -> "nope" |> qp