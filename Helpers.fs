module Helpers
    open CommonData
    open System.Text.RegularExpressions
    open Expecto
    open System.Drawing
    
    let qp item = printfn "%A" item
    let qpl lst = List.map (qp) lst

    let (|ParseRegex|_|) (regex: string) (str: string) =
       let m = Regex("^" + regex + "[\\s]*" + "$").Match(str)
       if m.Success
       then Some (m.Groups.[1].Value)
       else None

    let regValid r =
        Map.containsKey r regNames

    let regsValid rLst = 
        rLst 
        |> List.fold (fun b r -> b && (regValid r)) true

    let uppercase (x: string) = x.ToUpper()

    let splitAny (str: string) char =
        let nospace = str.Replace(" ", "")                                    
        nospace.Split([|char|])              
        |> Array.map uppercase    
        |> List.ofArray

    let checkValid2 opList =
        match opList with
        | h1 :: h2 :: _ when (regsValid [h1 ; h2]) -> true 
        | _ -> false

    let setReg reg contents cpuData =
        let setter reg' old = 
            match reg' with
            | x when x = reg -> contents
            | _ -> old
        {cpuData with Regs = Map.map setter cpuData.Regs}
    
    let rec setMultRegs regLst contentsLst cpuData =
        match regLst, contentsLst with
        | rhead :: rtail, chead :: ctail when (List.length regLst = List.length contentsLst) ->
            let newCpuData = setReg rhead chead cpuData
            setMultRegs rtail ctail newCpuData
        | [], [] -> cpuData
        | _ -> failwithf "Something went wrong with lists"
    
    let setMem mem contents cpuData =
        let setter mem' old =
            match mem' with
            | x when x = mem -> DataLoc contents
            | _ -> old
        {cpuData with MM = Map.map setter cpuData.MM}
    
    let rec setMultMem memLst contentsLst cpuData =
        match memLst, contentsLst with
        | mhead :: mtail, chead :: ctail when (List.length memLst = List.length contentsLst) ->
            let newCpuData = setMem mhead chead cpuData
            setMultMem mtail ctail newCpuData
        | [], [] -> cpuData
        | _ -> failwithf "Something went wrong with lists"

    [<Tests>]
    let helperTests =
        let validRegisterCheck (reg: string) ans =
            match ans with
            | x when (x && (regValid reg)) -> true
            | x when (not x && not (regValid reg)) -> true
            | _ -> false

        let uppercaseCheck inp ans res = 
            match res with 
            | x when ((ans = (uppercase inp)) && x) -> true
            | x when ((ans <> (uppercase inp)) && not x) -> true
            | _ -> false
        
        let splitAnyCheck (str: string) (ch: char) ans res = 
            let combined = (splitAny str ch) |> List.fold (+) ""
            match res with
            | x when ((combined = (uppercase ans)) && x) -> true
            | x when ((combined <> (uppercase ans)) && not x) -> true
            | _ -> false
        
        let setRegCheck reg value cpu =
            
            setReg 

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
            
        
