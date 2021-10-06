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
    let mutable numberOfNodes = argv.[1] |> int
    if argv.[2] = TopologyType.ThreeD || argv.[2] = TopologyType.ImPThreeD then
        // round up to cude
        let cubeRoot = Convert.ToInt32(System.Math.Ceiling(cuberoot(numberOfNodes |> float)))
        numberOfNodes <- cubeRoot * cubeRoot * cubeRoot
        printfn "cubeRoot: %d" cubeRoot    
    printfn "Number of nodes %d" numberOfNodes
    argvParams <- ArgvInputs(numberOfNodes, argv.[2], argv.[3])
    
setInputs(argv)

let createNetwork(param) =
    match box param with
    | :? ArgvInputs as param ->
        let maxRecieveCount = 2
        for i = 1 to param.NumberOfNodes do
            let name = param.Topology + "-" + Convert.ToString(i)
            let networkNode = spawn system name (actorOf2 NodeFunctions)
            let nodeParams = NodeParams(i, param, maxRecieveCount, i, 1)
            networkNode <! nodeParams

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