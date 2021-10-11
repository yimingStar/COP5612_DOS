open System.Threading
open System.Collections.Generic
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
let config =
    ConfigurationFactory.ParseString(
            @"akka {
                loggers = Akka.Event.DefaultLogger, Akka
                loglevel = DEBUG

                actor.provider = remote
                remote.helios.tcp {
                    hostname = " + hostIP + "
                    port = " + port + "
                }
            }"
        )

// let config =
//     Configuration.parse
//         @"akka {
//             actor.loggers = ""Akka.event.slf4j.Slf4jLogger""
//             actor.loglevel = ""DEBUG""
//             actor.logging-filter = ""Akka.event.slf4j.Slf4jLoggingFilter""
//         }"

let proc = Process.GetCurrentProcess()
let realTime = Stopwatch.StartNew()
let system = System.create "proj2Master" config
let printTargetIdx = 1


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
    // printfn "[%s] Start gossiping to neighbor, neighbors %A" nodeName neighborSet
    let neigborCount = neighborSet.Count
    let mutable randomNeighborIdx = -1
    let mutable neigborName = ""
    if neigborCount > 0 then
        randomNeighborIdx <- Random().Next(0, neigborCount)
        let nList = Set.toList(neighborSet)
        neigborName <- topology + "-" + nList.[randomNeighborIdx].ToString()
        // printfn "neigborName %s" neigborName
        let nActor = select ("/user/" + string neigborName) system
        do! Async.Sleep systemLimitParams.roundDuration 
        nActor <! GOSSIP msg
}


let randomSenderFunction (nodeMailbox:Actor<SenderType>) =
    let mutable neighborSet = Set.empty
    let mutable topology = ""
    let mutable gossipMsg: GossipMsg = {
        Content = ""
    }
    let mutable nodeIdx = 0
    let mutable nodeName = ""
    let mutable selfActor = select ("") system
    let mutable sendCount = 0

    let rec loop () = actor {
        let! (msg: SenderType) = nodeMailbox.Receive()
        match msg with
        | STARTSENDER(setNodeIdx: int, setSet:Set<int>, setMsg: GossipMsg, setTopology: string) -> 
            nodeIdx <- setNodeIdx
            nodeName <- setTopology + "-" + Convert.ToString(nodeIdx) + "-sender"

            neighborSet <- setSet
            gossipMsg <- setMsg
            topology <- setTopology

            selfActor <- select ("/user/" + string nodeName) system
            selfActor <! SEND 
        | UPDATE(newNSet:Set<int>) ->
            neighborSet <- newNSet
        | SEND ->
            let task = GossipToNeighbor(nodeName, neighborSet, gossipMsg, topology)
            Async.RunSynchronously task
            sendCount <- sendCount + 1
            selfActor <! SEND
        | STOPSEND ->
            let sender = nodeMailbox.Sender()
            sender <! sendCount
            selfActor <! PoisonPill.Instance
        return! loop ()
    }
    loop()


let NodeFunction (nodeMailbox:Actor<ReceiveType>) = 
    let mainActor = select ("/user/main") system
    let mutable nodeParams: NodeParams = {
        NodeIdx = -1
        SystemParams = ArgvInputs(-1, "", "")
        MaxRecieveCount = 0
        PushSumS = 0
        PushSumW = 0
    }
    let mutable recvCount = 0
    let mutable nodeName = "unset"
    
    let mutable originalNeighborSet = Set.empty
    let mutable neighborSet = Set.empty
    let mutable selfActor = select ("") system
    let mutable selfSendActor =  select ("") system

    let startTime = DateTime.Now.ToString("hh.mm.ss.ffffff");
    let actorTime = System.Diagnostics.Stopwatch()
    actorTime.Start()
    
    let rec loop () = actor {
        let! (msg: ReceiveType) = nodeMailbox.Receive()
        let sender = nodeMailbox.Sender()

        match msg with
        | INIT(param:NodeParams) -> 
            nodeParams <- param
            nodeName <- nodeParams.SystemParams.Topology + "-" + Convert.ToString(nodeParams.NodeIdx)
            neighborSet <- creatNeighborSet(nodeParams.NodeIdx, nodeParams.SystemParams.NumberOfNodes, nodeParams.SystemParams.Topology)
            originalNeighborSet <- neighborSet
            selfActor <- select ("/user/" + string nodeName) system
            let senderName = nodeName + "-sender"
            selfSendActor <- select ("/user/" + string senderName) system
            selfActor <! WAITING "READY"

        | GOSSIP(gossipMsg: GossipMsg) ->
            let topology = nodeParams.SystemParams.Topology
            let senderName = sender.Path.Name.ToString()
            if nodeParams.SystemParams.GossipAlgo = AlgoType.RANDOM then
                recvCount <- recvCount + 1
                if recvCount < systemLimitParams.randomLimit then
                    if recvCount = 1 then 
                        // start to be a sender actor
                        let senderName = nodeName + "-sender" 
                        let senderActor = spawn system senderName randomSenderFunction
                        senderActor <! STARTSENDER(nodeParams.NodeIdx, neighborSet, gossipMsg, topology) 
                    printfn "recieve rumor msg from %s, receive count %d" senderName recvCount
                    selfActor <! WAITING ""
                elif recvCount = systemLimitParams.randomLimit then
                    printfn "recieve rumor msg from %s, stop recv" senderName
                    selfActor <! STOPRECV ""
        | WAITING str -> ()
        | STOPRECV str ->
            // inform neighbors
            let topology = nodeParams.SystemParams.Topology
            printfn "[%s] receive all the rumors" nodeName
            for i in Set.toList(originalNeighborSet) do
                let neigborName = topology + "-" + i.ToString()
                let nActor = select ("/user/" + string neigborName) system
                nActor <! INFORMFINISH (nodeParams.NodeIdx)

        | INFORMFINISH(neighborIdx: int) ->
            if nodeParams.NodeIdx = printTargetIdx then
                printfn "[%s]'s %d neighbor is done" nodeName neighborIdx
            // update neighborSet and pass it to sender actor
            neighborSet <- neighborSet.Remove(neighborIdx) 
            if neighborSet.IsEmpty then
                // close the actor, all neighbor is finish
                actorTime.Stop()
                let endTime = DateTime.Now.ToString("hh.mm.ss.ffffff");
                if nodeParams.NodeIdx = printTargetIdx then
                    printfn "endTime %A" endTime
                    printfn "actor durationTime = %dms" actorTime.ElapsedMilliseconds
                
                let getSendCount = async { 
                    let! response = selfSendActor <? STOPSEND
                    return response 
                }
                let sendCount = Async.RunSynchronously(getSendCount, systemLimitParams.systemTimeOut) |> int
                if nodeParams.NodeIdx = printTargetIdx then
                    printfn "send time %A" sendCount

                let infos: NodeInfos = {
                   NodeIdx = nodeParams.NodeIdx
                   SendCount = sendCount
                   RunTime = actorTime.ElapsedMilliseconds
                   StartTime = startTime
                   EndTime = endTime
                }

                let sendNodeInfo = async { 
                    // let! response = mainActor <? RECORDNODE(infos)
                    // return response
                    mainActor <! RECORDNODE(infos)
                }
                Async.RunSynchronously(sendNodeInfo, systemLimitParams.systemTimeOut)
                // let hasSendInfo = Async.RunSynchronously(sendNodeInfo, systemLimitParams.systemTimeOut)
                // if hasSendInfo then
                //     printfn "node info records"
                selfActor <! PoisonPill.Instance
            selfSendActor <! UPDATE(neighborSet)
        return! loop ()
    }
    loop ()


let MainFunction (mainMailbox:Actor<MainNodeType>) =
    let mutable nodeInfoList = []
    let mutable selfActor = select ("/user/main") system
    let mutable totalSendTime = 0

    let rec loop () = actor {
        let! (msg: MainNodeType) = mainMailbox.Receive()
        match msg with
        | RECORDNODE(info: NodeInfos) ->
            printfn "info %A" info
            nodeInfoList <- nodeInfoList @ [info]
            totalSendTime <- totalSendTime + info.SendCount
            if nodeInfoList.Length = argvParams.NumberOfNodes then
                for r in nodeInfoList do
                    let recordLine = sprintf "%d, %d, %d, %s, %s" r.NodeIdx r.SendCount r.RunTime r.StartTime r.EndTime
                    System.IO.File.AppendAllLinesAsync(recordFilePath, [recordLine]) |> ignore
                selfActor <! STOPSYSTEM
        | STOPSYSTEM ->
            printfn "STOP SYSTEM SIGNAL" 
            mainMailbox.Context.System.Terminate() |> ignore
        return! loop ()
    }
    loop()


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


let setMainActor() =
    spawn system "main" MainFunction 

let argv = fsi.CommandLineArgs
printfn "input arguments: %A" (argv) 

inputCheck(argv)
setInputs(argv)

setMainActor()
createNetwork(argvParams)
sendMessage(argvParams, "test", 1)

system.WhenTerminated.Wait()