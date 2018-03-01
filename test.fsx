// open System
// open System.Text.RegularExpressions
// open Helpers
// let str1 = "r0, {r0, r2, r3}" //["R0"; "[R1"; "#4]"] -> R0  [R1  #4]
// let str2 = "r0, [r1]" //["R0"; "[R1]"] -> R0  [R1]
// let str3 = "r0, [r1], #4" //["R0"; "[R1]"; "#4"] -> R0  [R1]  #4
// let str4 = "r0, [r1, #4]!" //["R0"; "[R1"; "#4]!"] -> R0  [R1  #4]!

// let str5 = "r0, [r1, r2, lsl r3]"
                        
// let splitAny (str: string) char =
//     let nospace = str.Replace(" ", "")                                    
//     nospace.Split([|char|])              
//     |> Array.map (fun r -> r.ToUpper())    
//     |> List.ofArray

// let splitMult = splitAny str1 '{'

// splitMult |> qp

// let (|ParseRegex|_|) regex str =
//    let m = Regex("^" + regex + "[\\s]*" + "$").Match(str)
//    if m.Success
//    then Some (m.Groups.[1].Value)
//    else None

// let (|MemMatch|_|) str =
//     match str with 
//     // [r12]
//     | ParseRegex "\[([rR][0-9]{1,2})\]" pre -> pre |> Some
//     // [r12
//     | ParseRegex "\[([rR][0-9]{1,2})" pre -> pre |> Some
//     // r12]
//     | ParseRegex "([rR][0-9]{1,2})\]" pre -> pre |> Some

//     | _ -> "poop" |> Some

// match "r12]" with
// | MemMatch out -> out |> qp
// | _ -> "nope" |> qp


//             let checkValid opList =
//                 match opList with
//                 | [reg; addr; _] when (regsValid [reg; addr]) -> true // e.g. LDR R0, [R1], #4
//                 | [reg; addr] when (regsValid [reg; addr]) -> true // e.g. LDR R0, [R1]
//                 | _ -> false
// let checkValid opList =
//     match opList with
//     | h1 :: h2 :: tail when (regsValid [h1 ; h2] -> true) 
//     | _ -> false


let qp item = printfn "%A" item

let rec makeOffsetList inlst outlist incr start = 
    match inlst with
    | _ :: tail -> (start + incr) |> makeOffsetList tail (start :: outlist) incr
    | [] -> outlist

let lst = ["R1";"R2";"R3";"R4"]

// let offsetList start = 
//     let lst =
//         match operands.suff with           
//         | Some IA -> 
//             start
//             |> makeOffsetList rl [] 4
//         | Some IB -> 
//             (start + 4)
//             |> makeOffsetList rl [] 4
//         | Some DA -> 
//             start
//             |> makeOffsetList rl [] -4
//         | Some DB ->
//             (start - 4)
//             |> makeOffsetList rl [] -4
//         | _ -> failwithf "Isnt a suffix"
//     List.map (fun el -> el |> uint32) lst

let offsetList = makeOffsetList lst [] -4 100
qp offsetList