#time "on"
#load "Packages.fsx"
#load "ProjectTypes.fsx"


open System
open System.Diagnostics
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
let setIdentifier(numberOfNode: int) = 
    let mutable numOfIdentifier = 64
    if(numberOfNode >= 20 && numberOfNode < 64) then
        numOfIdentifier <- 128
    else if(numberOfNode >= 64 && numberOfNode < 128) then
        numOfIdentifier <- 256
    else
        numOfIdentifier <- 1024
    numOfIdentifier

let setInputs(argv: string[])  = 
    let setParams: SystemParams = {
        NumOfNodes = argv.[1] |> int
        NumOfRequest = argv.[2] |> int
        NumOfIdentifier = setIdentifier(argv.[1] |> int)
    }
    setParams

let system = System.create "proj3Master" config
let argv = fsi.CommandLineArgs
printfn "input arguments: %A" (argv) 

systemParams = setInputs(argv)
System.Console.ReadLine() |> ignore
// The identifier length m must be large enough to make the probability of two nodes or keys hashing to the same identifier negligible. 