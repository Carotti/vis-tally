open System
open System.Text.RegularExpressions

[<Struct>]
    type RName = R0 | R1 | R2 | R3 | R4 | R5 | R6 | R7 
                 | R8 | R9 | R10 | R11 | R12 | R13 | R14 | R15
let regNames = 
    Map.ofList [ 
        "R0",R0 ; "R1",R1 ; "R2",R2 ; "R3",R3 ; "R4",R4 ; "R5",R5
        "R6",R6 ; "R7",R7 ; "R8",R8 ; "R9", R9 ; "R10",R10 ; "R11",R11 ; 
        "R12",R12 ; "R13",R13 ; "R14",R14 ; "R15",R15 ; 
        "PC",R15 ; "LR",R14 ; "SP",R13 
    ] 
let qp item = printfn "%A" item
let qpl lst = List.map (qp) lst

let regValid r =
    Map.containsKey r regNames

let regsValid rLst = 
    rLst 
    |> List.fold (fun b r -> b && (regValid r)) true

let splitAny (str: string) char =
    let nospace = str.Replace(" ", "")                                    
    nospace.Split([|char|])              
    |> Array.map (fun r -> r.ToUpper())    
    |> List.ofArray
 
let (|ParseRegex|_|) (regex: string) (str: string) =
   let m = Regex("^" + regex + "[\\s]*" + "$").Match(str)
   if m.Success
   then Some (m.Groups.[1].Value)
   else None

let (|RegListMatch|_|) str =
    match str with
    | ParseRegex "([rR][0-9]{1,2})\}" lastReg -> 
        qp lastReg
        lastReg |> Some
    | ParseRegex "([rR][0-9]{1,2})" listReg -> 
        qp listReg
        listReg |> Some
    | _ -> None

let splitMult = splitAny "r0, {r1, r2}" '{'

qp splitMult

let ops = 
    match splitMult with
    | [rn; rlst] ->
        let splitList = splitAny (rlst.Replace("}", "")) ','
        let firstReg = rn.Replace(",", "")
        qp firstReg
        qp splitList
        match firstReg :: splitList with
        | x when (regsValid x) -> qp x
        | _ -> qp "Kill me"
        
    | _ -> qp "fail"

ops
// let ops =
//     match splitMult with
//     | [reg; reglist] ->
//         let splitList = splitAny reglist ','
//         let rec matchList f lst = 
//             match lst with
//             | [] -> []
//             | head :: tail -> f head :: matchList f tail
//         let lst = matchList RegListMatch splitList
//         match [reg; lst] with
//         | [reg; lst] when (regsValid lst) ->
//             (Ok lst)
//             |> consMemMult reg lst
//         | _ -> Error "Fail asdfljh"
//     | _ -> Error "Aint matching fam"
                