module BranchTest
    open TestTop
    open TestFormats
    open Branch

    open Execution
    open CommonLex
    open CommonTop
    open CommonData

    open Expecto

    /// Hacky but used to downcast from the toplevel parse function
    /// to a BRANCH instruction
    let branchDowncast (ins : Parse<CommonTop.Instr>) : Parse<Branch.Instr> =
        match ins.PInstr with
        | IBRANCH bIns -> 
            {
                PInstr = bIns
                PLabel = ins.PLabel
                PSize = ins.PSize
                PCond = ins.PCond
            }
        | _ -> failwithf "Invalid downcast to BRANCH"

    let produceBranch txt = 
        match parseTop txt with
        | Ok x -> x
        | f -> failwithf "Invalid Branch production %A" f

    let runBranch txt dpStart = 
        produceBranch txt
        |> branchDowncast
        |> resolve ts 
        |> function
            | Ok x -> execute dpStart x
            | _ -> failwithf "Invalid symbol in branch being run"

    // Easy access for unit tests, only Branch instructions...
    let idp : DataPath<Branch.Instr> = initialDp ()

    // Expected out if condition false executed
    let idpF = {idp with Regs = (Map.add R15 4u idp.Regs)}

    // Expected out if condition true executed
    let idpT = {idp with Regs = (Map.add R15 4444u idp.Regs)}

    /// Construct a datapath with particular flags
    let flags n c z v dp = 
        {dp with Fl = {N = n; C = c; Z = z; V = v}}

    /// Construct a unit tests for branch instructions
    let unitTestB name txt dpStart dpExp =
        unitTest name txt dpExp (runBranch txt dpStart)

    /// Construct tests for either B or BL depending on the op given
    let makeBTests op =
        // BL sets the link register if condition true executed
        let idpT' =
            match op with
            | "B" -> idpT
            | "BL" -> {idpT with Regs = (Map.add R14 4u idpT.Regs)}
            | _ -> failwithf "CannotmakeBTests with anything other than B or BL"

        let condTest cond n c z v ex =
            let res = 
                match ex with
                | true -> Ok (flags n c z v idpT')
                | false -> Ok (flags n c z v idpF)
            unitTestB <|
                op + cond + " " + (sprintf "%A %A %A %A %A" n c z v ex) <|
                op + cond + " branchTarget" <|
                flags n c z v idp <|
                res

        testList op [
                condTest "" false false false false true
                condTest "AL" false false false false true
                condTest "NV" false false false false false
                condTest "EQ" false false true false true
                condTest "EQ" false false true true true
                condTest "EQ" false false false false false
                condTest "EQ" false false false true false
                condTest "NE" false false true false false
                condTest "NE" false false true true false
                condTest "NE" false false false false true
                condTest "NE" false false false true true
                condTest "HS" false false false false false
                condTest "HS" true false false true false
                condTest "HS" false true false false true
                condTest "HS" true true false true true
                condTest "LO" false false false false true
                condTest "LO" true false false true true
                condTest "LO" false true false false false
                condTest "LO" true true false true false
                condTest "MI" false false false false false
                condTest "MI" false false true false false
                condTest "MI" true false false false true
                condTest "MI" true true false false true
                condTest "PL" false false false false true
                condTest "PL" false false true false true
                condTest "PL" true false false false false
                condTest "PL" true true false false false
                condTest "VS" false false false false false
                condTest "VS" true true false false false
                condTest "VS" false false false true true
                condTest "VS" true false false true true
                condTest "VC" false false false false true
                condTest "VC" true true false false true
                condTest "VC" false false false true false
                condTest "VC" true false false true false
                condTest "HI" false true true true false
                condTest "HI" false false false false false
                condTest "HI" true true false false true
                condTest "HI" false true false false true
                condTest "LS" false true true true true
                condTest "LS" false false false false true
                condTest "LS" true true false false false
                condTest "LS" false true false false false
                condTest "GE" false false true true false
                condTest "GE" true true false false false
                condTest "GE" true false false true true
                condTest "GE" false true true false true
                condTest "LT" false false true true true
                condTest "LT" true true false false true
                condTest "LT" true false false true false
                condTest "LT" false true true false false
                condTest "GT" false false true false false
                condTest "GT" false false false true false
                condTest "GT" false true false false true
                condTest "GT" true false false true true
                condTest "LE" false false true false true
                condTest "LE" false false false true true
                condTest "LE" false true false false false
                condTest "LE" true false false true false
            ]

    [<Tests>]
    let branchTests = 
        testList "Branch Tests" [
            testList "END" [
                unitTestB <|
                    "Always end" <|
                    "END" <|
                    idp <|
                    (Error EXIT)
                unitTestB <|
                    "Always end lowercase" <|
                    "end" <|
                    idp <|
                    (Error EXIT)
                unitTestB <|
                    "Always end mixcase" <|
                    "eND" <|
                    idp <|
                    (Error EXIT)
                unitTestB <|
                    "Always end suffix" <|
                    "ENDAL" <|
                    idp <|
                    (Error EXIT)
                unitTestB <|
                    "Never end suffix" <|
                    "ENDNV" <|
                    idp <|
                    Ok idpF
            ]
            makeBTests "B"
            makeBTests "BL"
        ]
