#time "on"
#load "packages.fsx"
#load "ProjectModules.fsx"

open System
open Akka.Actor
open Akka.Configuration
open Akka.FSharp
open ProjectModules

let slaveConfig =
    ConfigurationFactory.ParseString(
            @"akka {
            actor.provider = remote
            remote.helios.tcp {
                hostname = localhost
                port = 5567
            }
        }")

let system = System.create "proj1Slave" slaveConfig

let mainActions (mailbox: Actor<obj>) msg =
    // let sender = mailbox.Sender()
    // printfn "main actor %s, recieve sender %s, msg %s" mailbox.Self.Path.Name (sender.Path.Name.ToString()) (msg.ToString())
    match box msg with
    | :? ActorActions as param ->
        if param.Cmdtype = ActionType.Stop then
            system.Terminate() |> ignore
    | _ -> 
        printfn "%A" msg

let mainActor = "main-actor"
let mainController = spawn system mainActor (actorOf2 mainActions)

let arriveMessage: ActorActions = {
    Cmdtype = ActionType.RemoteArrives
    Content = "akka.tcp://proj1Slave@localhost:5567/"
}
// sending arrive message to master server
let masterServer = system.ActorSelection("akka.tcp://proj1Master@localhost:5566/user/main-actor")
masterServer <! arriveMessage
system.WhenTerminated.Wait()