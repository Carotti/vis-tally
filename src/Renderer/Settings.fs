module Settings

open Fable.Core
open Fable.Core.JsInterop
open Fable.Import
open Fable.Import.Electron
open Node.Exports
open Fable.PowerPack

open Fable.Import.Browser

open Ref
open Tabs

let editorFontSize = "editor-font-size"
let editorTheme = "editor-theme"



let themes = [
                "vs-light", "Light"; 
                "vs-dark", "Dark"; 
              ]

let getSettingInput (name : string) =
    let input = document.getElementById(name) :?> HTMLInputElement
    input.value

let setSettingInput (name : string) =
    setSetting name (getSettingInput name)

// Go through the form extracting all of the relevant settings
let saveSettings () =
    setSettingInput editorFontSize
    setSettingInput editorTheme
    updateAllEditors()

let makeFormGroup label input =
    let fg = document.createElement("div")
    fg.classList.add("form-group")

    let lab = document.createElement("label")
    lab.innerHTML <- label

    let br = document.createElement("br")

    fg.appendChild(lab) |> ignore
    fg.appendChild(br) |> ignore
    fg.appendChild(input) |> ignore

    fg

let makeInputVal inType name =
    let fi = document.createElement_input()
    fi.``type`` <- inType
    fi.id <- name
    fi.value <- (getSetting name).ToString()
    // Whenever a form input is changed, set the settings tab unsaved
    fi.onchange <- (fun _ -> setTabUnsaved (getSettingsTabId ()))
    fi

let makeInputSelect options name =
    let makeOption (optionValue, optionName) =
        let opt = document.createElement_option()
        opt.innerHTML <- optionName
        opt.value <- optionValue
        opt

    let select = document.createElement_select()
    select.classList.add("form-control")
    select.classList.add("settings-select")
    select.id <- name

    List.map (makeOption >> (fun x -> select.appendChild(x))) options |> ignore

    Browser.console.log (sprintf "Setting to %A" ((getSetting name).ToString()))
    select.value <- (getSetting name).ToString()
    Browser.console.log (sprintf "Value after: %A" select.value)

    select

let editorForm () =
    let form = document.createElement("form")

    let fontSizeInput = makeInputVal "number" editorFontSize
    let fontSize = makeFormGroup "Font Size" fontSizeInput
    form.appendChild(fontSize) |> ignore

    let themeSelect = makeInputSelect themes editorTheme
    let theme = makeFormGroup "Theme" themeSelect
    form.appendChild(theme) |> ignore

    form

// HTML description for the settings menu
let settingsMenu () =
    let menu = document.createElement("div")
    menu.classList.add("settings-menu")
    menu.classList.add("editor")

    let editorHeading = document.createElement("h2")
    editorHeading.innerHTML <- "Editor"

    menu.appendChild(editorHeading) |> ignore
    menu.appendChild(editorForm()) |> ignore

    let saveButton = document.createElement("button")
    saveButton.classList.add("btn")
    saveButton.classList.add("btn-default")

    saveButton.innerHTML <- "Save"
    saveButton.addEventListener_click(fun _ -> saveSettings() ; setTabSaved (getSettingsTabId ()))

    menu.appendChild(saveButton) |> ignore

    menu

let createSettingsTab () =
    // If the settings tab already exists, just switch to it, else create it
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