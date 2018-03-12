(* 
    High Level Programming @ Imperial College London # Spring 2018
    Project: A user-friendly ARM emulator in F# and Web Technologies ( Github Electron & Fable Compliler )
    Contributors: Angelos Filos
    Module: Ref
    Description: References to `HTML` elements from `index.html`.
*)

module Ref

open Fable.Core
open Fable.Core.JsInterop
open Fable.Import
open Fable.Import.Browser
open Fable.Import.JS
open Microsoft.FSharp.Collections

type Representations =
    | Hex
    | Bin
    | Dec
    | UDec

let repToId = Map.ofList [
                Hex, "rep-hex";
                Bin, "rep-bin";
                Dec, "rep-dec";
                UDec, "rep-udec";
                ]

type Views =
    | Registers
    | Memory
    | Symbols

let viewToIdView = Map.ofList [
                    Registers, "view-reg";
                    Memory, "view-mem";
                    Symbols, "view-sym";
                ]
let viewToIdTab = Map.ofList [
                        Registers, "tab-reg";
                        Memory, "tab-mem";
                        Symbols, "tab-sym"
                    ]

// A reference to the settings for the app
let settings : obj = importDefault "electron-settings"

let fontSize: HTMLSelectElement =
    Browser.document.getElementById("font-size") :?> HTMLSelectElement
let register (id: int): HTMLElement =
    Browser.document.getElementById(sprintf "R%i" id)
let explore: HTMLButtonElement =
    Browser.document.getElementById("explore") :?> HTMLButtonElement
let save: HTMLButtonElement =
    Browser.document.getElementById("save") :?> HTMLButtonElement
let run: HTMLButtonElement =
    Browser.document.getElementById("run") :?> HTMLButtonElement
let flag (id: string): HTMLElement =
    Browser.document.getElementById(sprintf "flag_%s" id)

let representation rep =
    Browser.document.getElementById(repToId.[rep])

let viewView view =
    Browser.document.getElementById(viewToIdView.[view])

let viewTab view =
    Browser.document.getElementById(viewToIdTab.[view])

let byteViewBtn = 
    Browser.document.getElementById("byte-view")

let memList =
    Browser.document.getElementById("mem-list")

let symView =
    Browser.document.getElementById("sym-view")

let symTable =
    Browser.document.getElementById("sym-table")

let fileTabMenu = 
    Browser.document.getElementById("tabs-files")
let newFileTab =
    Browser.document.getElementById("new-file-tab")


let fileTabIdFormatter = sprintf "file-tab-%d"

let fileTab id = 
    Browser.document.getElementById(fileTabIdFormatter id)

let fileViewIdFormatter = sprintf "file-view-%d"

let fileView id =
    Browser.document.getElementById(fileViewIdFormatter id)

let fileViewPane =
    Browser.document.getElementById("file-view-pane")

let tabNameIdFormatter = sprintf "file-view-name-%d"

let fileTabName id =
    Browser.document.getElementById(tabNameIdFormatter id)

let tabFilePathIdFormatter = sprintf "file-view-path-%d"

let tabFilePath id =
    Browser.document.getElementById(tabFilePathIdFormatter id)