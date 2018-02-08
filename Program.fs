// Learn more about F# at http://fsharp.org
module Program
open System
open Common.CommonTop
open Common.CommonData

[<EntryPoint>]
let main argv =
    /// test the initProjectLexer code
    let test = parseLine None (WA 0u)
    0 // return an integer exit code
