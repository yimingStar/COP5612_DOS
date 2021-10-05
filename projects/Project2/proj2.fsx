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

let mutable argvParams = ArgvInputs(0, "", "")

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

let proc = Process.GetCurrentProcess()
let realTime = Stopwatch.StartNew()
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
    argvParams <- ArgvInputs(argv.[1] |> int, argv.[2], argv.[3])
    
setInputs(argv)

let createLineNetwork(systemParams: ArgvInputs) =
    for i = 1 to systemParams.NumberOfNodes do
        let name = systemParams.Topology + "-" + Convert.ToString(i)
        let networkNode = spawn system name (actorOf2 NodeFunctions)
        let nodeParams = NodeParams(i, systemParams, 2, i, 1)
        networkNode <! nodeParams

let createNetwork(param) =
    match box param with
    | :? ArgvInputs as param ->
        if param.Topology = TopologyType.LINE then
            createLineNetwork(param)
    | _ ->  failwith "Invalid input variables to build a network"

createNetwork(argvParams)

let sendMessage(systemParams: ArgvInputs, content: string, startIdx: int) =
    let gossipMsg: GossipMsg = {
        Content = content
    }
    let startNodesName = systemParams.Topology + "-" + Convert.ToString(startIdx)
    system.ActorSelection(sprintf "/user/%s" startNodesName) <! gossipMsg

sendMessage(argvParams, "test", 1)

realTime.Stop()
let cpuTime = proc.TotalProcessorTime.TotalMilliseconds
printfn "CPU Time = %dms" (int64 cpuTime)
printfn "Real Time = %dms" realTime.ElapsedMilliseconds

System.Console.ReadLine() |> ignore