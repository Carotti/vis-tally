module Program
    open Integration

    open CommonData
    open Helpers

    open Expecto

    /// Prettyprinter for printing cpu regs, flags and memory
    let prettyPrint cpuData =
        cpuData.Regs |> Map.toList |> List.map (fun (r, v) -> printfn "(%A : 0x%x)" r v) |> ignore
        cpuData.Fl |> qp |> ignore
        // cpuData.MM |> Map.toList |> qpl |> ignore
         
    [<EntryPoint>]
    let main argv =
        printf "~~~~~~~~~~~~~~~~~~~~~~~~~~~~~"
        match argv.[0] with
        | "tests" -> runTestsInAssembly Expecto.Tests.defaultConfig [||] |> ignore
        | _ ->
            match runCode argv.[0] with
            | Some dp -> prettyPrint dp
            | None -> ()
        0