module MenuBar

open Fable.Core
open Fable.Core.JsInterop
open Fable.Import
open Fable.Import.Electron

open Update

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
                    (fun _ _ -> deleteFileTab currentFileTabId)

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

    let fileMenu = createEmpty<MenuItemOptions>
    fileMenu.label <- Some "File"
    fileMenu.submenu <- items |> U2.Case2 |> Some

    fileMenu

let getEditMenu =
    let undo = createMenuItem
                "Undo"
                (Some "CmdOrCtrl+Z")
                (fun _ _ -> ())
    undo.role <- U2.Case1 MenuItemRole.Undo |> Some

    let redo = createMenuItem
                "Redo"
                (Some "CmdOrCtrl+Y")
                (fun _ _ -> ())
    redo.role <- U2.Case1 MenuItemRole.Redo |> Some

    let cut = createMenuItem
                "Cut"
                (Some "CmdOrCtrl+X")
                (fun _ _ -> ())
    cut.role <- U2.Case1 MenuItemRole.Cut |> Some

    let copy = createMenuItem
                "Copy"
                (Some "CmdOrCtrl+C")
                (fun _ _ -> ())
    copy.role <- U2.Case1 MenuItemRole.Copy |> Some
    
    let paste = createMenuItem
                    "Paste"
                    (Some "CmdOrCtrl+V")
                    (fun _ _ -> ())
    paste.role <- U2.Case1 MenuItemRole.Paste |> Some
    
    let preferences = createMenuItem
                        "Preferences"
                        Option.None
                        (fun _ _ -> ())

    let items = ResizeArray<MenuItemOptions> [
                    undo
                    redo
                    menuSeperator
                    cut
                    copy
                    paste
                    menuSeperator
                    preferences
                ]

    let editMenu = createEmpty<MenuItemOptions>
    editMenu.label <- Some "Edit"
    editMenu.submenu <- items |> U2.Case2 |> Some

    editMenu

let setMainMenu () =
    let template = ResizeArray<MenuItemOptions> [
                        getFileMenu
                        getEditMenu
                    ]
    electron.remote.Menu.setApplicationMenu(electron.remote.Menu.buildFromTemplate(template))