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
                actor.provider = remote
                remote.helios.tcp {
                    hostname = " + hostIP + "
                    port = " + port + "
                }
            }"
        )

let proc = Process.GetCurrentProcess()
let realTime = Stopwatch.StartNew()
let system = System.create "proj2Master" config
let printTargetIdx = 2


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

let PushSumToNeighbor(nodeIdx: int, nodeName: string, neighborSet: Set<int>, msg: PushSumMsg, topology: string) = async {
    // if nodeIdx = printTargetIdx then
    //     printfn "[%s] Start Push to neighbor, neighbors %A" nodeName neighborSet
    let neigborCount = neighborSet.Count
    let mutable randomNeighborIdx = -1
    let mutable neigborName = ""
    if neigborCount > 0 && msg.PushSumS > 0.0 && msg.PushSumW > 0.0 then
        randomNeighborIdx <- Random().Next(0, neigborCount)
        let nList = Set.toList(neighborSet)
        neigborName <- topology + "-" + nList.[randomNeighborIdx].ToString()
        let nActor = select ("/user/" + string neigborName) system
        do! Async.Sleep systemLimitParams.roundDuration 
        nActor <! PUSHSUM msg
}

let SenderFunction (nodeMailbox:Actor<SenderType>) =
    let mutable nodeIdx = 0
    let mutable nodeName = ""
    let mutable nodeRecvName = ""

    let mutable selfActor = select ("") system
    let mutable selfRecvActor = select ("") system

    let mutable neighborSet = Set.empty
    let mutable topology = ""
    let mutable algo = ""

    let mutable gossipMsg: GossipMsg = {
        Content = ""
    }

    let mutable giveSumMsg: PushSumMsg = {
        PushSumS = 0.0
        PushSumW = 0.0
    }

    let mutable stopRecv = false
    let mutable stopSend = false

    let mutable sendCount = 0
    let mutable ratioChang: double = 0.0

    let rec loop () = actor {
        let! (msg: SenderType) = nodeMailbox.Receive()
        match msg with
        | STARTSENDER(setNodeIdx: int, setSet:Set<int>, setMsg: GossipMsg, setTopology: string, setAlgo: string) -> 
            nodeIdx <- setNodeIdx
            nodeName <- setTopology + "-" + Convert.ToString(nodeIdx) + "-sender"

            neighborSet <- setSet
            topology <- setTopology
            algo <- setAlgo

            gossipMsg <- setMsg
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
            let sender = nodeMailbox.Sender()
            sender <! sendCount
        // PushSum Flow Functions
        | SETPSSENDER(setNodeIdx: int, setSet:Set<int>, setTopology: string, setAlgo: string, pushSumMsg: PushSumMsg) -> 
            nodeIdx <- setNodeIdx
            topology <- setTopology
            nodeName <- topology + "-" + Convert.ToString(nodeIdx) + "-sender"
            nodeRecvName <- topology + "-" + Convert.ToString(nodeIdx)
            neighborSet <- setSet

            selfActor <- select ("/user/" + string nodeName) system
            selfRecvActor <- select ("/user/" + string nodeRecvName) system

            // start timer
            selfActor <! UDATESW(pushSumMsg)
            selfActor <! PSSEND
        | UDATESW(pushSumMsg: PushSumMsg) ->
            giveSumMsg <- pushSumMsg
        | PSSEND ->
            let task = PushSumToNeighbor(nodeIdx, nodeName, neighborSet, giveSumMsg, topology)
            Async.RunSynchronously task
            sendCount <- sendCount + 1
            selfActor <! PSSEND
        return! loop ()
    }
    loop()

let closeNode(mainActor, selfActor, selfSendActor, nodeIdx, sendCount, change, runTime, startTime, endTime) =
    let infos: NodeInfos = {
       NodeIdx = nodeIdx
       SendCount = sendCount
       RatioChange = change
       RunTime = runTime // actorTime.ElapsedMilliseconds
       StartTime = startTime
       EndTime = endTime
    }

    selfSendActor <! PoisonPill.Instance
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
    let mutable close = false

    let mutable originalNeighborSet = Set.empty
    let mutable neighborSet = Set.empty
    let mutable selfActor = select ("") system
    let mutable selfSendActor =  select ("") system

    let mutable prevS = 0.0
    let mutable prevW = 1.0
    let mutable sVal = 0.0
    let mutable wVal = 1.0
    let mutable inRangCount = 0

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
            nodeSenderName <- nodeName + "-sender"

            prevS <- (double)nodeParams.NodeIdx
            prevW <- 1.0
            sVal <- (double)nodeParams.NodeIdx
            wVal <- 1.0
                
            neighborSet <- creatNeighborSet(nodeParams.NodeIdx, nodeParams.SystemParams.NumberOfNodes, nodeParams.SystemParams.Topology)
            originalNeighborSet <- neighborSet
            selfActor <- select ("/user/" + string nodeName) system
            let senderActor = spawn system nodeSenderName SenderFunction
            selfSendActor <- select ("/user/" + string nodeSenderName) system
            selfActor <! WAITING "READY"

        | GOSSIP(gossipMsg: GossipMsg) ->
            let senderName = sender.Path.Name.ToString()
            if nodeParams.SystemParams.GossipAlgo = AlgoType.RANDOM then
                recvCount <- recvCount + 1
                if recvCount < systemLimitParams.randomLimit then
                    if recvCount = 1 then 
                        // start to be a sender actor
                        selfSendActor <! STARTSENDER(nodeParams.NodeIdx, neighborSet, gossipMsg, topology, nodeParams.SystemParams.GossipAlgo)
                    // if nodeParams.NodeIdx = printTargetIdx then     
                    //     printfn "[%s] recieve rumor msg from %s, receive count %d" nodeName senderName recvCount
                    selfActor <! WAITING ""
                elif recvCount = systemLimitParams.randomLimit && not stopRecv then
                    // printfn "recieve rumor msg from %s, stop recv" senderName
                    selfActor <! STOPRECV ""
        
        | PUSHSUM(pushSumMsg: PushSumMsg) ->
            // Receive 
            recvCount <- recvCount + 1

            sVal <- sVal + pushSumMsg.PushSumS
            wVal <- wVal + pushSumMsg.PushSumW

            prevS <- sVal
            prevW <- wVal

            // left half
            sVal <- sVal/2.0
            wVal <- wVal/2.0

            let newPushSum: PushSumMsg = {
                PushSumS = sVal
                PushSumW = wVal
            }

            if recvCount = 1 then
                // set the sender
                selfSendActor <! SETPSSENDER(nodeParams.NodeIdx, neighborSet, topology, nodeParams.SystemParams.GossipAlgo, newPushSum)
            else

                selfSendActor <! UDATESW(newPushSum)
            
            let change: double = abs((sVal / wVal) - (prevS/prevW))
            // let task = async {
            //     printfn "[%s] gain value, original value %f, new value %f, change value %f, count %d" nodeName (sVal / wVal) (prevS/prevW) change inRangCount
            // }
            // try
            //     Async.RunSynchronously(task, 500)
            // with :? System.TimeoutException ->
            //     ()
                
            if change <= systemLimitParams.pushSumRange then
                inRangCount <- inRangCount + 1
                if inRangCount = systemLimitParams.pushSumLimit && not stopRecv then
                    selfActor <! STOPRECV ""
            elif change > systemLimitParams.pushSumRange then
                inRangCount <- 0
                selfActor <! WAITING ""

        | WAITING str ->
            if stopRecv && stopSend && not close then
                close <- true
                actorTime.Stop()
                let endTime = DateTime.Now.ToString("hh.mm.ss.ffffff")
                let change: double = abs((sVal / wVal) - (prevS/prevW))
                closeNode(mainActor, selfActor, selfSendActor, nodeParams.NodeIdx, sendCount, change, actorTime.ElapsedMilliseconds, startTime, endTime)

        | STOPRECV str ->
            // inform neighbors
            stopRecv <- true
            for i in Set.toList(originalNeighborSet) do
                let neigborName = topology + "-" + i.ToString()
                let nActor = select ("/user/" + string neigborName) system
                nActor <! INFORMFINISH (nodeParams.NodeIdx)
             
            selfActor <! WAITING ""

        | INFORMFINISH(neighborIdx: int) ->
            neighborSet <- neighborSet.Remove(neighborIdx)
            selfSendActor <! UPDATESET(neighborSet) 
            
            if neighborSet.IsEmpty && not stopSend then
                // printfn "[%s]'s close send actor, all neighbor is finish" nodeName
                // close send actor, all neighbor is finish
                let getSendCount = async { 
                    let! response = selfSendActor <? STOPSEND
                    stopSend <- true
                    return response 
                }
                try
                    sendCount <- Async.RunSynchronously(getSendCount, systemLimitParams.systemTimeOut) |> int
                with :? System.TimeoutException ->
                    sendCount <- -1
                    
            selfActor <! WAITING ""
        return! loop ()
    }
    loop ()


let MainFunction (mainMailbox:Actor<MainNodeType>) =
    let mutable nodeInfoList = []
    let mutable selfActor = select ("/user/main") system
    let mutable totalSendTime = 0
    let mutable writeFile = false

    let rec loop () = actor {
        let! (msg: MainNodeType) = mainMailbox.Receive()
        match msg with
        | RECORDNODE(info: NodeInfos) ->
            // printfn "info %A" info
            nodeInfoList <- nodeInfoList @ [info]
            totalSendTime <- totalSendTime + info.SendCount
            
            if nodeInfoList.Length = argvParams.NumberOfNodes && not writeFile then
                writeFile <- true
                printfn "write FILE !!!!!!!!!!!!!! %d" nodeInfoList.Length
                for r in nodeInfoList do
                    let recordLine = sprintf "%d, %d, %f, %d, %s, %s" r.NodeIdx r.SendCount r.RatioChange r.RunTime r.StartTime r.EndTime
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