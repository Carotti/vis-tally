// Learn more about F# at http://fsharp.org
module Program
open System
open CommonTop
open CommonData
open DP
open DPExecution
open System.Linq


let qp thing = thing |> printfn "%A"

let qpl lst = lst |> List.map (qp)

let parseREPL dp =
    let rec repl'() =
        printf  "~> "
        System.Console.ReadLine().ToUpper()
        |> parseLine None (WA 0u) 
        |> qp
        repl'()
    repl'()

let exeREPL dp =
    let printRegs dp =
        dp.Regs |> Map.toList |> List.map (fun (r, v) -> printfn "%A : %x" r v) |> ignore
    let printFlags dp =
         dp.Fl |> qp |> ignore
    
    printRegs dp
    printFlags dp

    let rec repl' dp =
        printf  "~> "
        System.Console.ReadLine().ToUpper()
        |> parseLine None (WA 0u)
        |> function
        | Error e ->
            e |> qp
            repl' dp
        | Ok instr ->
            execute dp instr
            |> function
            | Ok dp' ->
              printRegs dp'
              printFlags dp'
              repl' dp'
            | Error e' ->
                e' |> qp
                repl' dp
    repl' dp
    


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


    "ready to REPL..." |> (printfn "%s")
    parseREPL() |> ignore

    // "ready to REPL..." |> qp
    // let dp = initialiseDP false false false false [0u]
    // exeREPL dp |> ignore

    0 // return an integer exit code