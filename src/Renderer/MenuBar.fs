module MenuBar

open Fable.Core
open Fable.Core.JsInterop
open Fable.Import
open Fable.Import.Electron

open Update
open Settings
open Tabs

let id2 = (fun _ _ -> ())

let handlerCaster f = System.Func<MenuItem, BrowserWindow, unit> f |> Some

let menuSeperator = 
    let sep = createEmpty<MenuItemOptions>
    sep.``type`` <- Some Separator
    sep

let createMenuItem label accelerator click = 
    let item = createEmpty<MenuItemOptions>
    item.label <- Some label
    item.accelerator <- accelerator
    item.click <- handlerCaster click
    item
    
let makeSubMenu name items =
    let subMenu = createEmpty<MenuItemOptions>
    subMenu.label <- Some name
    subMenu.submenu <- items |> U2.Case2 |> Some

    subMenu

let getFileMenu =
    let save = createMenuItem "Save" 
                (Some "CmdOrCtrl+S") 
                (fun _ _ -> saveFile())

    let saveAs = createMenuItem 
                    "Save As" 
                    (Some "CmdOrCtrl+Shift+S")
                    (fun _ _ -> saveFileAs())

    let openf = createMenuItem
                    "Open"
                    (Some "CmdOrCtrl+O")
                    (fun _ _ -> openFile())

    let newf = createMenuItem
                "New"
                (Some "CmdOrCtrl+N")
                (fun _ _ -> createFileTab())

    let exit = createMenuItem 
                "Quit"
                (Some "Ctrl+Q")
                (fun _ _ -> electron.remote.app.quit())

    let close = createMenuItem
                    "Close"
                    (Some "Ctrl+W")
                    (fun _ _ ->  deleteCurrentTab ())

    let items = ResizeArray<MenuItemOptions> [
                    newf
                    menuSeperator
                    save
                    saveAs
                    openf
                    menuSeperator
                    close
                    menuSeperator
                    exit
                ]

    makeSubMenu "File" items

let getEditMenu =
    let undo = createMenuItem
                "Undo"
                (Some "CmdOrCtrl+Z")
                (fun _ _ -> editorUndo ())

    let redo = createMenuItem
                "Redo"
                (Some "CmdOrCtrl+Shift+Z")
                (fun _ _ -> editorRedo ())

    let cut = createMenuItem
                "Cut"
                (Some "CmdOrCtrl+X")
                id2
    cut.role <- U2.Case1 MenuItemRole.Cut |> Some

    let copy = createMenuItem
                "Copy"
                (Some "CmdOrCtrl+C")
                id2
    copy.role <- U2.Case1 MenuItemRole.Copy |> Some
    
    let paste = createMenuItem
                    "Paste"
                    (Some "CmdOrCtrl+V")
                    id2
    paste.role <- U2.Case1 MenuItemRole.Paste |> Some

    let selectAll = createMenuItem
                        "Select All"
                        (Some "CmdOrCtrl+A")
                        (fun _ _ -> editorSelectAll ())

    let find = createMenuItem
                "Find"
                (Some "CmdOrCtrl+F")
                (fun _ _ -> editorFind())
               
    let findReplace = createMenuItem
                        "Replace"
                        (Some "CmdOrCtrl+H")
                        (fun _ _ -> editorFindReplace())
    
    let preferences = createMenuItem
                        "Preferences"
                        Option.None
                        (fun _ _ -> createSettingsTab ())

    let items = ResizeArray<MenuItemOptions> [
                    undo
                    redo
                    menuSeperator
                    cut
                    copy
                    paste
                    menuSeperator
                    selectAll
                    menuSeperator
                    find
                    findReplace
                    menuSeperator
                    preferences
                ]

    makeSubMenu "Edit" items


let getViewMenu = 
    let fullscreen = createMenuItem
                        "Toggle Fullscreen"
                        (Some "F11")
                        id2
    fullscreen.role <- U2.Case1 MenuItemRole.Togglefullscreen |> Some

    let zoomIn = createMenuItem
                    "Zoom In"
                    (Some "CmdOrCtrl+Plus")
                    id2
    zoomIn.role <- U2.Case1 MenuItemRole.Zoomin |> Some

    let zoomOut = createMenuItem
                    "Zoom Out"
                    (Some "CmdOrCtrl+-")
                    id2
    zoomOut.role <- U2.Case1 MenuItemRole.Zoomout |> Some

    let resetZoom = createMenuItem
                       "Reset Zoom"
                       (Some "CmdOrCtrl+0")
                       id2
    resetZoom.role <- U2.Case1 MenuItemRole.Resetzoom |> Some

    let items = ResizeArray<MenuItemOptions> [
                    fullscreen
                    menuSeperator
                    zoomIn
                    zoomOut
                    resetZoom
                ]

    makeSubMenu "View" items

let setMainMenu () =
    let template = ResizeArray<MenuItemOptions> [
                        getFileMenu
                        getEditMenu
                        getViewMenu
                    ]
    electron.remote.Menu.setApplicationMenu(electron.remote.Menu.buildFromTemplate(template))