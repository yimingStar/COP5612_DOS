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

let hostIP = getLocalIP
let port = "5566" 
let masterConfig =
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

let system = System.create "proj1Master" masterConfig
let mutable clientSet = Set.empty
let mutable bitCoinStr = null
let mutable argvParams: ArgvInputs = {
    LeadZeros = 0
    NumberOfActors = 0
    Prefix=""
}

let argv = fsi.CommandLineArgs
printfn "input arguments: %A" (argv) 
let mutable totalMiners = 0
// function checks input
let leadZerosCheck(argv: string[]) = 
    let mutable validLeadZeros = 0
    try
        validLeadZeros <- (argv.[1] |> int)
        if validLeadZeros <= 0 then
            failwith "Invalid number of leadZero"
    with 
        | :? System.IndexOutOfRangeException as ex -> printfn "Exception! %A " (ex.Message)
    validLeadZeros

let makeBitCoinString(s:string, sub: string) =
    bitCoinStr <- s + " " + sub
    printfn "\n%s\n" bitCoinStr

let mainActions (mailbox: Actor<obj>) msg =
    let sender = mailbox.Sender()
    match box msg with
    | :? ArgvInputs as param ->
        // create local actors
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
            makeBitCoinString(param.RandomStr, param.HashedStr)
            let coinFindCmd: ActorActions = {
                Cmdtype = ActionType.CoinFound
                Content = "Coin Found"
            }
            
            // broadcasting found msg
            system.ActorSelection("/user/main-actor") <! coinFindCmd
            for clientHost in Set.toList(clientSet) do
                printfn "broadcast Coin found message to %A" clientHost
                let clientMainActor = system.ActorSelection(clientHost + "user/main-actor")
                clientMainActor <! coinFindCmd
            
    | :? ActorActions as param ->
        if param.Cmdtype = ActionType.CoinFound then
            printfn "Recieve coin found signal, stop mining"
            for i = 1 to totalMiners do
                let name = "mine-local-" + Convert.ToString(i)
                system.ActorSelection(sprintf "/user/%s" name) <! PoisonPill.Instance
            
            realTime.Stop()
            let cpuTime = proc.TotalProcessorTime.TotalMilliseconds
            printfn "CPU Time = %dms" (int64 cpuTime)
            printfn "Real Time = %dms" realTime.ElapsedMilliseconds
            
            system.ActorSelection(sprintf "/user/main-actor") <! PoisonPill.Instance
            // system.Terminate() |> ignore

        else if param.Cmdtype = ActionType.RemoteArrives then
            printfn "Remote node [%s] arrived, pass the params to client " (param.Content)
            clientSet <- clientSet.Add(param.Content)
            sender <! argvParams
                
    | _ ->  printfn "%A" msg


let mainActor = "main-actor"
let mainController = spawn system mainActor (actorOf2 mainActions)

let setInputs(argv: string[]) = 
    let inputs: ArgvInputs = {
        LeadZeros = leadZerosCheck(argv) 
        NumberOfActors = 4
        Prefix = "yimingchang;"
    }
    argvParams <- inputs

setInputs(argv)

let startMsg = sprintf("%s, %s: %s") getStartProjMsg "start server on ip" hostIP
mainController <! startMsg 
mainController <! argvParams
System.Console.ReadLine() |> ignore
// system.WhenTerminated.Wait()
