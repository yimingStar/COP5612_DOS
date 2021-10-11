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


let GossipToNeighbor(nodeIdx: int, nodeName: string, neighborSet: Set<int>, msg: GossipMsg, topology: string) = async {
    // if nodeIdx = printTargetIdx then
    //     printfn "[%s] Start gossiping to neighbor, neighbors %A" nodeName neighborSet
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


let SenderFunction (nodeMailbox:Actor<SenderType>) =
    let mutable neighborSet = Set.empty
    let mutable topology = ""
    let mutable gossipMsg: GossipMsg = {
        Content = ""
    }
    let mutable nodeIdx = 0
    let mutable nodeName = ""
    let mutable selfActor = select ("") system
    let mutable sendCount = 0
    let mutable ratioChang: double = 0.0

    let mutable prevS = 0.0
    let mutable prevW = 1.0
    let mutable sVal = 0.0
    let mutable wVal = 1.0
    let mutable stopRecv = false
    let mutable stopSend = false
    let mutable inRangCount = 0

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
            selfActor <! RSEND 
        | UPDATESET(newNSet:Set<int>) ->
            neighborSet <- newNSet
        | RSEND ->
            let task = GossipToNeighbor(nodeIdx, nodeName, neighborSet, gossipMsg, topology)
            Async.RunSynchronously task
            sendCount <- sendCount + 1
            selfActor <! RSEND
        | STOPSEND ->
            printfn "[%s] Recv STOPSEND" nodeName 
            let sender = nodeMailbox.Sender()
            let change: double = abs((sVal / wVal) - (prevS/prevW))
            sender <! sendCount

        return! loop ()
    }
    loop()

let closeNode(mainActor, selfActor, nodeIdx, sendCount, runTime, startTime, endTime) =
    let infos: NodeInfos = {
       NodeIdx = nodeIdx
       SendCount = sendCount
       RatioChange = 0.0
       RunTime = runTime // actorTime.ElapsedMilliseconds
       StartTime = startTime
       EndTime = endTime
    }

    let sendNodeInfo = async { 
        // let! response = mainActor <? RECORDNODE(infos)
        // return response
        mainActor <! RECORDNODE(infos)
    }
    Async.RunSynchronously(sendNodeInfo, systemLimitParams.systemTimeOut)
    selfActor <! PoisonPill.Instance


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
    let mutable sendCount = 0
    let mutable ratioChang: double = 0.0

    let mutable nodeName = "unset"
    let mutable nodeSenderName = "unset"
    let mutable topology = "unset"
    let mutable stopRecv = false
    let mutable stopSend = false

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
            topology <- nodeParams.SystemParams.Topology
            nodeName <- topology + "-" + Convert.ToString(nodeParams.NodeIdx)

            neighborSet <- creatNeighborSet(nodeParams.NodeIdx, nodeParams.SystemParams.NumberOfNodes, nodeParams.SystemParams.Topology)
            originalNeighborSet <- neighborSet
            selfActor <- select ("/user/" + string nodeName) system
            nodeSenderName <- nodeName + "-sender"
            selfSendActor <- select ("/user/" + string nodeSenderName) system
            selfActor <! WAITING "READY"

        | GOSSIP(gossipMsg: GossipMsg) ->
            let senderName = sender.Path.Name.ToString()
            if nodeParams.SystemParams.GossipAlgo = AlgoType.RANDOM then
                recvCount <- recvCount + 1
                if recvCount < systemLimitParams.randomLimit then
                    if recvCount = 1 then 
                        // start to be a sender actor
                        let senderActor = spawn system nodeSenderName SenderFunction
                        selfSendActor <! STARTSENDER(nodeParams.NodeIdx, neighborSet, gossipMsg, topology)
                    if nodeParams.NodeIdx = printTargetIdx then     
                        printfn "[%s] recieve rumor msg from %s, receive count %d" nodeName senderName recvCount
                    selfActor <! WAITING ""
                elif recvCount = systemLimitParams.randomLimit && not stopRecv then
                    // printfn "recieve rumor msg from %s, stop recv" senderName
                    selfActor <! STOPRECV ""
        
        | PUSHSUM(pushSumMsg: PushSumMsg) ->
            recvCount <- recvCount + 1
            if recvCount = 1 then
                // spawn the sender actor
                selfSendActor <! SETPSSENDER(nodeParams.NodeIdx, neighborSet, topology)
            else
                selfSendActor <! GAINVALUE(pushSumMsg)
            selfActor <! WAITING ""

        | WAITING str ->
            if stopRecv && stopSend then
                if nodeParams.NodeIdx = printTargetIdx then
                    printfn "[%s]'s DONE!!!!!!!!!!!!!!!!!" nodeName 
                actorTime.Stop()
                let endTime = DateTime.Now.ToString("hh.mm.ss.ffffff")
                closeNode(mainActor, selfActor, nodeParams.NodeIdx, sendCount, actorTime.ElapsedMilliseconds, startTime, endTime)

        | STOPRECV str ->
            // inform neighbors
            stopRecv <- true
            if nodeParams.NodeIdx = printTargetIdx then 
                printfn "[%s] receive all the rumors, %A" nodeName originalNeighborSet
            for i in Set.toList(originalNeighborSet) do
                let neigborName = topology + "-" + i.ToString()
                let nActor = select ("/user/" + string neigborName) system
                nActor <! INFORMFINISH (nodeParams.NodeIdx)
            selfActor <! WAITING ""
            
        | INFORMFINISH(neighborIdx: int) ->
            if nodeParams.NodeIdx = printTargetIdx then
                printfn "[%s]'s neigbor %d is done" nodeName neighborIdx
            // update neighborSet and pass it to sender actor
            neighborSet <- neighborSet.Remove(neighborIdx)
            selfSendActor <! UPDATESET(neighborSet) 
            
            if neighborSet.IsEmpty && not stopSend then
                // close send actor, all neighbor is finish
                let getSendCount = async { 
                    let! response = selfSendActor <? STOPSEND
                    printfn "response %A" response
                    stopSend <- true
                    return response 
                }
                sendCount <- Async.RunSynchronously(getSendCount, systemLimitParams.systemTimeOut) |> int
                selfSendActor <! PoisonPill.Instance
            
            selfActor <! WAITING ""
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


let sendStartMessage(systemParams: ArgvInputs, content: string, startIdx: int) =
    let startNodesName = systemParams.Topology + "-" + Convert.ToString(startIdx)
    if argvParams.GossipAlgo = AlgoType.RANDOM then
        let gossipMsg: GossipMsg = {
            Content = content
        }   
        system.ActorSelection(sprintf "/user/%s" startNodesName) <! GOSSIP gossipMsg
    elif argvParams.GossipAlgo = AlgoType.PUSHSUM then
        let pushSumMsg: PushSumMsg = {
            PushSumS = 0.0
            PushSumW = 0.0
        }   
        system.ActorSelection(sprintf "/user/%s" startNodesName) <! PUSHSUM pushSumMsg


let setMainActor() =
    spawn system "main" MainFunction 

let argv = fsi.CommandLineArgs
printfn "input arguments: %A" (argv) 

inputCheck(argv)
setInputs(argv)

setMainActor()
createNetwork(argvParams)
sendStartMessage(argvParams, "test", 1)

system.WhenTerminated.Wait()