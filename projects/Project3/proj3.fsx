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
let roundDuration = 150
let requestDuration = 1000
let checkPredecessorDuration = 1000
let mutable prinfnServerNumber = -1
let mutable firstServerChordID = -1
let mutable secondServerChordID = -1

let createServerNumberStr(serverNum: int) = 
    let mutable numberStr = "Server-#" + Convert.ToString(serverNum)
    if serverNum > 2 then 
        let randomStartIdx = Random().Next(0, 10)
        let randomLength = Random().Next(16, 26)
        let mutable randomString = Guid.NewGuid().ToString().Substring(randomStartIdx, randomLength)
        numberStr <- numberStr + "-" + randomString
    numberStr


let hashWithShaOne(originalStr: string) =
    let hashedBytes = originalStr |> System.Text.Encoding.UTF8.GetBytes |> (new SHA1Managed()).ComputeHash
    let hashedInt = BitConverter.ToUInt16(hashedBytes, 0) |> int
    hashedInt


let getNodeChordId(nodeName: string) = 
    let hashedInt = hashWithShaOne(nodeName)
    let identifier = hashedInt % systemParams.NumOfIdentifier
    identifier


let setIdentifier(numberOfNode: int) = 
    // The identifier length m must be large enough to make the probability of two nodes or keys hashing to the same identifier negligible. 
    let mutable m = 0
    if(numberOfNode <= 4) then
        m <- 3 // 8
    elif(numberOfNode >= 4 && numberOfNode < 16) then
        m <- 9 // 512 
    elif(numberOfNode >= 16 && numberOfNode < 32) then
        m <- 10 // 1024
    else if(numberOfNode >= 32 && numberOfNode < 128) then
        m <- 12 // 2048
    else if(numberOfNode >= 128) then
        m <- 14 // 4096 * 4
    else if(numberOfNode >= 1000) then
        m <- 15
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
    let mutable serverNumber = -1

    // chord id of next server node
    let mutable successor = -1
    let mutable predecessor = -1

    let defaultFingerCol:FingerCol  = {
        Idx = -1
        KeyId = -1
        Succesor = -1
    }
    // finger table params
    let finger = Array.create (systemParams.PowM) (defaultFingerCol)
    let mutable nextFingerIdx = 0
    let mutable waitingSystemMsg = Set.empty // waiting to know the successor of id = k
    let mutable finderKeyIdMap = Map.empty
    
    // Data key params
    let mutable waitingDataMsg = Set.empty
    let mutable requestCount = 0
    let mutable recvRequest = 0
    
    let rec loop () = actor {
        let! (msg: NodeActions) = nodeMailbox.Receive()
        match msg with
        | INIT(setServerNumber: int) ->
            chordId <- (nodeMailbox.Self.Path.Name |> int)
            serverNumber <- setServerNumber
            selfActor <- select ("/user/" + Convert.ToString(chordId)) system
            // set the successor
            successor <- chordId
            if serverNumber = 1 then
                successor <- secondServerChordID
            else 
                if serverNumber = 2 then
                    successor <- firstServerChordID
                else
                    let randomChordNode = select ("/user/" + Convert.ToString(firstServerChordID)) system
                    waitingSystemMsg <- waitingSystemMsg.Add(chordId)
                    randomChordNode <! FindSuccesor(chordId, chordId, MessageType.SYSTEM)
            
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
            selfActor <! StartRequestTask

        | FixFinger -> 
            // periodacally send msg to update the finger table
            // 1. Send message update FindSuccessor(finger.[nextFingerIdx].KeyId, chordId)
            // 2. Set the task to waitingList
            let task = async {
                // if serverNumber = prinfnServerNumber then
                //     printfn "SererNum: %d, chordId %d, successor %d, fingerTable:\n%A" serverNumber chordId successor finger
                nextFingerIdx <- (nextFingerIdx + 1) % systemParams.PowM
                let renewKeyId = finger.[nextFingerIdx].KeyId
                let targetSuccessor = finger.[nextFingerIdx].Succesor
                
                waitingSystemMsg <- waitingSystemMsg.Add(renewKeyId)
                let targetActor = select ("/user/" + Convert.ToString(targetSuccessor)) system

                targetActor <! FindSuccesor(renewKeyId, chordId, MessageType.SYSTEM)
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

        | FindSuccesor(keyId: int, requestId: int, messageType: MessageType) ->
            // The request ancestor is requestId -> if find pass it back to requestId
            if isBetween(keyId, chordId, successor+1) then
                let requestActor = select ("/user/" + Convert.ToString(requestId)) system
                requestActor <! ConfirmSUCCESSOR(keyId, successor, messageType)
            else
                // unfound continue pass message to find it, pass on the message through the circle   
                let mutable passingChordId = chordId
                let mutable inFingerTable = false
                for i = systemParams.PowM-1 downto 0 do
                    if isBetween(finger.[i].Succesor, chordId, keyId) && not inFingerTable then
                        passingChordId <- finger.[i].Succesor
                        inFingerTable <- true

                let passNode = select ("/user/" + Convert.ToString(passingChordId)) system                         
                passNode <! FindSuccesor(keyId, requestId, messageType)

        | ConfirmSUCCESSOR(keyId: int, setSuccessorId: int, messageType: MessageType) ->
            // get id's successor as chordId
            if messageType = MessageType.SYSTEM then
                waitingSystemMsg <- waitingSystemMsg.Remove(keyId)
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

            elif messageType = MessageType.DATA then
                waitingDataMsg <- waitingDataMsg.Remove(keyId)
                recvRequest <- recvRequest + 1
                if serverNumber = prinfnServerNumber then
                    let task = async {
                        printfn "START from (ServerNum: %d, chordID %d), GET key %d stored on Server with chordID: %d" serverNumber chordId keyId setSuccessorId
                    }
                    Async.RunSynchronously(task)
                if recvRequest = systemParams.NumOfRequest then
                    selfActor <! STOP

        | Notify(targetChordId: int) ->
            if predecessor = -1 || isBetween(targetChordId, predecessor, chordId) then
                predecessor <- targetChordId

        | StartRequestTask ->
            let task = async {
                do! Async.Sleep requestDuration
                let randomKeyId = Random().Next(0, systemParams.NumOfIdentifier-1)
                if serverNumber = prinfnServerNumber then
                    printfn "Request key %d from (ServerNum: %d, chordID %d)" randomKeyId serverNumber chordId
                waitingDataMsg <- waitingDataMsg.Add(chordId)
                selfActor <! LOOKUP(randomKeyId, chordId)
                requestCount <- requestCount + 1
            }
            Async.RunSynchronously(task)
            if requestCount = systemParams.NumOfRequest then
                selfActor <! WAITING
            else
                selfActor <! StartRequestTask
        
        | LOOKUP(keyId: int, requestID: int) ->
            if keyId = chordId then
                let requestActor = select ("/user/" + Convert.ToString(requestID)) system
                requestActor <! ConfirmSUCCESSOR(keyId, successor, MessageType.DATA)
            selfActor <! FindSuccesor(keyId, requestID, MessageType.DATA) 

        | CheckPredecessor ->
            let checkTask = async {
                // check if predecessor if failing
                if predecessor <> -1 then
                    try
                        let predecessorActor = select ("/user/" + Convert.ToString(predecessor)) system
                        predecessorActor.Ask(isAlive, TimeSpan.FromSeconds(2.0)) |> ignore
                    with
                        | :? Akka.Actor.AskTimeoutException
                            -> printfn "SERVER [ServerNum: %d, chordID %d] predecessor %d is unreachable" serverNumber chordId predecessor 
            }
            checkTask |> Async.Start

        | WAITING -> ()
        
        | STOP -> 
             if serverNumber = prinfnServerNumber then
                let task = async {
                    printfn "END SERVER [ServerNum: %d, chordID %d], Finger Table:\n%A" serverNumber chordId finger
                }
                Async.RunSynchronously(task)
        return! loop()
    }
    loop()

let createNetwork(param) =
    match box param with
    | :? SystemParams as param ->
        for i = 1 to param.NumOfNodes do
            let mutable validChordId = false
            while not validChordId do
                try
                    let inputStr = createServerNumberStr(i)
                    let chordId = getNodeChordId(inputStr)
                    if i = 1 then 
                        firstServerChordID <- chordId
                    elif i = 2 then
                        secondServerChordID <- chordId
                    let chordIdStr = Convert.ToString(chordId)
                    // set id as the 
                    let task = async {
                        let networkNode = spawn system chordIdStr NodeFunction
                        networkNode <! INIT(i)
                    }
                    Async.RunSynchronously(task)
                    validChordId <- true
                with 
                    | :? Akka.Actor.InvalidActorNameException as ex -> printfn "Same ChordID Exception! %A " (ex.Message) 

    | _ ->  failwith "Invalid input variables to build a network"


let argv = fsi.CommandLineArgs
printfn "input arguments:\n%A" (argv) 

systemParams <- setInputs(argv)
printfn "system systemParams:\n%A" (systemParams)
prinfnServerNumber <- systemParams.NumOfNodes
createNetwork(systemParams)

System.Console.ReadLine() |> ignore