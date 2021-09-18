
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

type Inputs = {
    LeadZeros: int
    NumberOfActors: int
    Prefix: string
}

type MiningInputs = {
    LeadZeros: int
    Prefix: string
    ActorName: string
}

let system = ActorSystem.Create("proj1Server", config)

let sha256Hasher = SHA256Managed.Create()
let hashWithSha256(originalStr: string) =
    let hashedBytes = originalStr |> System.Text.Encoding.UTF8.GetBytes |> (new SHA256Managed()).ComputeHash
    let hashedString = hashedBytes |> Array.map (fun (x : byte) -> System.String.Format("{0:X2}", x)) |> String.concat System.String.Empty
    hashedString.ToLower()
type CoinMining = 
    inherit Actor
    override x.OnReceive message =
        match message with
        | :? MiningInputs as param ->
            let prefix = param.Prefix
            let leadZero = param.LeadZeros
            let mutable checkString = prefix + Guid.NewGuid().ToString()
            let hashedString = checkString |> hashWithSha256
            printfn "%s is hashed into %s" checkString hashedString
        | _ ->  failwith "unknown mining inputs"

type ActorGenerator = // Class
    // reference https://doc.akka.io/docs/akka/2.4/java/untyped-actors.html
    // F# type using https://docs.microsoft.com/en-us/dotnet/fsharp/language-reference/inheritance
    // match expression https://docs.microsoft.com/en-us/dotnet/fsharp/language-reference/match-expressions
    // Type symbols and operators https://docs.microsoft.com/en-us/dotnet/fsharp/language-reference/symbol-and-operator-reference/ 
    inherit Actor
    override x.OnReceive message =
        match message with
        | :? Inputs as param -> 
            let totalActors = param.NumberOfActors * param.LeadZeros
            // let productMode = if param.ModeStr = "0" then false else true
            // create actors  
            for i = 1 to totalActors do
                // spawn the actors
                let name = "remote-actor-" + Convert.ToString(i)
                let remoteActor = system.ActorOf(Props(typedefof<CoinMining>), name)
                let minerInput: MiningInputs = {
                    LeadZeros = param.LeadZeros
                    Prefix = param.Prefix
                    ActorName = name
                }
                remoteActor <! minerInput
            
        | _ ->  failwith "unknown input"

let printStart courseInfo projectInfo =
    sprintf "Course %s %s" courseInfo projectInfo

// start progect1 program
let courseInfo = "[COP5612] DOS"
let projectInfo = "Project 1"
let startMsg = printStart courseInfo projectInfo
let lang = "F#" 
printfn "Start program of %s with %s" startMsg lang

let argv = fsi.CommandLineArgs
printfn "input arguments: %A" (argv) 

let leadZerosCheck(argv: string[]) = 
    let mutable validLeadZeros = 0
    try
        validLeadZeros <- (argv.[1] |> int)
        if validLeadZeros <= 0 then
            failwith "Invalid number of leadZero"
    with 
        | :? System.IndexOutOfRangeException as ex -> printfn "Exception! %A " (ex.Message)
    validLeadZeros

let InputParams: Inputs = {
    LeadZeros = leadZerosCheck(argv) 
    NumberOfActors = 1
    Prefix = "yimingchang;"
}
// Akka props https://doc.akka.io/api/akka/current/akka/actor/Props.html
let createActors = system.ActorOf(Props(typedefof<ActorGenerator>), "actor-generator")
createActors <! InputParams
