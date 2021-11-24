open System
open Akka.Actor
open Akka.Configuration
open Akka.FSharp
open FSharp.Data
open FSharp.Json
open ServerTypes

let hostIP = "localhost"
let port = "5566" 
let config =
    ConfigurationFactory.ParseString(
            @"akka {
                actor.provider = remote
                remote.helios.tcp {
                    hostname = " + hostIP + "
                    port = " + port + "
                }
            }"
        )

let serverSystem = System.create "twitterServer" (config)
let userDataPath = __SOURCE_DIRECTORY__ + "./data/users.json"
let JsonConfig = JsonConfig.create(allowUntyped = true)
let userData = JsonValue.Load(userDataPath)

let serverEngine (serverMailbox:Actor<String>) =
    let mutable selfActor = select ("") serverSystem
    let rec loop () = actor {
        let! (msg: String) = serverMailbox.Receive()
        let actionObj = Json.deserializeEx<MessageType> JsonConfig msg
        printfn "receive obj: %A" actionObj
        // match msg with

        // | _ -> printfn "[Invalid Action] server no action match %s" msg
        return! loop()
    }
    loop()

[<EntryPoint>]
let main argv =
    // create server main actor
    let serverMainActor = spawn serverSystem "serverEngine" serverEngine
    serverMainActor <! START
    System.Console.ReadLine() |> ignore
    0 // return an integer exit code