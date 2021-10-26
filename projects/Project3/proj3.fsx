#time "on"
#load "Packages.fsx"
#load "ProjectTypes.fsx"


open System
open System.Diagnostics
open System.Security.Cryptography
open Akka.Actor
open Akka.Configuration
open Akka.FSharp
open ProjectTypes

let mutable systemParams: SystemParams = {
    NumOfNodes = 0
    NumOfRequest = 0
    NumOfIdentifier = 0
}

let hostIP = "localhost"
let port = "5567" 
let config =
    ConfigurationFactory.ParseString(
            @"akka {
                actor.provider = remote
                remote.helios.tcp {
                    hostname = " + hostIP + "
                    port = " + port + "
                }
            }"
        )

// System flow 
// 1. Build the Chord ring
// 2. Input set the nodes on ring
// 3. Assign the key values on the nodes
// 4. build the finger lookup table
// 5. every node start to request for K key-value (Random?)

// additional adding and failing nodes
// 6. adding and failing few servers

let system = System.create "proj3Master" config

let hashWithShaOne(originalStr: string) =
    let hashedBytes = originalStr |> System.Text.Encoding.UTF8.GetBytes |> (new SHA1Managed()).ComputeHash
    let hashedInt = BitConverter.ToUInt16(hashedBytes, 0) |> int
    hashedInt


let matchNodeToRing(nodeName: string) = 
    let hashedInt = hashWithShaOne(nodeName)
    let identifier = hashedInt % systemParams.NumOfIdentifier
    // printfn "nodeName %s, hash %d into identifier: %d " nodeName hashedInt identifier
    identifier


let setIdentifier(numberOfNode: int) = 
    // The identifier length m must be large enough to make the probability of two nodes or keys hashing to the same identifier negligible. 
    let mutable numOfIdentifier = 64
    if(numberOfNode >= 20 && numberOfNode < 64) then
        numOfIdentifier <- 128
    else if(numberOfNode >= 64 && numberOfNode < 128) then
        numOfIdentifier <- 256
    else if(numberOfNode >= 128) then
        numOfIdentifier <- 1024
    numOfIdentifier


let setInputs(argv: string[])  = 
    let setParams: SystemParams = {
        NumOfNodes = argv.[1] |> int
        NumOfRequest = argv.[2] |> int
        NumOfIdentifier = setIdentifier(argv.[1] |> int)
    }
    setParams


let NodeFunction (nodeMailbox:Actor<NodeActions>) =
    let mutable id = -1
    let rec loop () = actor {
        let! (msg: NodeActions) = nodeMailbox.Receive()
        let sender = nodeMailbox.Sender()
        match msg with
        | INIT ->
            let nodeName = nodeMailbox.Self.Path.Name
            id <- matchNodeToRing(nodeName)
        | STORE(key:string, value:string) -> ()
        return! loop()
    }
    loop()


let createNetwork(param) =
    match box param with
    | :? SystemParams as param ->
        for i = 1 to param.NumOfNodes do
            let name = "Server-" + Convert.ToString(i)
            let networkNode = spawn system name NodeFunction
            networkNode <! INIT

    | _ ->  failwith "Invalid input variables to build a network"


let argv = fsi.CommandLineArgs
printfn "input arguments: %A" (argv) 

systemParams <- setInputs(argv)
createNetwork(systemParams)

System.Console.ReadLine() |> ignore