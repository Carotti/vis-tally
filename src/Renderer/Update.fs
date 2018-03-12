(* 
    High Level Programming @ Imperial College London # Spring 2018
    Project: A user-friendly ARM emulator in F# and Web Technologies ( Github Electron & Fable Compliler )
    Contributors: Angelos Filos
    Module: Renderer.Update
    Description: Event helper functions for `HTML` elements in `index.html`.
*)

module Update

open Fable.Core
open Fable.Core.JsInterop
open Fable.Import
open Fable.Import.Electron
open Node.Exports
open Fable.PowerPack

open Fable.Import.Browser


open Ref
open Fable
open System.Configuration

[<Emit("$0 === undefined")>]
let isUndefined (_: 'a) : bool = jsNative

// The current number representation being used
let mutable currentRep = Hex
let mutable currentView = Registers

let mutable byteView = false

let mutable memoryMap : Map<uint32, uint32> = Map.ofList []

let mutable currentFileTabId = -1 // By default no tab is open
let mutable fileTabList = []

// Map tabIds to the editors which are contained in them
let mutable editors : Map<int, obj> = Map.ofList []

let mutable editorOptions = createObj [
                                "value" ==> "";
                                "language" ==> "arm";
                                "theme" ==> "vs-light";
                                "renderWhitespace" ==> "all";
                                "roundedSelection" ==> false;
                                "scrollBeyondLastLine" ==> false;
                                "automaticLayout" ==> true;
                            ]

let mutable settingsTab : int option = Microsoft.FSharp.Core.option.None

// Returns a formatter for the given representation
let formatter rep = 
// TODO: Use binformatter from testformats.fs
    let binFormatter fmt x =
        let rec bin a =
            let bit = string (a % 2u)
            match a with 
            | 0u | 1u -> bit
            | _ -> bin (a / 2u) + bit
        sprintf fmt (bin x)
    match rep with
    | Hex -> (sprintf "0x%X")
    | Bin -> (binFormatter "0b%s")
    | Dec -> (int32 >> sprintf "%d")
    | UDec -> (sprintf "u%u")

let fontSize (size: int) =
    let options = createObj ["fontSize" ==> size]
    window?code?updateOptions options
    
let setRegister (id: int) (value: uint32) =
    let el = Ref.register id
    el.innerHTML <- formatter currentRep value

let registerValue (id : int) : uint32 =
    let el = Ref.register id
    let html = el.innerHTML
    match html.[0] with
    | 'u' -> uint32 html.[1..]
    | _ -> uint32 html

let flag (id: string) (value: bool) =
    let el = Ref.flag id
    match value with
        | false ->
            el.setAttribute("style", "background: #fcfcfc")
            el.innerHTML <- sprintf "%i" 0
        | true ->
            el.setAttribute("style", "background: #4285f4")
            el.innerHTML <- sprintf "%i" 1

let setTheme theme = 
    window?monaco?editor?setTheme(theme)

let setRepresentation rep =
    // Disable the other button
    (representation currentRep).classList.remove("btn-rep-enabled")
    |> ignore

    // Enable the newly pressed button
    let btnNew = representation rep
    btnNew.classList.add("btn-rep-enabled");

    // Reassign currentRep, ew mutability required
    currentRep <- rep

    // Update all of the current register values to update the formatting
    [0..15]
    |> List.map (
        (fun r -> (r, registerValue r)) 
        >> (fun (r, v) -> setRegister r v)
    )

let setView view =
    // Change the active tab
    (viewTab currentView).classList.remove("active")
    (viewTab view).classList.add("active")

    // Change the visibility of the views
    (viewView currentView).classList.add("invisible")
    (viewView view).classList.remove("invisible")

    // ew mutability again, update the variable
    currentView <- view

let toggleByteView () = 
    byteView <- not byteView
    match byteView with
    | true -> 
        byteViewBtn.classList.add("btn-byte-active")
        byteViewBtn.innerHTML <- "Disable Byte View"
    | false -> 
        byteViewBtn.classList.remove("btn-byte-active")
        byteViewBtn.innerHTML <- "Enable Byte View"

// Converts a memory map to a list of lists which are contiguous blocks of memory
let contiguousMemory (mem : Map<uint32, uint32>) =
    Map.toList mem
    |> List.fold (fun state (addr, value) -> 
        match state with
        | [] -> [[(addr, value)]]
        | hd :: tl ->
            match hd with
            | [] -> failwithf "Contiguous memory never starts a new list with no elements"
            | hd' :: _ when fst hd' = addr - 4u -> 
                ((addr, value) :: hd) :: tl // Add to current contiguous block
            | _ :: _ -> [(addr, value)] :: state // Non-contiguous, add to new block
    ) [] 
    |> List.map List.rev // Reverse each list to go back to increasing
    |> List.rev // Reverse the overall list

// Converts a list of (uint32 * uint32) to a byte addressed
// memory list of (uint32 * uint32) which is 4 times longer
// LITTLE ENDIAN
let lstToBytes (lst : (uint32 * uint32) list) =
    lst
    |> List.collect (fun (addr, value) -> 
        [
            addr, value |> byte |> uint32
            addr + 1u, (value >>> 8) |> byte |> uint32
            addr + 2u, (value >>> 16) |> byte |> uint32;
            addr + 3u, (value >>> 24) |> byte |> uint32;
        ]
    )

// Creates the html to format the memory table in contiguous blocks
let updateMemory () =
    let makeRow (addr : uint32, value : uint32) =
        let tr = document.createElement("tr")
        tr.classList.add("tr-head-mem")

        let tdAddr = document.createElement("td")
        tdAddr.classList.add("selectable-text")
        tdAddr.innerHTML <- sprintf "0x%X" addr

        let tdValue = document.createElement("td")
        tdValue.classList.add("selectable-text")
        tdValue.innerHTML <- formatter currentRep value

        tr.appendChild(tdAddr) |> ignore
        tr.appendChild(tdValue) |> ignore
        tr

    let makeContig (lst : (uint32 * uint32) list) = 
        let li = document.createElement("li")
        li.classList.add("list-group-item")
        li.style.padding <- "0px"

        let table = document.createElement("table")
        table.classList.add("table-striped")

        let tr = document.createElement("tr")

        let thAddr = document.createElement("th")
        thAddr.classList.add("th-mem")
        thAddr.innerHTML <- "Address"

        let thValue = document.createElement("th")
        thValue.classList.add("th-mem")
        thValue.innerHTML <- "Value"

        tr.appendChild(thAddr) |> ignore
        tr.appendChild(thValue) |> ignore

        table.appendChild(tr) |> ignore

        let byteSwitcher = 
            match byteView with
            | true -> lstToBytes
            | false -> id

        // Add each row to the table from lst
        lst
        |> byteSwitcher
        |> List.map (makeRow >> (fun html -> table.appendChild(html)))
        |> ignore

        li.appendChild(table) |> ignore
        li
    
    // Clear the old memory list
    memList.innerHTML <- ""

    // Add the new memory list
    memoryMap
    |> contiguousMemory
    |> List.map (makeContig >> (fun html -> memList.appendChild(html)))
    |> ignore

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

let setTabFilePath id path =
    let fp = (tabFilePath id)
    fp.innerHTML <- path
 
let getTabFilePath id =
    let fp = (tabFilePath id)
    fp.innerHTML

let baseFilePath (path : string) =
    path.Split [|'/'|]
    |> Array.last

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

    let editor = window?monaco?editor?create(fv, editorOptions)
    
    // Whenever the content of this editor changes
    editor?onDidChangeModelContent(fun _ ->
        setTabUnsaved id // Set the unsaved icon in the tab
    ) |> ignore

    editors <- Map.add id editor editors
    // Return the id of the tab we just created
    id

let createSettingsTab () =
    match settingsTab with
    | Some tab -> selectFileTab tab
    | Microsoft.FSharp.Core.option.None ->
        let id = createTab "Settings"
        settingsTab <- Some id

        let sv = document.createElement("div")
        sv.classList.add("invisible")
        sv.id <- fileViewIdFormatter id

        sv.innerHTML <- "Settings Tab for now"

        fileViewPane.appendChild(sv) |> ignore
        selectFileTab id
let createFileTab () = 
    createNamedFileTab "Untitled.S" 
    |> selectFileTab // Switch to the tab we just created

let deleteCurrentTab () =
    match currentFileTabId >= 0 with
    | false -> ()
    | true -> deleteFileTab currentFileTabId

// Load the node Buffer into the specified tab
let loadFileIntoTab tId (fileData : Node.Buffer.Buffer) =
    let editor = editors.[tId]
    editor?setValue(fileData.toString("utf8")) |> ignore
    setTabSaved tId

// Return the code in tab id tId as a string
let getCode tId =
    let editor = editors.[tId]
    editor?getValue() :?> string

// If x is undefined, return errCase, else return Ok x
let resultUndefined errCase x =
    match isUndefined x with
    | true -> Result.Error errCase
    | false -> Result.Ok x

let openFile () =
    let options = createEmpty<OpenDialogOptions>
    options.properties <- ResizeArray(["openFile"; "multiSelections"]) |> Some

    let readPath (path, tId) = 
        fs.readFile(path, (fun err data -> // TODO: find out what this error does
            loadFileIntoTab tId data
        ))
        |> ignore
        tId // Return the tab id list again to open the last one

    let makeTab path =
        let tId = createNamedFileTab (baseFilePath path)
        setTabFilePath tId path
        (path, tId)

    let result = electron.remote.dialog.showOpenDialog(options)

    let checkResult (res : ResizeArray<string>) =
        match isUndefined res with
        | true -> Result.Error () // No files were opened, so don't do anything
        | false -> Result.Ok (res.ToArray())

    result
    |> resultUndefined ()
    |> Result.map (fun x -> x.ToArray())
    |> Result.map Array.toList
    |> Result.map (List.map (makeTab >> readPath))
    |> Result.map List.last
    |> Result.map selectFileTab
    |> ignore


let writeToFile str path =
    let errorHandler _err = // TODO: figure out how to handle errors which can occur
        ()
    fs.writeFile(path, str, errorHandler)

let writeCurrentCodeToFile path = (writeToFile (getCode currentFileTabId) path)

let saveFileAs () =
    let options = createEmpty<SaveDialogOptions>

    let currentPath = getTabFilePath currentFileTabId
    
    // If a path already exists for this file, open it
    match currentPath with
    | "" -> ()
    | _ -> options.defaultPath <- Some currentPath

    let result = electron.remote.dialog.showSaveDialog(options)

    // Performs op on x then returns x
    let split op x =
        op x |> ignore
        x

    // Write the file, return the path again so it can be set
    let writer = split writeCurrentCodeToFile

    // Update the path of this tab and return the path again
    let pathUpdater = split (setTabFilePath currentFileTabId)
       
    // If result is an actual path, write the contents of the current tab
    // to the file
    result
    |> resultUndefined ()
    |> Result.map writer
    |> Result.map pathUpdater
    |> Result.map baseFilePath
    |> Result.map (setTabName currentFileTabId)
    |> ignore

    setTabSaved (currentFileTabId)

// If a path already exists for a file, write it straight to disk without the dialog
let saveFile () =
    match getTabFilePath currentFileTabId with
    | "" -> saveFileAs () // No current path exists
    | path -> writeCurrentCodeToFile path

    setTabSaved (currentFileTabId)

let editorFind () =
    let action = editors.[0]?getAction("actions.find")
    action?run() |> ignore
 
let editorFindReplace () =
    let action = editors.[currentFileTabId]?getAction("editor.action.startFindReplaceAction")
    action?run() |> ignore

let editorUndo () =
    editors.[currentFileTabId]?trigger("Update.fs", "undo") |> ignore

let editorRedo () =
    editors.[currentFileTabId]?trigger("Update.fs", "redo") |> ignore

let editorSelectAll () = 
    editors.[currentFileTabId]?trigger("Update.fs", "selectAll") |> ignore