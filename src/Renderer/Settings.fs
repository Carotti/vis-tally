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

let editorForm () =
    let form = document.createElement("form")

    let fontSize = document.createElement("div")


    form

// HTML description for the settings menu
let settingsMenu () =
    let menu = document.createElement("div")
    menu.classList.add("settings-menu")
    menu.classList.add("editor")

    let editorHeading = document.createElement("h1")
    editorHeading.innerHTML <- "Editor"

    menu.appendChild(editorHeading) |> ignore
    menu.appendChild(editorForm()) |> ignore

    menu

let createSettingsTab () =
    match settingsTab with
    | Some tab -> selectFileTab tab
    | Microsoft.FSharp.Core.option.None ->
        let id = createTab " Settings"
        settingsTab <- Some id

        let tabName = fileTabName id
        tabName.classList.add("icon")
        tabName.classList.add("icon-cog")

        let sv = settingsMenu ()

        sv.classList.add("invisible")
        sv.id <- fileViewIdFormatter id

        fileViewPane.appendChild(sv) |> ignore
        selectFileTab id