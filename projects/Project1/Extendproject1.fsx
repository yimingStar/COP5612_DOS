#time "on"
#load "Packages.fsx"
#load "ProjectTypes.fsx"
#load "UnitFunctions.fsx"

open System
open System.Diagnostics
open Akka.Actor
open Akka.Configuration
open Akka.FSharp
open ProjectTypes
open UnitFunctions


let proc = Process.GetCurrentProcess()
let realTime = Stopwatch.StartNew()
    
let argv = fsi.CommandLineArgs
printfn "input arguments: %A" (argv) 


let masterHostIP = if argv.Length < 2 then "127.0.0.1" else argv.[1]
let masterActorStr = sprintf "akka.tcp://proj1Master@%s:5566/user/main-actor" masterHostIP 

let hostIP = getLocalIP
let port = if argv.Length < 3 then "5567" else argv.[2] 
let slaveConfig =
    ConfigurationFactory.ParseString(
        @"akka {
            coordinated-shutdown.terminate-actor-system = on
            coordinated-shutdown.run-by-actor-system-terminate = on
            coordinated-shutdown.run-by-actor-system-terminate = on
            actor.provider = remote
            remote.helios.tcp {
                hostname = " + hostIP + "
                port = " + port + "
            }
        }"
    )

let system = System.create "proj1Slave" slaveConfig
let masterController = system.ActorSelection(masterActorStr)
let mutable totalMiners = 0
let mutable bitCoinStr = null
let makeBitCoinString(s:string, sub: string) =
    bitCoinStr <- s + " " + sub

let mainActions (mailbox: Actor<obj>) msg =
    // let sender = mailbox.Sender()
    // printfn "main actor %s, recieve sender %s, msg %s" mailbox.Self.Path.Name (sender.Path.Name.ToString()) (msg.ToString())
    match box msg with
    | :? ArgvInputs as param ->
        // create actor
        let minerInput: MiningInputs = {
            LeadZerosStr = String.replicate param.LeadZeros "0"
            Prefix = param.Prefix
        }
        totalMiners <- param.NumberOfActors
        for i = 1 to totalMiners do
            let name = "mine-local-" + Convert.ToString(i)
            let mineActor = spawn system name (actorOf2 CoinMining)
            mineActor <! minerInput

    | :? BitCoin as param ->
        // found bit coin
        if isNull bitCoinStr then
            try
                printfn "Coin found, send Coin to master"
                makeBitCoinString(param.RandomStr, param.HashedStr)
                masterController <! param
            with ex ->
                printfn "unable to send bit coin %A, start to reconnect" ex
                let masterController = system.ActorSelection(masterActorStr)
                masterController <! param
            
    | :? ActorActions as param ->
        if param.Cmdtype = ActionType.CoinFound then
            printfn "Recieve coin found signal, stop mining"
            for i = 1 to totalMiners do
                let name = "mine-local-" + Convert.ToString(i)
                system.ActorSelection(sprintf "/user/%s" name) <! PoisonPill.Instance
            
            realTime.Stop()
            let cpuTime = proc.TotalProcessorTime.TotalMilliseconds
            printfn "CPU time = %dms" (int64 cpuTime)
            printfn "Absolute time = %dms" realTime.ElapsedMilliseconds
            
            system.ActorSelection(sprintf "/user/main-actor") <! PoisonPill.Instance

        else if param.Cmdtype = ActionType.SendArrive then
            let arriveMessage: ActorActions = {
                Cmdtype = ActionType.RemoteArrives
                Content = sprintf "akka.tcp://proj1Slave@%s:%s/" hostIP port
            }
            masterController <! arriveMessage
    | _ -> 
        printfn "%A" msg

let mainActor = "main-actor"
let mainController = spawn system mainActor (actorOf2 mainActions)
let startMsg = sprintf("%s, %s: %s") getStartProjMsg "start client on ip" hostIP
mainController <! startMsg 

// sending arrive message to master server
let triggerArriveMessage: ActorActions = {
    Cmdtype = ActionType.SendArrive
    Content = "send server a arrive message"
}
mainController <! triggerArriveMessage
System.Console.ReadLine() |> ignore
// system.WhenTerminated.Wait()
