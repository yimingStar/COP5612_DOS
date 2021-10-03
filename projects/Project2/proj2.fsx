#time "on"
#load "Packages.fsx"
#load "ProjectTypes.fsx"

open System
open System.Diagnostics
open Akka.Actor
open Akka.Configuration
open Akka.FSharp
open ProjectTypes
open UnitFunctions

let mutable argvParams: ArgvInputs = {
    NumbersOfNodes = 0
    Topology = ""
    GossipAlgo = ""
}

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

let system = System.create "proj2Master" masterConfig


let inputCheck(argv: string[]) = 
    try
        if argv.Length < 4 then
            failwith "Invalid arg variables"
    with 
        | :? System.IndexOutOfRangeException as ex -> printfn "Exception! %A " (ex.Message)

let argv = fsi.CommandLineArgs
printfn "input arguments: %A" (argv) 

inputCheck(argv)
let setInputs(argv: string[]) = 
    let inputs: ArgvInputs = {
        NumbersOfNodes = argv[1]
        Topology = argv[2]
        GossipAlgo = argv[3]
    }
    argvParams <- inputs
    
setInputs(argv)

let createNetwork(input) =
    match box input with
    | :? ArgvInputs with input ->
        printfn "%A"
    | _ ->  failwith "Invalid input variables to build a network"

createNetwork(argvParams)

System.Console.ReadLine() |> ignore