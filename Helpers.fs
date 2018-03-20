module Helpers
    open CommonData    
    open System.Text.RegularExpressions
    open Expecto
    
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
 
    /// Very simple property based tests for functions which I use a lot.
    /// Some truly are trivial! Huzzah!
    [<Tests>]
    let helperTests =
        /// Test to see if my valid register function is consistent
        let validRegisterCheck (reg: string) ans =
            match ans with
            | x when (x && (regValid reg)) -> true
            | x when (not x && not (regValid reg)) -> true
            | _ -> false

        /// yes really!?, one can never be too sure.
        let uppercaseCheck inp ans res = 
            match res with 
            | x when ((ans = (uppercase inp)) && x) -> true
            | x when ((ans <> (uppercase inp)) && not x) -> true
            | _ -> false
        
        /// Checks splitting and input string with different characters
        let splitAnyCheck (str: string) (ch: char) ans res = 
            let combined = (splitAny str ch) |> List.fold (+) ""
            match res with
            | x when ((combined = (uppercase ans)) && x) -> true
            | x when ((combined <> (uppercase ans)) && not x) -> true
            | _ -> false
       

        testList "Helpers Tests" [
            testList "Checking regValid fn" [
                testProperty "Valid Register 0" <| validRegisterCheck "R0" true
                testProperty "Invalid Register" <| validRegisterCheck "R17" false
                testProperty "Negative Num Register" <| validRegisterCheck "R-24" false
                testProperty "Valid Register 15" <| validRegisterCheck "R15" true
            ]

            testList "Checking uppercase fn" [
                testProperty "Single letter" <| uppercaseCheck "a" "A" true
                testProperty "Multiple letter" <| uppercaseCheck "ab c" "AB C" true
                testProperty "Uppercase fail" <| uppercaseCheck "a bc" "GH F" false
            ]

            testList "Checking splitAny fn" [
                testProperty "Reg Reg Num" <| splitAnyCheck "r0, r1, #4" ',' "r0r1#4" true
                testProperty "Load Multiple" <| splitAnyCheck "r0, {r3-r7}" '{' "r0,r3-r7}" true
                testProperty "Testing fail" <| splitAnyCheck "r1, [r2, #4]!" ',' "r1[r2,#4]!" false
            ]
        ]
            
        
