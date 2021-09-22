

#time "on"
#load "packages.fsx"
#load "ProjectTypes.fsx"
#load "UnitFunctions.fsx"

open System
open Akka.Actor
open Akka.Configuration
open Akka.FSharp
open ProjectTypes
open UnitFunctions

let hostIP = getLocalIP
let port = "5566" 
let masterConfig =
    ConfigurationFactory.ParseString(
            @"akka {
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
    printfn "%s" bitCoinStr

let mainActions (mailbox: Actor<obj>) msg =
    let sender = mailbox.Sender()
    match box msg with
    | :? ArgvInputs as param ->
        // create actor
        for i = 1 to argvParams.NumberOfActors do
            let name = "mine-local-" + Convert.ToString(i)
            let mineActor = spawn system name (actorOf2 CoinMining)
            let minerInput: MiningInputs = {
                LeadZeros = param.LeadZeros
                Prefix = param.Prefix
                ActorName = name
            }
            mineActor <! minerInput

    | :? BitCoin as param ->
        // found bit coin
        if isNull bitCoinStr then
            makeBitCoinString(param.RandomStr, param.HashedStr)
            let stopSystemCmd: ActorActions = {
                Cmdtype = ActionType.Stop
                Content = "stop mining"
            }
            system.ActorSelection("/user/miner*") <! stopSystemCmd

            // printfn "%A" clientSet
            for clientHost in Set.toList(clientSet) do
                let clientMainActor = system.ActorSelection(clientHost + "user/main-actor")
                clientMainActor <! stopSystemCmd
            
            system.ActorSelection("/user/main-actor") <! stopSystemCmd

    | :? ActorActions as param ->
        // printfn "Actor command received %A" param
        if param.Cmdtype = ActionType.Stop then ()
            // system.Terminate() |> ignore

        else if param.Cmdtype = ActionType.StartLocal then 
            for i = 1 to argvParams.NumberOfActors do
            let name = "miner-local-actor_" + Convert.ToString(i)
            let mineActor = spawn system name (actorOf2 CoinMining)
            let minerInput: MiningInputs = {
                LeadZeros = argvParams.LeadZeros
                Prefix = argvParams.Prefix
                ActorName = name
            }
            mineActor <! minerInput

        else if param.Cmdtype = ActionType.RemoteArrives then
            printfn "Remote node [%s] arrived, start spawning " (param.Content)
            for i = 1 to argvParams.NumberOfActors do
                let remoteName = "miner-remote-" + clientSet.Count.ToString() + "-actor-" + Convert.ToString(i)
                let minerInput: MiningInputs = {
                    LeadZeros = argvParams.LeadZeros
                    Prefix = argvParams.Prefix
                    ActorName = remoteName
                }
                
                clientSet <- clientSet.Add(param.Content)
                let remoteAddress = Address.Parse(param.Content)
                let mineRemoteActor = spawne system remoteName <@ actorOf2 CoinMining @> [SpawnOption.Deploy (Deploy.None.WithScope(RemoteScope(remoteAddress)))]
                mineRemoteActor <! minerInput
                
    | _ ->  printfn "%A" msg


let mainActor = "main-actor"
let mainController = spawn system mainActor (actorOf2 mainActions)

let setInputs(argv: string[]) = 
    let inputs: ArgvInputs = {
        LeadZeros = leadZerosCheck(argv) 
        NumberOfActors = 5
        Prefix = "yimingchang;"
    }
    argvParams <- inputs

setInputs(argv)

let StartLocalActors: ActorActions = {
    Cmdtype = ActionType.StartLocal
    Content = sprintf "akka.tcp://proj1Master@%s:%s/" hostIP port
}

let startMsg = sprintf("%s, %s: %s") getStartProjMsg ",start server on ip" hostIP
mainController <! startMsg 
mainController <! StartLocalActors
System.Console.ReadLine() |> ignore