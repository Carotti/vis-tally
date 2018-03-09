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
open Electron
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

/// Initialization after `index.html` is loaded
let init () =
    // TODO: Implement actions for the buttons
    Ref.explore.addEventListener_click(fun _ ->
        Browser.console.log "Code updated"
        Update.code("mov r7, #5")
    )
    Ref.save.addEventListener_click(fun _ ->
        Browser.window.alert (sprintf "%A" (Ref.code ()))
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

    // Just for testing memory visualisation
    memoryMap <- testMemory
    updateMemory ()

init() |> ignore