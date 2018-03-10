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
        let mutable tr = document.createElement("tr")
        tr.classList.add("tr-head-mem")

        let mutable tdAddr = document.createElement("td")
        tdAddr.classList.add("selectable-text")
        tdAddr.innerHTML <- sprintf "0x%X" addr

        let mutable tdValue = document.createElement("td")
        tdValue.classList.add("selectable-text")
        tdValue.innerHTML <- formatter currentRep value

        tr.appendChild(tdAddr) |> ignore
        tr.appendChild(tdValue) |> ignore
        tr

    let makeContig (lst : (uint32 * uint32) list) = 
        let mutable li = document.createElement("li")
        li.classList.add("list-group-item")
        li.style.padding <- "0px"

        let mutable table = document.createElement("table")
        table.classList.add("table-striped")

        let mutable tr = document.createElement("tr")

        let mutable thAddr = document.createElement("th")
        thAddr.classList.add("th-mem")
        thAddr.innerHTML <- "Address"

        let mutable thValue = document.createElement("th")
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
    let confirmDelete = 
        match isTabUnsaved id with
        | false -> true
        | true -> Browser.window.confirm(
                    sprintf "Are you sure you want to close '%s'?" (getTabName id)
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
        editors <- Map.remove id editors
    
let setTabUnsaved id = 
    let tab = fileTab id

    match tab.lastElementChild.classList.contains("icon-record") with
    | true -> ()
    | false ->
        let mutable unsaved = document.createElement("span")
        unsaved.classList.add("icon")
        unsaved.classList.add("unsaved")
        unsaved.classList.add("icon-record")
        unsaved.style.opacity <- "0.5"

        tab.appendChild(unsaved) |> ignore

let setTabSaved id =
    let tab = fileTab id
    tab.lastElementChild.classList.remove("unsaved")
 
let createNamedFileTab name =
    let mutable tab = document.createElement("div")
    tab.classList.add("tab-item")
    tab.classList.add("tab-file")

    let defaultFileName = document.createElement("span")
    defaultFileName.classList.add("tab-file-name")

    let mutable cancel = document.createElement("span")
    cancel.classList.add("icon")
    cancel.classList.add("icon-cancel")
    cancel.classList.add("icon-close-tab")

    let mutable spacer = document.createElement("span")
    spacer.innerHTML <- " "

    let id = uniqueTabId ()
    tab.id <- fileTabIdFormatter id

    // Add the necessary elements to create the new tab
    tab.appendChild(cancel) |> ignore
    tab.appendChild(defaultFileName) |> ignore
    tab.appendChild(spacer) |> ignore

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

    // Create the new view div
    let mutable fv = document.createElement("div")
    fv.classList.add("editor")
    fv.classList.add("invisible")    
    fv.id <- fileViewIdFormatter id

    fileViewPane.appendChild(fv) |> ignore

    let mutable editor = window?monaco?editor?create(fv, editorOptions)
    
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

let loadFileIntoTab tId (fileData : Node.Buffer.Buffer) =
    let editor = editors.[tId]
    editor?setValue(fileData.toString("utf8")) |> ignore
    setTabSaved tId

let openFile () =
    let options = createEmpty<OpenDialogOptions>
    options.properties <- ResizeArray(["openFile"; "multiSelections"]) |> Some
    
    (List.map ((fun x -> (x, createNamedFileTab x)) >> (fun (path, tId) ->
        fs.readFile(path, (fun err data -> // TODO: find out what this error does
            loadFileIntoTab tId data
        ))
        |> ignore
        tId // Return the tab id list again to open the last one
        )) ((electron.remote.dialog.showOpenDialog(options)).ToArray()
    |> Array.toList))
    |> List.last
    |> selectFileTab
    |> ignore
    ()