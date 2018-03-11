(* 
    High Level Programming @ Imperial College London # Spring 2018
    Project: A user-friendly ARM emulator in F# and Web Technologies ( Github Electron & Fable Compliler )
    Contributors: Angelos Filos
    Module: Main
    Description: Electron Renderer Process - Script to executed after `index.html` is loaded.
*)

module Renderer

open Fable.Core
open Fable.Core.JsInterop
open Fable.Import
open Fable.Import.Electron
open Node.Exports
open Fable.PowerPack

open Fable.Import.Browser

// open DevTools to see the message
// Menu -> View -> Toggle Developer Tools
Browser.console.log "Hi from the renderer.js" |> ignore

open Ref
open Update
open Emulator

// TODO: Delete this piece of shit
let testMemory = Map.ofList [
                        0x0u, 0xAABBCCDDu;
                        0x4u, 0x11223344u;
                        0x8u, 0x11111111u;
                        0x100u, 0x22222222u;
                        0x104u, 0xAABBCCDDu;
                    ]

/// Access to `Emulator` project
let dummyVariable = Emulator.Common.A

// Attach a click event on each of the map elements to a function f
// Which accepts the map element as an argument
let mapClickAttacher map (refFinder : 'a -> HTMLElement) f =
    let attachRep ref = (refFinder ref).addEventListener_click(fun _ -> f ref)
    map
    |> Map.toList
    |> List.map (fst >> attachRep)
    |> ignore

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

let setMainMenu () =
    let template = ResizeArray<MenuItemOptions> [
                        getFileMenu
                    ]
    electron.remote.Menu.setApplicationMenu(electron.remote.Menu.buildFromTemplate(template))

/// Initialization after `index.html` is loaded
let init () =

    // Show the body once we are ready to go!
    document.getElementById("vis-body").classList.remove("invisible")

    // TODO: Implement actions for the buttons
    Ref.explore.addEventListener_click(fun _ ->
        openFile ()
    )
    Ref.save.addEventListener_click(fun _ ->
        saveFile ()
    )
    Ref.run.addEventListener_click(fun _ ->
        setTheme "vs-dark" |> ignore
        Browser.window.alert "NotImplemented :|"
    )
    // just for fun!
    (Ref.register 0).addEventListener_click(fun _ ->
        Browser.console.log "register R0 changed!" |> ignore
        Update.setRegister 0 (uint32 (System.Random().Next 1000))
    )
    (Ref.flag "N").addEventListener_click(fun _ ->
        Browser.console.log "flag N changed!" |> ignore
        Update.flag "N" true
    )

    mapClickAttacher repToId Ref.representation (fun rep ->
        Browser.console.log (sprintf "Representation changed to %A" rep) |> ignore
        setRepresentation rep |> ignore
        updateMemory ()
    )

    mapClickAttacher viewToIdTab Ref.viewTab (fun view ->
        Browser.console.log (sprintf "View changed to %A" view) |> ignore
        setView view
    )

    (Ref.byteViewBtn).addEventListener_click(fun _ ->
        Browser.console.log "Toggling byte view" |> ignore
        toggleByteView ()
        updateMemory ()
    )

    (Ref.newFileTab).addEventListener_click(fun _ ->
        Browser.console.log "Creating a new file tab" |> ignore
        createFileTab ()
    )

    memoryMap <- testMemory
    updateMemory ()

    // Create an empty tab to start with
    createFileTab ()

setMainMenu ()

let handleMonacoReady (_: Event) = init ()

let listener: U2<EventListener, EventListenerObject> = !^handleMonacoReady

document.addEventListener("monaco-ready", listener)

