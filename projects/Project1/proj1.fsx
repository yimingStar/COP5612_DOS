

#time "on"
#load "packages.fsx"
#load "ProjectModules.fsx"

open System
open System.Security.Cryptography
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
// let masterConfig =
//     Configuration.parse
//         @"akka {
//             actor.provider = ""Akka.Remote.RemoteActorRefProvider, Akka.Remote""
//             remote.helios.tcp {
//                 hostname = localhost
//                 port = 9000
//             }
//         }"

let system = ActorSystem.Create("proj1Master")
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
    // let sender = mailbox.Sender()
    // printfn "main actor %s, recieve sender %s, msg %s" mailbox.Self.Path.Name (sender.Path.Name.ToString()) (msg.ToString())
    match box msg with
    | :? BitCoin as param ->
        // found bit coin
        if isNull bitCoinStr then
            makeBitCoinString(param.RandomStr, param.HashedStr)

        // stop all actors
        let stopSystemCmd: ActorActions = {
            Cmdtype = ActionType.Stop
            Content = "stop mining"
        }
        system.ActorSelection("/user/*") <! stopSystemCmd
        
    | :? ActorActions as param ->
        // printfn "Actor command received %A" param
        if param.Cmdtype = ActionType.Stop then
            system.Terminate() |> ignore

        else if param.Cmdtype = ActionType.StartLocals then
            for i = 1 to argvParams.NumberOfActors do
                let name = "local-miner-" + Convert.ToString(i)
                let minerInput: MiningInputs = {
                    LeadZeros = argvParams.LeadZeros
                    Prefix = argvParams.Prefix
                    ActorName = name
                }
                let mineActor = spawn system name (actorOf2 CoinMining)
                mineActor <! minerInput

        else if param.Cmdtype = 2 then
            for i = 1 to argvParams.NumberOfActors do
            // remote system connected
            // 1. spawn middleman -> use for termination
                let remoteName = "remote-miner-" + Convert.ToString(i)
                let minerInput: MiningInputs = {
                    LeadZeros = argvParams.LeadZeros
                    Prefix = argvParams.Prefix
                    ActorName = remoteName
                }
                let remoteAddress = Address.Parse("akka.tcp://proj1Slave@localhost:9001")
                let mineRemoteActor = spawne system remoteName <@ actorOf2 CoinMining @> 
                                            [SpawnOption.Deploy (Deploy.None.WithScope(RemoteScope(remoteAddress)))]                        
                mineRemoteActor <! minerInput
    | _ ->  ()

let mainActor = "main-actor"
let mainController = spawn system mainActor (actorOf2 mainActions)

let setInputs(argv: string[]) = 
    let result: ArgvInputs = {
        LeadZeros = leadZerosCheck(argv) 
        NumberOfActors = leadZerosCheck(argv)*100
        Prefix = "yimingchang;"
    }
    argvParams <- result

setInputs(argv)

let startLocalActorsCmd: ActorActions = {
    Cmdtype = ActionType.StartLocals
    Content = "start Local Actors"
}

mainController <! startLocalActorsCmd

system.WhenTerminated.Wait()