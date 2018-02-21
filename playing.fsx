
open System
open System.Text.RegularExpressions
// open CommonData
// open CommonLex
// open CommonTop
let qp thing = thing |> printfn "%A"

let qpl lst = lst |> List.map (qp)

Map.containsKey
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

// let checkLiteral (lit:uint32) =
//     let rotMask n = (0xFFu >>> n) ||| (0xFFu <<< 32 - n)
//     [0..2..30] 
//     |> List.map (fun r -> rotMask r, r)
//     |> List.filter (fun (mask, _r) -> (mask &&& lit) = lit)
//     |> function
//     | hd :: _tl ->
//         let rotB = fst hd |> (&&&) lit
//         let B = (rotB <<< snd hd) ||| (rotB >>> 32 - snd hd)
//         Ok (B, snd hd)
//     | [] -> Error ("Not a valid literal. Rotation problems.")


// let (|ParseRegex|_|) regex txt =
//     let m = Regex.Match(txt, "^" + regex + "[\\s]*" + "$")
//     match m.Success with
//     | true -> Some (m.Groups.[1].Value)
//     | false -> None


// let (|LitMatch|_|) txt =
//     match txt with
//     | ParseRegex "#&([0-9a-fA-F]+)" num -> 
//         (uint32 ("0x" + num)) |> Some |> Option.map (checkLiteral)
//     | ParseRegex "#(0b[0-1]+)" num
//     | ParseRegex "#(0x[0-9a-fA-F]+)" num
//     | ParseRegex "#([0-9]+)" num -> num |> uint32 |> Some |> Option.map (checkLiteral)
//     | _ ->
//         // "Not a valid literal. Literal expression problems." |> qp 
//         None

// let numLst = [
//     "#0b101010";
//     "#104";
//     "#0xf000000f"
//     "#&ab123ef";
//     "#2301393";
//     "#0b423234";
// ]

// let matchLst lst =
//     lst
//     |> List.map (fun x ->   match x with 
//                             | LitMatch intVal -> (x, intVal) |> qp
//                             | _ -> (x, "Not a valid literal. Literal expression problems.") |> qp
//                             )
    /// ARM register names
    /// NB R15 is the program counter as read
// [<Struct>]
// type RName = R0 | R1 | R2 | R3 | R4 | R5 | R6 | R7 
//              | R8 | R9 | R10 | R11 | R12 | R13 | R14 | R15

   

//     /// Map used to convert strings into RName values, 
//     /// includes register aliasses PC, LR, SP
// let regNames = 
//     Map.ofList [ 
//         "R0",R0 ; "R1",R1 ; "R2",R2 ; "R3",R3 ; "R4",R4 ; "R5",R5
//         "R6",R6 ; "R7",R7 ; "R8",R8 ; "R9", R9 ; "R10",R10 ; "R11",R11 ; 
//         "R12",R12 ; "R13",R13 ; "R14",R14 ; "R15",R15 ; 
//         "PC",R15 ; "LR",R14 ; "SP",R13 
//     ] 



// qp regs
// String.RegexMatch
