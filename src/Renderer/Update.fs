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
open Fable.Import.Browser

open Ref

// The current number representation being used
let mutable currentRep = Hex
let mutable currentView = Registers

let mutable byteView = false

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
    | Dec -> (sprintf "%d")
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
let code (text: string) =
    window?code?setValue(text)

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