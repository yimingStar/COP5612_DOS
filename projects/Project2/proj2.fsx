open System.Threading
#time "on"
#load "Packages.fsx"
#load "ProjectTypes.fsx"
#load "UnitFunctions.fsx"
#load "Settings.fsx"
#load "Constants.fsx"

open System
open System.Diagnostics
open Akka.Actor
open Akka.Configuration
open Akka.FSharp
open ProjectTypes
open UnitFunctions
open Settings
open Constants

let mutable argvParams = ArgvInputs(0, "", "")

let hostIP = "localhost"
let port = "5567" 
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

let setInputs(argv: string[]) =
    let mutable numberOfNodes = argv.[1] |> int
    if argv.[2] = TopologyType.ThreeD || argv.[2] = TopologyType.ImPThreeD then
        // round up to cude
        let cubeRoot = Convert.ToInt32(System.Math.Ceiling(cuberoot(numberOfNodes |> float)))
        numberOfNodes <- cubeRoot * cubeRoot * cubeRoot
        printfn "cubeRoot: %d" cubeRoot    
    printfn "Number of nodes %d" numberOfNodes
    argvParams <- ArgvInputs(numberOfNodes, argv.[2], argv.[3])

let GossipToNeighbor(nodeName: string, neighborSet: Set<int>, msg: GossipMsg, topology: string) = async {
    printfn "[%s] Start gossiping to neighbor, neighbors %A" nodeName neighborSet
    let neigborCount = neighborSet.Count
    let mutable randomNeighborIdx = -1
    let mutable neigborName = ""

    randomNeighborIdx <- Random().Next(0, neigborCount)
    let nList = Set.toList(neighborSet)
    neigborName <- topology + "-" + nList.[randomNeighborIdx].ToString()
    printfn "neigborName %s" neigborName
    let nActor = select ("/user/" + string neigborName) system
    do! Async.Sleep systemLimitParams.roundDuration 
    nActor <! GOSSIP msg
}

let randomSenderFunction (nodeMailbox:Actor<SenderType>) =
    let rec loop () = actor {
        let! (msg: SenderType) = nodeMailbox.Receive()
        match msg with
        | STARTSENDER(nodeName: string, neighborSet:Set<int>, gossipMsg: GossipMsg, topology: string) -> 
            let selfActor = select ("/user/" + string nodeName) system
            let task = GossipToNeighbor(nodeName, neighborSet, gossipMsg, topology)
            Async.RunSynchronously task
            selfActor <! STARTSENDER(nodeName, neighborSet, gossipMsg, topology) 
        return! loop ()
    }
    loop()

let NodeFunction (nodeMailbox:Actor<ReceiveType>) = 
    let mutable nodeParams: NodeParams = {
        NodeIdx = -1
        SystemParams = ArgvInputs(-1, "", "")
        MaxRecieveCount = 0
        PushSumS = 0
        PushSumW = 0
    }
    let mutable recvCount = 0
    let mutable nodeName = "unset"
    let mutable neighborSet = Set.empty
    let mutable selfActor = select ("") system
    
    let rec loop () = actor {
        let! (msg: ReceiveType) = nodeMailbox.Receive()
        let sender = nodeMailbox.Sender()

        match msg with
        | INIT(param:NodeParams) -> 
            nodeParams <- param
            nodeName <- nodeParams.SystemParams.Topology + "-" + Convert.ToString(nodeParams.NodeIdx)
            neighborSet <- creatNeighborSet(nodeParams.NodeIdx, nodeParams.SystemParams.NumberOfNodes, nodeParams.SystemParams.Topology)

            selfActor <- select ("/user/" + string nodeName) system
            selfActor <! WAITING "READY"

        | GOSSIP(gossipMsg: GossipMsg) ->
            let topology = nodeParams.SystemParams.Topology
            let senderName = sender.Path.Name.ToString()
            if nodeParams.SystemParams.GossipAlgo = AlgoType.RANDOM then
                recvCount <- recvCount + 1
                if recvCount <= systemLimitParams.randomLimit then
                    if recvCount = 1 then 
                        // start to be a sender actor
                        let senderName = nodeName + "-random-sender" 
                        let senderActor = spawn system senderName randomSenderFunction
                        senderActor <! STARTSENDER(senderName, neighborSet, gossipMsg, topology) 
                    let actionStr = sprintf "recieve rumor msg from %s, receive count %d" senderName recvCount
                    selfActor <! WAITING actionStr
                else
                    selfActor <! STOPRECV ""
        | WAITING str ->
            printfn "[%s] WAITING state, prev action - %s" nodeName str
        | STOPRECV str ->
            // inform neighbors
            printfn "[%s] receive all the rumors" nodeName
        return! loop ()
    }
    loop ()

let createNetwork(param) =
    match box param with
    | :? ArgvInputs as param ->
        let maxRecieveCount = 2
        for i = 1 to param.NumberOfNodes do
            let name = param.Topology + "-" + Convert.ToString(i)
            let networkNode = spawn system name NodeFunction
            let nodeParams: NodeParams = {
                NodeIdx = i
                SystemParams = param
                MaxRecieveCount = maxRecieveCount
                PushSumS = i
                PushSumW = 1
            }
            networkNode <! INIT nodeParams

    | _ ->  failwith "Invalid input variables to build a network"

let sendMessage(systemParams: ArgvInputs, content: string, startIdx: int) =
    let gossipMsg: GossipMsg = {
        Content = content
    }
    let startNodesName = systemParams.Topology + "-" + Convert.ToString(startIdx)
    system.ActorSelection(sprintf "/user/%s" startNodesName) <! GOSSIP gossipMsg

let argv = fsi.CommandLineArgs
printfn "input arguments: %A" (argv) 

inputCheck(argv)
setInputs(argv)
createNetwork(argvParams)

sendMessage(argvParams, "test", 1)

realTime.Stop()
let cpuTime = proc.TotalProcessorTime.TotalMilliseconds
// printfn "CPU Time = %dms" (int64 cpuTime)
// printfn "Real Time = %dms" realTime.ElapsedMilliseconds

System.Console.ReadLine() |> ignore