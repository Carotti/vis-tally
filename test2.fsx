open System
open System.Text.RegularExpressions
open System.Collections.Generic

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

let (|RegListMatch|_|) str =
    match str with
    | ParseRegex "(([rR][0-9]{1,2})-([rR][0-9]{1,2}))" listReg -> 
        qp listReg
        listReg |> Some
    | ParseRegex "([rR][0-9]{1,2})\}" lastReg -> 
        qp lastReg
        lastReg |> Some
    | ParseRegex "([rR][0-9]{1,2})" listReg -> 
        qp listReg
        listReg |> Some
    | _ -> None

let splitMult = splitAny "r0, {r1-r3}" '{'

qp splitMult

let ops = 
    match splitMult with
    | [rn; rlst] ->
        let list = splitAny (rlst.Replace("}", "")) ','
        let firstReg = rn.Replace(",", "")
        qp (firstReg :: list)
        let rec regListMatch lst = 
            match lst with
            | head :: tail when (regValid head) ->
                match head with
                | RegListMatch reg -> (Ok (regListMatch tail)) |> consMemMult reg tail
                | _ -> Error "Lord"
            | _ -> "God Almighty"
        regListMatch list
    | _ -> qp "Shit happened"

ops

let rec parseExprList txt =
        match txt with
        | Expr (exp, rst) ->
            match rst with
            | RegexPrefix "," (_, rst') -> 
                Result.map (fun lst -> (ExpUnresolved exp) :: lst) (parseExprList rst')
            | "" -> Ok [ExpUnresolved exp]
            | _ -> sprintf "Invalid Expression '%s'" txt |> Error
        | _ -> sprintf "Bad expression list '%s'" txt |> Error

let fullList (lst: int list) : string List = List.map (fun i -> i.ToString) lst
qp fullList
let makeReg = string >> ((+) "R")
let fullRegList = List.map (fun i -> i |> makeReg) [0..15]
qp fullRegList     
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
                