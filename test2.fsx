open System
open System.Text.RegularExpressions

let qp item = printfn "%A" item
let qpl lst = List.map (qp) lst

// let regValid r =
//     Map.containsKey r regNames

// let regsValid rLst = 
//     rLst 
//     |> List.fold (fun b r -> b && (regValid r)) true

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

let (|ParseRegex2|_|) (regex: string) (str: string) =
   let m = Regex("^" + regex + "[\\s]*" + "$").Match(str)
   if m.Success
   then Some (m.Groups.[1].Value, m.Groups.[2].Value)
   else None

// let (|RegListMatch|_|) str =
//     match str with
//     | ParseRegex "(([rR][0-9]{1,2})-([rR][0-9]{1,2}))" listReg -> 
//         qp listReg
//         listReg |> Some
//     | ParseRegex "([rR][0-9]{1,2})\}" lastReg -> 
//         qp lastReg
//         lastReg |> Some
//     | ParseRegex "([rR][0-9]{1,2})" listReg -> 
//         qp listReg
//         listReg |> Some
//     | _ -> None

let splitMult = splitAny "r0, {r1-r3, r4, r5}" '{'

qp splitMult


let (|RegListExpand|_|) str =
    match str with
    | ParseRegex2 "[rR]([0-9]{1,2})-[rR]([0-9]{1,2})" (low, high) -> (low, high) |> Some
    | _ -> None

let (|RegListMatch|_|) str =
    let createList n = 
        match n with
        | RegListExpand (low, high) -> 
            let makeReg = (string >> (+) "R")
            let ilow = int low
            let ihigh = int high
            let fullRegList = List.map (fun r -> r |> makeReg) [ilow..ihigh]
            fullRegList |> Some
        | _ -> None
    
    match str with
    | ParseRegex "(([rR][0-9]{1,2})-([rR][0-9]{1,2}))" listReg -> createList listReg
    | ParseRegex "([rR][0-9]{1,2})!" bangReg -> [bangReg] |> Some
    | ParseRegex "([rR][0-9]{1,2})" reg -> [reg] |> Some
    | _ -> None

let ops = 
    match splitMult with
    | [rn; rlst] ->
        let splitList = splitAny (rlst.Replace("}", "")) ','
        let firstReg = rn.Replace(",", "")
        let matcher x =
            match x with
            | RegListMatch x -> x
            | _ -> ["poop"]

        let rec doAll f list =
            match list with
            | [] -> []
            | head :: tail -> f head :: doAll f tail
        let fullValues = doAll matcher splitList 
        let doneAH = List.concat fullValues
        qp doneAH
    | _ -> "Gah" |> qp

// ops |> qp
let qp item = printfn "%A" item
let lessThan32 rn = (fun x -> x % 32) rn

lessThan32 47 |> qp




let rec makeOffsetList inlst outlist n = 
    match inlst with
    | _ :: tail -> (n + 4) |> makeOffsetList tail ([n] :: outlist)
    | [] -> outlist


makeList [0..5] 0 [] |> qp