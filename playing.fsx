
open System
open System.Text.RegularExpressions
let qp thing = thing |> printfn "%A"

let qpl lst = lst |> List.map (qp)


// let sentance = "hello my name is Chris"

// sentance.Split( [|" my "|], 
//     System.StringSplitOptions.RemoveEmptyEntries)
// |> Array.toList

// |> qpl

// let (|Even|Odd|) input =
//     if input % 2 = 0
//         then Even
//     else
//         Odd

// let TestNumber input =
//    match input with
//    | Even -> printfn "%d is even" input
//    | Odd -> printfn "%d is odd" input

// TestNumber 7
// TestNumber 11
// TestNumber 32

// let txt = "hello"
// txt.ToUpper() |> qp

// 

let checkLiteral (lit:uint32) =
    let rotMask n = (0xFFu >>> n) ||| (0xFFu <<< 32 - n)
    [0..2..30] 
    |> List.map (fun r -> rotMask r, r)
    |> List.filter (fun (mask, _r) -> (mask &&& lit) = lit)
    |> function
    | hd :: _tl ->
        let rotB = fst hd |> (&&&) lit
        let B = (rotB <<< snd hd) ||| (rotB >>> 32 - snd hd)
        Ok (B, snd hd)
    | [] -> Error ("Not a valid literal. Rotation problems.")


let (|ParseRegex|_|) regex txt =
    let m = Regex.Match(txt, "^" + regex + "[\\s]*" + "$")
    match m.Success with
    | true -> Some (m.Groups.[1].Value)
    | false -> None


let (|LitMatch|_|) txt =
    match txt with
    | ParseRegex "#&([0-9a-fA-F]+)" num -> 
        (uint32 ("0x" + num)) |> Some |> Option.map (checkLiteral)
    | ParseRegex "#(0b[0-1]+)" num
    | ParseRegex "#(0x[0-9a-fA-F]+)" num
    | ParseRegex "#([0-9]+)" num -> num |> uint32 |> Some |> Option.map (checkLiteral)
    | _ ->
        // "Not a valid literal. Literal expression problems." |> qp 
        None

let numLst = [
    "#0b101010";
    "#104";
    "#0xf000000f"
    "#&ab123ef";
    "#2301393";
    "#0b423234";
]

let matchLst lst =
    lst
    |> List.map (fun x ->   match x with 
                            | LitMatch intVal -> (x, intVal) |> qp
                            | _ -> (x, "Not a valid literal. Literal expression problems.") |> qp
                            )

matchLst numLst




// String.RegexMatch