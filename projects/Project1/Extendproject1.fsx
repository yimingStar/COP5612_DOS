#time "on"
#load "Packages.fsx"
#load "ProjectTypes.fsx"
#load "UnitFunctions.fsx"

open System
open Akka.Actor
open Akka.Configuration
open Akka.FSharp
open ProjectTypes
open UnitFunctions

let hostIP = getLocalIP
let port = "5568" 
let slaveConfig =
    ConfigurationFactory.ParseString(
            @"akka {
            actor.provider = remote
            remote.helios.tcp {
                hostname = " + hostIP + "
                port = " + port + "
            }
        }")

let system = System.create "proj1Slave" slaveConfig

let mainActions (mailbox: Actor<obj>) msg =
    // let sender = mailbox.Sender()
    // printfn "main actor %s, recieve sender %s, msg %s" mailbox.Self.Path.Name (sender.Path.Name.ToString()) (msg.ToString())
    match box msg with
    | :? ActorActions as param ->
        if param.Cmdtype = ActionType.Stop then ()
            // system.Terminate().Start() |> ignore
    | _ -> 
        printfn "%A" msg

let mainActor = "main-actor"
let mainController = spawn system mainActor (actorOf2 mainActions)
let startMsg = sprintf("%s, %s: %s") getStartProjMsg ",start client on ip" hostIP
mainController <! startMsg 

let arriveMessage: ActorActions = {
    Cmdtype = ActionType.RemoteArrives
    Content = sprintf "akka.tcp://proj1Slave@%s:%s/" hostIP port
}
// sending arrive message to master server
let argv = fsi.CommandLineArgs
printfn "input arguments: %A" (argv) 

let masterHostIP = if argv.Length < 2 then "127.0.0.1" else argv.[1]
let masterActorStr = sprintf "akka.tcp://proj1Master@%s:5566/user/main-actor" masterHostIP 
let masterServer = system.ActorSelection(masterActorStr)
masterServer <! arriveMessage
System.Console.ReadLine() |> ignore
