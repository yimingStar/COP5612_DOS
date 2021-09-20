#time "on"
#load "packages.fsx"
#load "ProjectModules.fsx"

open System
open Akka.Actor
open Akka.Configuration
open Akka.FSharp
open ProjectModules

let config =
    Configuration.parse
        @"akka {
            actor.provider = ""Akka.Remote.RemoteActorRefProvider, Akka.Remote""
            remote.helios.tcp {
                hostname = localhost
                port = 9001
            }
        }"

let system = ActorSystem.Create("proj1Slave", config)

let mainActions (mailbox: Actor<obj>) msg =
    // let sender = mailbox.Sender()
    // printfn "main actor %s, recieve sender %s, msg %s" mailbox.Self.Path.Name (sender.Path.Name.ToString()) (msg.ToString())
    match box msg with  
    | :? ActorActions as param ->
        // printfn "Actor command received %A" param
        if param.Cmdtype = ActionType.Stop then
            system.Terminate() |> ignore
    | _ ->  ()

let mainActor = "main-actor"
let mainController = spawn system mainActor (actorOf2 mainActions)
mainController <! "starts"

let arriveMessage: ActorActions = {
    Cmdtype = ActionType.RemoteArrives
    Content = "akka.tcp://proj1Slave@localhost:9001"
}

// sending arrive message to master server
let masterServer = system.ActorSelection("akka.tcp://proj1Master@localhost:9000/user/main-actor")
masterServer <! arriveMessage

system.WhenTerminated.Wait()