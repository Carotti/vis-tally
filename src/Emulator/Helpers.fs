module Helpers
    open CommonData    
    open System.Text.RegularExpressions

    /// A function to check the validity of literals according to the ARM spec.
    let checkLiteral lit =
        let lst = [0..2..30] 
                    |> List.map (fun x -> rotRight lit x, x)
                    |> List.filter (fun (x, _) -> x < 256u)
        match lst with
        | [] ->
            let txt = lit |> string
            (txt, notValidLiteralEM)
            ||> makeError
            |> ``Invalid literal``
            |> Error
        | (x, r) :: _ -> Ok (byte x, r)
    
    /// Visuals apparent minimum data address may be useful?
    let minAddress = 0x100u
    /// Word Length 4 bytes
    let word = 0x4u
    /// The Old Chris QuickPrint classic from tick 1
    let qp item = printfn "%A" item
    let qpl lst = List.map (qp) lst

    /// Partial Active pattern for regexes. 
    /// Returns the 1st () element in the group
    let (|ParseRegex|_|) (regex: string) (str: string) =
       let m = Regex("^" + regex + "[\\s]*" + "$").Match(str)
       if m.Success
       then Some (m.Groups.[1].Value)
       else None

    /// Partial Active pattern for regexes. 
    /// Returns the 1st and 2nd () elements in the group
    let (|ParseRegex2|_|) (regex: string) (str: string) =
       let m = Regex("^" + regex + "[\\s]*" + "$").Match(str)
       if m.Success
       then Some (m.Groups.[1].Value, m.Groups.[2].Value)
       else None

    /// makes a reg
    let makeReg r = regNames.[r]
    /// Takes a number and converts it into a reg string
    let makeRegFn = (string >> (+) "R") // needed elsewhere

    /// makes a RName from a number
    let makeRegFromNum r =
        r |> makeRegFn |> makeReg

    /// Check if reg is valid by seeing if key is in map
    let regValid r =
        Map.containsKey r regNames

    /// Check all regs in lst are valid
    let regsValid rLst = 
        rLst 
        |> List.fold (fun b r -> b && (regValid r)) true

    let uppercase (x: string) = x.ToUpper()

    /// Split an input string at the provided charater.
    /// Return as a string list.
    let splitAny (str: string) char =
        let nospace = str.Replace(" ", "")                                    
        nospace.Split([|char|])              
        |> Array.map uppercase    
        |> List.ofArray

    let checkValid2 opList =
        match opList with
        | h1 :: h2 :: _ when (regsValid [h1 ; h2]) -> true 
        | _ -> false