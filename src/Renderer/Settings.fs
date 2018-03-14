module Settings

open Fable.Import.Browser

open Ref
open Tabs
open Editor

let editorFontSize = "editor-font-size"
let editorTheme = "editor-theme"
let editorWordWrap = "editor-word-wrap"
let editorRenderWhitespace = "editor-render-whitespace"

let inputSettings = [
                        editorFontSize
                        editorTheme
                        editorWordWrap
                        editorRenderWhitespace
                    ]


let themes = [
                "vs-light", "Light"; 
                "vs-dark", "Dark"; 
              ]

let setSettingsUnsaved = (fun _ -> setTabUnsaved (getSettingsTabId ()))

let getSettingInput (name : string) =
    let input = document.getElementById(name) :?> HTMLInputElement
    input.value

let setSettingInput (name : string) =
    setSetting name (getSettingInput name)

// Go through the form extracting all of the relevant settings
let saveSettings () =
    List.map setSettingInput inputSettings |> ignore
    updateAllEditors()

let makeFormGroup label input =
    let fg = document.createElement("div")
    fg.classList.add("form-group")

    let lab = document.createElement("label")
    lab.innerHTML <- label
    lab.classList.add("settings-label")

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
    fi.onchange <- setSettingsUnsaved
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

    select.value <- (getSetting name).ToString()

    select.onchange <- setSettingsUnsaved

    select

let makeInputCheckbox name trueVal falseVal =
    let checkbox = document.createElement_input()
    checkbox.``type`` <- "checkbox"
    checkbox.id <- name

    let setValue() = 
        checkbox.value <- match checkbox.``checked`` with
                            | true -> trueVal
                            | false -> falseVal

    // When the checkbox is ticked, update its value
    checkbox.addEventListener_click (fun _ -> setValue())

    checkbox.``checked`` <- match (getSetting name).ToString() with
                            | x when x = trueVal -> true
                            | _ -> false
    
    setValue()

    checkbox.onchange <- setSettingsUnsaved
    checkbox

let editorForm () =
    let form = document.createElement("form")

    let makeAdd label input =
        let group = makeFormGroup label input
        form.appendChild(group) |> ignore

    let fontSizeInput = makeInputVal "number" editorFontSize
    makeAdd "Font Size" fontSizeInput

    let themeSelect = makeInputSelect themes editorTheme
    makeAdd "Theme" themeSelect

    let wordWrapCheck = makeInputCheckbox editorWordWrap "on" "off"
    makeAdd "Word Wrap" wordWrapCheck

    let renderWhitespace = makeInputCheckbox editorRenderWhitespace "all" "none"
    makeAdd "Render Whitespace Characters" renderWhitespace

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