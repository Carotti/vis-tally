module Program
    open Integration
         
    [<EntryPoint>]
    let main argv =
        runCode argv.[0]
        0