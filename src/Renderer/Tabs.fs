module Tabs

open Fable.Core
open Fable.Core.JsInterop
open Fable.Import
open Fable.Import.Electron
open Node.Exports
open Fable.PowerPack

open Fable.Import.Browser

open Ref

// Default settings if they haven't already been defined by electron-settings
let defaultSettings = Map.ofList [
                            "editor-font-size" ==> "12"
                            "editor-theme" ==> "vs-light"
                        ]

let getSetting (name : string) =
    let setting = settings?get(name)
    match isUndefined setting with
    | true -> defaultSettings.[name]
    | false -> setting

let editorOptions () = createObj [
                        // User defined settings
                        "theme" ==> getSetting "editor-theme";
                        "renderWhitespace" ==> "all";
                        "fontSize" ==> getSetting "editor-font-size";
                        // Application defined settings
                        "value" ==> "";
                        "language" ==> "arm";
                        "roundedSelection" ==> false;
                        "scrollBeyondLastLine" ==> false;
                        "automaticLayout" ==> true;
                    ]
    
let setSetting (name : string) (value : obj) =
    settings?set(name, value) |> ignore

let mutable currentFileTabId = -1 // By default no tab is open
let mutable fileTabList : int list = []

// Map tabIds to the editors which are contained in them
let mutable editors : Map<int, obj> = Map.ofList []

let mutable settingsTab : int option = Microsoft.FSharp.Core.option.None

let getSettingsTabId () =
    match settingsTab with
    | Some x -> x
    | _ -> failwithf "No settings tab exists"


let uniqueTabId () =
    // Look in fileTabList and find the next unique id
    match List.isEmpty fileTabList with
    | true -> 0
    | false -> (List.last fileTabList) + 1
let selectFileTab id =
    // Hacky match, but otherwise deleting also attempts to select the deleted tab
    match List.contains id fileTabList || id < 0 with
    | true ->
        Browser.console.log(sprintf "Switching to tab #%d" id)

        // Only remove active from the previously selected tab if it existed
        match currentFileTabId < 0 with
        | false ->
            let oldTab = fileTab currentFileTabId
            oldTab.classList.remove("active")
            let oldView = fileView currentFileTabId
            oldView.classList.add("invisible")
        | true -> ()

        // If the new id is -1, no tab is selected
        match id < 0 with
        | true -> ()
        | false ->
            let newTab = fileTab id
            newTab.classList.add("active")
            let newView = fileView id
            newView.classList.remove("invisible")

        currentFileTabId <- id
    | false -> ()

let getTabName id = 
    (fileTabName id).innerHTML

// Determines if a tab of a given id is unsaved
let isTabUnsaved id = 
    (fileTab id).lastElementChild.classList.contains("unsaved")

let deleteFileTab id =
    let isSettingsTab =
        match settingsTab with
        | Microsoft.FSharp.Core.option.None -> false
        | Some tab when tab = id -> true
        | _ -> false

    // Confirm delete message is slightly different for the settings menu
    let tabName =
        match isSettingsTab with
        | true -> "settings"
        | false -> sprintf "'%s" (getTabName id)

    let confirmDelete = 
        match isTabUnsaved id with
        | false -> true
        | true -> Browser.window.confirm(
                    sprintf "You have unsaved changes, are you sure you want to close %s?" tabName
                    )

    match confirmDelete with
    | false -> ()
    | true ->
        fileTabList <- List.filter (fun x -> x <> id) fileTabList
        match currentFileTabId with
        | x when x = id ->
            selectFileTab
                <| match List.isEmpty fileTabList with
                    | true -> -1
                    | false -> List.last fileTabList
        | _ -> ()
        fileTabMenu.removeChild(fileTab id) |> ignore
        fileViewPane.removeChild(fileView id) |> ignore
        match isSettingsTab with
        | true -> 
            settingsTab <- Microsoft.FSharp.Core.option.None
        | false ->
            let editor = editors.[id]
            editor?dispose() |> ignore // Delete the Monaco editor
            editors <- Map.remove id editors
    
let setTabUnsaved id = 
    let tab = fileTabName id
    tab.classList.add("unsaved")
let setTabSaved id =
    let tab = fileTabName id
    tab.classList.remove("unsaved")

let setTabName id name = 
    let nameSpan = fileTabName id
    nameSpan.innerHTML <- name

// Create a new tab of a particular name and then return its id
let createTab name =
    let tab = document.createElement("div")
    tab.classList.add("tab-item")
    tab.classList.add("tab-file")

    let defaultFileName = document.createElement("span")
    defaultFileName.classList.add("tab-file-name")

    let cancel = document.createElement("span")
    cancel.classList.add("icon")
    cancel.classList.add("icon-cancel")
    cancel.classList.add("icon-close-tab")

    let id = uniqueTabId ()
    tab.id <- fileTabIdFormatter id

    // Create an empty span to store the filepath of this tab
    let filePath = document.createElement("span")
    filePath.classList.add("invisible")
    filePath.id <- tabFilePathIdFormatter id

    // Add the necessary elements to create the new tab
    tab.appendChild(filePath) |> ignore
    tab.appendChild(cancel) |> ignore
    tab.appendChild(defaultFileName) |> ignore

    defaultFileName.innerHTML <- name

    defaultFileName.id <- tabNameIdFormatter id

    cancel.addEventListener_click(fun _ -> 
        Browser.console.log(sprintf "Deleting tab #%d" id)
        deleteFileTab id
    )

    tab.addEventListener_click(fun _ ->
        selectFileTab id
    )

    fileTabList <- fileTabList @ [id]

    fileTabMenu.insertBefore(tab, newFileTab) |> ignore
    id

let createNamedFileTab name =
    let id = createTab name

    // Create the new view div
    let fv = document.createElement("div")
    fv.classList.add("editor")
    fv.classList.add("invisible")    
    fv.id <- fileViewIdFormatter id

    fileViewPane.appendChild(fv) |> ignore

    let editor = window?monaco?editor?create(fv, editorOptions())
    
    // Whenever the content of this editor changes
    editor?onDidChangeModelContent(fun _ ->
        setTabUnsaved id // Set the unsaved icon in the tab
    ) |> ignore

    editors <- Map.add id editor editors
    // Return the id of the tab we just created
    id

let createFileTab () = 
    createNamedFileTab "Untitled.S" 
    |> selectFileTab // Switch to the tab we just created

let deleteCurrentTab () =
    match currentFileTabId >= 0 with
    | false -> ()
    | true -> deleteFileTab currentFileTabId
    
let updateEditor tId =
    editors.[tId]?updateOptions(editorOptions()) |> ignore

let setTheme theme = 
    window?monaco?editor?setTheme(theme)

let updateAllEditors () =
    (List.map (fst >> updateEditor) (editors
    |> Map.toList))
    |> ignore

    setTheme (editorOptions())?theme |> ignore