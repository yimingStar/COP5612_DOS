

#time "on"
#load "packages.fsx"
#load "ProjectModules.fsx"

open System
open Akka.Actor
open Akka.Configuration
open Akka.FSharp
open ProjectModules

let printStart courseInfo projectInfo =
    sprintf "Course %s %s" courseInfo projectInfo
// start progect1 program
let courseInfo = "[COP5612] DOS"
let projectInfo = "Project 1"
let startMsg = printStart courseInfo projectInfo
let lang = "F#" 

printfn "Start program of %s with %s" startMsg lang

let processIP = "localhost"
let masterConfig =
    ConfigurationFactory.ParseString(
            @"akka {
            actor.provider = remote
            remote.helios.tcp {
                hostname = " + processIP + "
                port = 5566
            }
        }")

let system = System.create "proj1Master" masterConfig
let mutable clientList = []
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
    // printfn "main actor %s, recieve sender %s, msg %s" mailbox.Self.Path.Name (sender.Path.Name.ToString()) (msg.ToString())
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

        // stop all actors
        let stopSystemCmd: ActorActions = {
            Cmdtype = ActionType.Stop
            Content = "stop mining"
        }
        for clientHost in clientList do
            let clientMainActor = system.ActorSelection(clientHost + "/user/main-actor")
            clientMainActor <! stopSystemCmd
        
        system.ActorSelection("/user/mine*") <! stopSystemCmd
        system.ActorSelection("/user/main-actor") <! stopSystemCmd
        
    | :? ActorActions as param ->
        // printfn "Actor command received %A" param
        if param.Cmdtype = ActionType.Stop then
            system.Terminate() |> ignore

        else if param.Cmdtype = ActionType.StartLocal then 
            for i = 1 to argvParams.NumberOfActors do
            let name = "local-miner-" + Convert.ToString(i)
            let mineActor = spawn system name (actorOf2 CoinMining)
            let minerInput: MiningInputs = {
                LeadZeros = argvParams.LeadZeros
                Prefix = argvParams.Prefix
                ActorName = name
            }
            mineActor <! minerInput

        else if param.Cmdtype = ActionType.RemoteArrives then
            printfn "get remote message %s, start spawning %d with argv %A" (msg.ToString()) argvParams.NumberOfActors (argvParams)
            for i = 1 to argvParams.NumberOfActors do
                let remoteName = "mine-remote-" + Convert.ToString(i)
                let minerInput: MiningInputs = {
                    LeadZeros = argvParams.LeadZeros
                    Prefix = argvParams.Prefix
                    ActorName = remoteName
                }
                
                let remoteAddress = Address.Parse(param.Content)
                let mineRemoteActor = spawne system remoteName <@ actorOf2 CoinMining @> [SpawnOption.Deploy (Deploy.None.WithScope(RemoteScope(remoteAddress)))]                        
                mineRemoteActor <! minerInput

                let newList = clientList @ [param.Content]
                clientList <- newList

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
    Content = "akka.tcp://proj1Master@" + processIP + ":5566/"
}
mainController <! "starts master server"
mainController <! StartLocalActors
system.WhenTerminated.Wait()