
#time "on"
#load "packages.fsx"

open System
open System.Security.Cryptography
open Akka.Actor
open Akka.Configuration
open Akka.FSharp

// @brief Program solving square sequence problem
// Given 2 input arguments K, N, where K >= N
// return first number of the perfect square sequence

// Method - Starting iterate through 1 ~ K as starting points, each sequence will be size of N.
// Time theta(K*N)  
let config =
    Configuration.parse
        @"akka {
            log-config-on-start : on
            stdout-loglevel : DEBUG
            loglevel : ERROR
            actor {
                provider = ""Akka.Remote.RemoteActorRefProvider, Akka.Remote""
                debug : {
                    receive : on
                    autoreceive : on
                    lifecycle : on
                    event-stream : on
                    unhandled : on
                }
                remote.helios.tcp {
                    hostname = localhost
                    port = 5566
                }
            }
        }"

type ArgvInputs = {
    LeadZeros: int
    NumberOfActors: int
    Prefix: string
}

type MiningInputs = {
    LeadZeros: int
    Prefix: string
    ActorName: string
}

type BitCoin = {
    RandomStr: string
    HashedStr: string
}

// ActionsType
let StopType = 0
type ActorActions = {
    Cmdtype: int
    Content: string
}

let printStart courseInfo projectInfo =
    sprintf "Course %s %s" courseInfo projectInfo
// start progect1 program
let courseInfo = "[COP5612] DOS"
let projectInfo = "Project 1"
let startMsg = printStart courseInfo projectInfo
let lang = "F#" 

printfn "Start program of %s with %s" startMsg lang

let system = ActorSystem.Create("proj1Server", config)
let mutable bitCoinStr = null 
let argv = fsi.CommandLineArgs
printfn "input arguments: %A" (argv) 

// Checking functions
let leadZerosCheck(argv: string[]) = 
    let mutable validLeadZeros = 0
    try
        validLeadZeros <- (argv.[1] |> int)
        if validLeadZeros <= 0 then
            failwith "Invalid number of leadZero"
    with 
        | :? System.IndexOutOfRangeException as ex -> printfn "Exception! %A " (ex.Message)
    validLeadZeros

// Hashed Functions
let hashWithSha256(originalStr: string) =
    let hashedBytes = originalStr |> System.Text.Encoding.UTF8.GetBytes |> (new SHA256Managed()).ComputeHash
    let hashedString = hashedBytes |> Array.map (fun (x : byte) -> System.String.Format("{0:X2}", x)) |> String.concat System.String.Empty
    hashedString.ToLower()

// Miners actor function
let CoinMining(mailbox: Actor<obj>) msg =
    let rec miningLoop() =
        let sender = mailbox.Sender()
        // printfn "actor %s, recieve sender %s, msg %s" mailbox.Self.Path.Name (sender.Path.Name.ToString()) (msg.ToString())
        match box msg with
        | :? MiningInputs as param ->
            let prefix = param.Prefix
            let checkString = String.replicate param.LeadZeros "0" // use to find the bitCoin
            let mutable randomString = prefix + Guid.NewGuid().ToString()
            let hashedString = randomString |> hashWithSha256
            if hashedString.StartsWith(checkString) then
                let coin: BitCoin = {
                    RandomStr = randomString
                    HashedStr = hashedString
                }
                sender <! coin
            else
                miningLoop()

        | :? ActorActions as param ->
            if param.Cmdtype = StopType then ()
        | _ ->  (failwith "unknown mining inputs")
    
    miningLoop()

let makeBitCoinString(s:string, sub: string) =
    bitCoinStr <- s + " " + sub
    printfn "%s" bitCoinStr

let mainActions (mailbox: Actor<obj>) msg =
    let sender = mailbox.Sender()
    // printfn "main actor %s, recieve sender %s, msg %s" mailbox.Self.Path.Name (sender.Path.Name.ToString()) (msg.ToString())
    match box msg with
    | :? ArgvInputs as param ->
        // create actor
        for i = 1 to param.NumberOfActors do
        let name = "mine-actor-" + Convert.ToString(i)
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
        let stopCmd: ActorActions = {
            Cmdtype = StopType
            Content = "stop mining"
        }
        system.ActorSelection("/user/*") <! stopCmd
        
    | :? ActorActions as param ->
        if param.Cmdtype = 0 then
            system.Terminate() |> ignore
    | _ ->  ()
    

let argvParams: ArgvInputs = {
    LeadZeros = leadZerosCheck(argv) 
    NumberOfActors = leadZerosCheck(argv)*1000
    Prefix = "yimingchang;"
}

let mainActor = "main-actor"
let mainController = spawn system mainActor (actorOf2 mainActions)
mainController <! argvParams 

system.WhenTerminated.Wait()