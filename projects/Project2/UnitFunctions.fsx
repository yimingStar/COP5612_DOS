#load "Packages.fsx"
#load "ProjectTypes.fsx"

open System
open System.Security.Cryptography
open Akka.FSharp
open ProjectTypes

let getLocalIP =
    let mutable ip = "" 
    try
        let localHost = System.Net.Dns.GetHostName()
        let getIP = System.Net.Dns.GetHostEntry(localHost).AddressList |> Array.find (fun x -> x.AddressFamily.ToString() = "InterNetwork")
        ip <- getIP.ToString()
    with ex -> 
        printfn "%A" ex
        ip <- "localhost"
    ip

let getStartProjMsg = 
    // start progect1 program
    let courseInfo = "[COP5612] DOS"
    let projectInfo = "Project 1 Mine Coins"
    let startMsg = sprintf "Start course %s %s" courseInfo projectInfo
    startMsg

let creatNeighborSet(nodeId: int, numberOfNodes: int, topology: string) =
    let mutable neiborSet = Set.empty
    if topology = TopologyType.LINE then
        if nodeId = 1 then
            neiborSet <- neiborSet.Add(2)
        elif nodeId = numberOfNodes then
            neiborSet <- neiborSet.Add(numberOfNodes-1)
        else 
            neiborSet <- neiborSet.Add(nodeId-1)
            neiborSet <- neiborSet.Add(nodeId+1)
    elif topology = TopologyType.FULL then
        for i=1 to numberOfNodes do
            if i <> nodeId then 
                neiborSet <- neiborSet.Add(i)
    elif topology = TopologyType.ThreeD || topology = TopologyType.ImPThreeD then
        // find the boundary of the plane by using 
        // each x-y plane 
        let cubeRoot = Convert.ToInt32(cuberoot(numberOfNodes |> float))
        let squareSize = cubeRoot * cubeRoot
        let heightLevel = System.Math.Ceiling(nodeId/squareSize)
        
    neiborSet

let NodeFunctions(mailbox: Actor<obj>) msg =
    let mutable nodeParams = NodeParams()
    let mutable recieveCount = 0
    let sender = mailbox.Sender()
    
    let mutable neighborSet = Set.empty
    let rec gossipLoop() =
        // printfn "actor %A" mailbox.Self.Path
        // printfn "recieve sender %s, msg %s" (sender.Path.Name.ToString()) (msg.ToString())
        match box msg with
        | :? NodeParams as param ->
            nodeParams <- param
            printfn "recieve  from %s, msg %s" (sender.Path.Name.ToString()) (param.ToString())
            // create neighborlist
            neighborSet <- creatNeighborSet(
                nodeParams.NodeIdx, nodeParams.SystemParams.NumberOfNodes, nodeParams.SystemParams.Topology)
            printfn "neighbors %A" neighborSet

        | :? GossipMsg as param ->
            printfn "recieve rumer from %s, msg %s" (sender.Path.Name.ToString()) (param.ToString())
            recieveCount <- recieveCount + 1
            
        | _ ->  (failwith "unknown gossip inputs")
    gossipLoop()

let cuberoot (f:float<'m^3>) : float<'m> = 
    System.Math.Pow(float f, 1.0/3.0) |> LanguagePrimitives.FloatWithMeasure