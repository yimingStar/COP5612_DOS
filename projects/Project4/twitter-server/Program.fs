open System
open System.Security.Cryptography
open Akka.Actor
open Akka.Configuration
open Akka.FSharp
open ServerTypes

let hostIP = "localhost"
let port = "9988" 
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

let serverSystem = System.create "twitter-server" (config)

// try
//     let serverSystem = System.create "twitter-server" (config)
//     ()
// with
//     | :? System.Exception as ex -> printfn "Creare server system failed, ex:%A"(ex.Message)

let serverFunction (serverMailbox:Actor<ServerActions>) =
    let mutable selfActor = select ("") serverSystem
    let rec loop () = actor {
        let! (msg: ServerActions) = serverMailbox.Receive()
        match msg with
        | START -> printfn "Server Start"
        return! loop()
    }
    loop()

[<EntryPoint>]
let main argv =
    // create server main actor
    let serverMainActor = spawn serverSystem "main-cordinator" serverFunction
    serverMainActor <! START
    System.Console.ReadLine() |> ignore
    0 // return an integer exit code