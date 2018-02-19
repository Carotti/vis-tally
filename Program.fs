// Learn more about F# at http://fsharp.org
module Program
open System
open CommonTop
open CommonData
open DP
open DPexecute
open Execution
open System.Linq


////////////////////////////////////////////////////////////////////////////////
// maccth helper functions, to delete 
let qp thing = thing |> printfn "%A"
let qpl lst = lst |> List.map (qp)
// maccth helper functions, to delete 
////////////////////////////////////////////////////////////////////////////////


let repl() =
    let rec repl'() =
        printf  "~> "
        System.Console.ReadLine().ToUpper()
        |> parseLine None (WA 0u) 
        |> qp
        repl'()
    repl'()


[<EntryPoint>]
let main argv =
    /// test the initProjectLexer code
    // let instrLst = [
    //     "hello: ADD R1, R2, #0xf000000f";
    //     "ADD R2, R4, #0xf8000007";
    //     "ADD R20, R4, #0xf8000007";
    //     "ADD R1, R2, R3, R4";
    //     "ADD R0, R1, R2 ";
    //     "ADD R1, R2, R20, RRX";
    // ]

    // instrLst
    // |> List.map (fun instr -> (instr + "\n", parseLine None (WA 0u) instr))
    // |> qpl
    // |> ignore

    // let test = parseLine None (WA 0u) "ADD R1, R2, R3, RRX"
    // test |> qp


    // "ready to REPL..." |> (printfn "%s")
    // repl()

    "hello" |> qp


    let dp = initialiseDP false false false false [0u..15u]

    dp |> qp

    let a =
        System.Console.ReadLine().ToUpper()
        |> parseLine None (WA 0u)
    
    let res =
        match a with
        | Ok a' -> execute dp a'
        | Error b ->
            "Just a dummy error"
            |> ``Run time error``
            |> Error

    qp res 

    0 // return an integer exit code
