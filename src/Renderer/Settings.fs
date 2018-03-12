module Settings

open Fable.Core
open Fable.Core.JsInterop
open Fable.Import
open Fable.Import.Electron
open Node.Exports
open Fable.PowerPack

open Fable.Import.Browser

open Update
open Ref

// HTML description for the settings menu
let settingsMenu () =
    let menu = document.createElement("div")

    
    menu.innerHTML <- "Settings Tab for now"

    menu

let createSettingsTab () =
    match settingsTab with
    | Some tab -> selectFileTab tab
    | Microsoft.FSharp.Core.option.None ->
        let id = createTab "Settings"
        settingsTab <- Some id

        let sv = settingsMenu ()

        sv.classList.add("invisible")
        sv.id <- fileViewIdFormatter id

        fileViewPane.appendChild(sv) |> ignore
        selectFileTab id