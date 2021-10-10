#load "Packages.fsx"
#load "ProjectTypes.fsx"
#load "Constants.fsx"

open System
open Akka.FSharp
open ProjectTypes
open Constants

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

let cuberoot (f:float<'m^3>) : float<'m> = 
    System.Math.Pow(float f, 1.0/3.0) |> LanguagePrimitives.FloatWithMeasure

let creatNeighborSet(nodeId: int, numberOfNodes: int, topology: string) =
    let mutable neighborSet = Set.empty
    if topology = TopologyType.LINE then
        if nodeId = 1 then
            neighborSet <- neighborSet.Add(2)
        elif nodeId = numberOfNodes then
            neighborSet <- neighborSet.Add(numberOfNodes-1)
        else 
            neighborSet <- neighborSet.Add(nodeId-1)
            neighborSet <- neighborSet.Add(nodeId+1)
    elif topology = TopologyType.FULL then
        for i=1 to numberOfNodes do
            if i <> nodeId then 
                neighborSet <- neighborSet.Add(i)
    elif topology = TopologyType.ThreeD || topology = TopologyType.ImPThreeD then
        // find the boundary of the plane by using 
        // each x-y plane 
        let cubeRoot = Convert.ToInt32(cuberoot(numberOfNodes |> float))
        let squareSize = cubeRoot * cubeRoot
        
        // adding top and bottom 
        if nodeId + squareSize <= numberOfNodes then
            neighborSet <- neighborSet.Add(nodeId + squareSize)
        if nodeId - squareSize >= 1 then
            neighborSet <- neighborSet.Add(nodeId - squareSize)
        
        // front and back 
        if (nodeId - 1) % cubeRoot <> 0 then
            // exist front node
            neighborSet <- neighborSet.Add(nodeId - 1)
        if nodeId % cubeRoot <> 0 then
            // exist back node  
            neighborSet <- neighborSet.Add(nodeId + 1)
        
        // left and right
        if nodeId + cubeRoot <= numberOfNodes then
            neighborSet <- neighborSet.Add(nodeId + cubeRoot)
        if nodeId - cubeRoot >= 1 then
            neighborSet <- neighborSet.Add(nodeId - cubeRoot)

        // 1 2  5 6
        // 3 4  7 8

        // 1 2 3   10 11 12   19 20 21
        // 4 5 6   13 14 15   22 23 24
        // 7 8 9   16 17 18   25 26 27

        // 1 2 3 4       17 18 19 20
        // 5 6 7 8       21 22 23 24
        // 9 10 11 12    25 26 27 28
        // 13 14 15 16   29 30 31 32

        if topology = TopologyType.ImPThreeD then
            // add a random neighbor
            let mutable randomNodeId = -1
            let mutable findRandom = true
            
            while findRandom do
                let r = System.Random()
                randomNodeId <- r.Next(1, numberOfNodes)
                if randomNodeId <> nodeId && not (neighborSet.Contains(randomNodeId)) then
                    findRandom <- false
            neighborSet <- neighborSet.Add(randomNodeId)
    neighborSet
