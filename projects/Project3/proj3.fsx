#time "on"
#load "Packages.fsx"
#load "ProjectTypes.fsx"


open System
open System.Diagnostics
open System.Security.Cryptography
open Akka.Actor
open Akka.Configuration
open Akka.FSharp
open ProjectTypes

let mutable systemParams: SystemParams = {
    NumOfNodes = 0
    NumOfRequest = 0
    PowM = 0
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
// 4. build the finger lookup table for each node
// 5. start to assign key-values from random point
// 6. Each node start to request key values

// additional adding and failing nodes
// 7. adding and failing few servers

let system = System.create "proj3Master" config
let roundDuration = 3000

let createServerNumberStr(serverNum: int) = 
    let numberStr = "Server-#" + Convert.ToString(serverNum)
    numberStr


let hashWithShaOne(originalStr: string) =
    let hashedBytes = originalStr |> System.Text.Encoding.UTF8.GetBytes |> (new SHA1Managed()).ComputeHash
    let hashedInt = BitConverter.ToUInt16(hashedBytes, 0) |> int
    hashedInt


let getNodeChordId(nodeName: string) = 
    let hashedInt = hashWithShaOne(nodeName)
    let identifier = hashedInt % systemParams.NumOfIdentifier
    // printfn "nodeName %s, hash %d into identifier: %d " nodeName hashedInt identifier
    identifier


let setIdentifier(numberOfNode: int) = 
    // The identifier length m must be large enough to make the probability of two nodes or keys hashing to the same identifier negligible. 
    let mutable m = 0
    if(numberOfNode <= 4) then
        m <- 3 // 8
    elif(numberOfNode >= 4 && numberOfNode < 16) then
        m <- 5 // 32
    elif(numberOfNode >= 16 && numberOfNode < 64) then
        m <- 7 // 128
    else if(numberOfNode >= 64 && numberOfNode < 128) then
        m <- 8 // 256
    else if(numberOfNode >= 128) then
        m <- 10 // 1024
    m


let setInputs(argv: string[])  = 
    let setParams: SystemParams = {
        NumOfNodes = argv.[1] |> int
        NumOfRequest = argv.[2] |> int
        PowM = setIdentifier(argv.[1] |> int)
        NumOfIdentifier = 2.0 ** (setIdentifier(argv.[1] |> int) |> float) |> int
    }
    setParams


let isBetween(id, startId, endId) = 
    let mutable result = false
    if startId < endId then
        if startId < id && id < endId then
            result <- true
    elif endId < startId then
        if (startId < id && id <= systemParams.NumOfIdentifier-1) || (0 <= id && id < endId) then
            result <- true
    result


let getFingerKeyId(chordId: int, powNumber:int) = 
    let move = (2.0**(powNumber |> float)) |> int
    (chordId + move) % systemParams.NumOfIdentifier


let NodeFunction (nodeMailbox:Actor<NodeActions>) =
    let mutable selfActor = select ("") system
    let mutable chordId = -1
    let mutable nodeNumber = -1
    
    // chord id of next server node
    let mutable successor = -1
    let mutable predecessor = -1

    let defaultFingerCol:FingerCol  = {
        Idx = -1
        KeyId = -1
        Succesor = -1
    }
    let finger = Array.create (systemParams.PowM) (defaultFingerCol)
    let mutable nextFingerIdx = 0
    
    let mutable waitingTask = Set.empty // waiting to know the successor of id = k
    let mutable finderKeyIdMap = Map.empty

    // finger table
    let rec loop () = actor {
        let! (msg: NodeActions) = nodeMailbox.Receive()
        match msg with
        | INIT(setNodeNumber: int) ->
            chordId <- (nodeMailbox.Self.Path.Name |> int)
            nodeNumber <- setNodeNumber
            selfActor <- select ("/user/" + Convert.ToString(chordId)) system

            // printfn "NodeNum: %d, chordId %d, successor %d, finger %A" nodeNumber chordId successor finger
            // set the successor
            successor <- chordId
            if nodeNumber <> 1 then 
                // select any node for finding the successor
                let prevNodeNumber = nodeNumber-1
                let prevNodeName = createServerNumberStr(prevNodeNumber)
                let prevChordId = getNodeChordId(prevNodeName)
                if nodeNumber = 2 then
                    successor <- prevChordId
                else
                    // printfn "NodeNum: %d, chordId %d send Join to prevNodeName %s with chordID %d" nodeNumber chordId prevNodeName prevChordId
                    let chordNode = select ("/user/" + Convert.ToString(prevChordId)) system
                    waitingTask <- waitingTask.Add(chordId)
                    chordNode <! FindSuccesor(chordId, chordId)
            
            // set the finger table checkID and check Range
            let mutable initialFingerSuccessor = chordId
            for idx in 0 .. finger.Length-1 do
                let keyId = getFingerKeyId(chordId, idx)
                let newCol: FingerCol = {
                    Idx = idx
                    KeyId = keyId
                    Succesor = initialFingerSuccessor
                }
                finger.[idx] <- newCol
                finderKeyIdMap <- finderKeyIdMap.Add(keyId, idx)
            selfActor <! FixFinger
            selfActor <! Stabilize

        | FixFinger -> 
            // periodacally send msg to update the finger table
            // 1. Send message update FindSuccessor(finger.[nextFingerIdx].KeyId, chordId)
            // 2. Set the task to waitingList
            let task = async {
                if nodeNumber = 2 then
                    printfn "NodeNum: %d, chordId %d, successor %d, finger %A" nodeNumber chordId successor finger
                let renewKeyId = finger.[nextFingerIdx].KeyId
                waitingTask <- waitingTask.Add(renewKeyId)
                selfActor <! FindSuccesor(renewKeyId, chordId)
                nextFingerIdx <- (nextFingerIdx + 1) % systemParams.PowM
                do! Async.Sleep roundDuration
            }
            Async.RunSynchronously(task)
            selfActor <! FixFinger
        
        | Stabilize -> 
            let task = async {
                if successor <> -1 then
                    let successorNode = select ("/user/" + Convert.ToString(successor)) system
                    successorNode <! AskPredecessor
                do! Async.Sleep roundDuration
            }
            Async.RunSynchronously(task)
            selfActor <! Stabilize

        | AskPredecessor -> 
            let sender = nodeMailbox.Sender()
            sender <! GetPredecessor(predecessor)

        | GetPredecessor(setPredecessor) ->
            let x = setPredecessor;
            if x <> -1 && isBetween(x, chordId, successor) then
                successor <- x
            let successorNode = select ("/user/" + Convert.ToString(successor)) system 
            successorNode <! Notify(chordId) 

        | FindSuccesor(keyId:int, requestId:int) ->
            if keyId = 2 then 
                printfn "NodeNum: %d, chordId %d receive FindSuccesor(id: %d, sId: requestId: %d) check successor %d" nodeNumber chordId keyId requestId successor
            // The request ancestor is requestId -> if find pass it back to requestId
            if isBetween(keyId, chordId, successor+1) then
                let requestActor = select ("/user/" + Convert.ToString(requestId)) system
                // printfn "NodeNum: %d, chordId %d requestId: %d send back (%d, %d)" nodeNumber chordId requestId keyId chordId
                requestActor <! ConfirmSUCCESSOR(keyId, successor)
            else
                // unfound continue pass message to find it, pass on the message through the circle   
                let mutable passingChordId = chordId
                let mutable inFingerTable = false
                for i = systemParams.PowM-1 downto 0 do
                    if isBetween(finger.[i].Succesor, chordId, keyId) && not inFingerTable then
                        passingChordId <- finger.[i].Succesor
                        inFingerTable <- true

                let passNode = select ("/user/" + Convert.ToString(passingChordId)) system                         
                passNode <! FindSuccesor(keyId, requestId)
                
            selfActor <! WAITING

        | ConfirmSUCCESSOR(keyId: int, setSuccessorId: int) ->
            // get id's successor as chordId
            // printfn "NodeNum: %d, chordId %d receive ConfirmSUCCESSOR(id: %d, setSuccessorId: %d)" nodeNumber chordId keyId setSuccessorId
            waitingTask <- waitingTask.Remove(keyId)
            if keyId = chordId then
                // update node successor
                successor <- setSuccessorId

            // check if this key is in the Finger Table
            if finderKeyIdMap.ContainsKey(keyId) then
                let fingerIdx = finderKeyIdMap.Item(keyId)
                let newCol: FingerCol = {
                    Idx = finger.[fingerIdx].Idx
                    KeyId = finger.[fingerIdx].KeyId
                    Succesor = setSuccessorId
                }
                finger.[fingerIdx] <- newCol
                if fingerIdx = 0 then
                    successor <- setSuccessorId
            selfActor <! WAITING
        | Notify(targetChordId: int) ->
            if predecessor = -1 || isBetween(targetChordId, predecessor, chordId) then
                predecessor <- targetChordId
            // while join
        | WAITING -> ()
        return! loop()
    }
    loop()

let createNetwork(param) =
    match box param with
    | :? SystemParams as param ->
        for i = 1 to param.NumOfNodes do
            let inputStr = createServerNumberStr(i)
            let chordId = getNodeChordId(inputStr)
            let chordIdStr = Convert.ToString(chordId)
            // set id as the 
            let networkNode = spawn system chordIdStr NodeFunction
            networkNode <! INIT(i)

    | _ ->  failwith "Invalid input variables to build a network"


let argv = fsi.CommandLineArgs
printfn "input arguments: %A" (argv) 

systemParams <- setInputs(argv)
printfn "system systemParams: %A" (systemParams)
createNetwork(systemParams)

System.Console.ReadLine() |> ignore