// Learn more about F# at http://docs.microsoft.com/dotnet/fsharp
open System
open Akka.Actor
open Akka.Configuration
open Akka.FSharp
open FSharp.Data
open FSharp.Json
open FSharp.Data.JsonExtensions
open ClientTypes

let mutable myUserObj: UserObject = {
    userId = ""
    account = ""
    subscribedList = []
    subscribers = []
    tweets = []
}

let mutable browsingTweetList: TweetObject list = []
let mutable ownTweetList: TweetObject list = []

let config = 
    Configuration.parse
        @"akka {
            actor.provider = remote
            remote {
                helios.tcp {
                    hostname = ""localhost""
                    port = 0
                }
            }
        }"


let mutable clientMainActor = null
let clientApp = System.create "twitterClient" (config)
let mutable clientActor = null
let JsonConfig = JsonConfig.create(allowUntyped = true)
let serverPath = "akka.tcp://twitterServer@localhost:5566/user/serverEngine"

let clientFunction (clientMailbox:Actor<String>) =
    let server = clientApp.ActorSelection(serverPath)
    let rec loop () = actor {
        let! (actionStr: String) = clientMailbox.Receive()
        let actionObj = Json.deserializeEx<MessageType> JsonConfig actionStr
        // printfn "receive obj: %A" actionObj
        
        match actionObj.action with
        | "CONNECT" ->
            printfn "[Send request] Send CONNECT to server"
            server <! actionStr

        | "REGISTER" -> 
            printfn "[Send request] Send REGISTER to server"
            server <! actionStr

        | "SUBSCRIBE" ->
            printfn "[Send request] Send SUBSCRIBE to server"
            server <! actionStr

        | "TWEET" ->
            printfn "[Send request] Send TWEET to server"
            server <! actionStr
            
        | "USER_DATA" ->
            printfn "[Recv response] Get USER_DATA %s\n" actionObj.data
            myUserObj <- Json.deserializeEx<UserObject> JsonConfig actionObj.data

        | "OWN_TWEET_DATA" -> 
            printfn "[Recv response] Get OWN_TWEET_DATA %s\n" actionObj.data
            ownTweetList <- Json.deserializeEx<TweetObject list> JsonConfig actionObj.data
            
        | "BROWSE_TWEET_DATA" ->
            printfn "[Recv response] Get BROWSE_TWEET_DATA %s\n" actionObj.data
            browsingTweetList <- Json.deserializeEx<TweetObject list> JsonConfig actionObj.data
        
        | "NEW_TWEET_DATA" ->
            printfn "[Recv response] Get NEW_TWEET_DATA %s\n" actionObj.data

        | "REQUIRE_USERID" ->
            printfn "[Recv response] Error Code 400 - Required REGISTER or SIGNIN"

        | "REQUIRE_ACCOUNT" ->
            printfn "[Recv response] Error Code 400 - Required REQUIRE_ACCOUNT"

        | _ -> printfn "[Invalid Action] client no action match %s" actionObj.action
        
        return! loop()
    }
    loop()

let rec readLinesFromConsole() =
    let server = clientApp.ActorSelection(serverPath) 
    let line = Console.ReadLine()
    if line <> null then
        let inputStrings = line.Split [|' '|]
        if inputStrings.Length > 0 then
            let setActionStr = inputStrings.[0]
            match setActionStr with
                | "CONNECT" ->
                    let userId = if inputStrings.Length > 1 then inputStrings.[1] else ""
                    printfn "[Recieve Action String] send CONNECT to client actor with userID: %s" userId

                    let inputData: CONNECTDATA = {
                        userId = userId
                    }

                    let sendRequest: MessageType = {
                        action = "CONNECT"
                        data = Json.serializeEx JsonConfig inputData
                    }

                    clientActor <! Json.serializeEx JsonConfig sendRequest
                    
                | "REGISTER" ->
                    let userAccount = if inputStrings.Length > 1 then inputStrings.[1] else ""
                    printfn "[Recieve Action String] send REGISTER to client actor with account: %s" userAccount
                    
                    let inputData: REGISTERDATA = {
                        account = userAccount
                    }

                    let sendRequest: MessageType = {
                        action = "REGISTER"
                        data = Json.serializeEx JsonConfig inputData
                    }

                    clientActor <! Json.serializeEx JsonConfig sendRequest

                | "SUBSCRIBE" ->
                    let targetUserId = if inputStrings.Length > 1 then inputStrings.[1] else ""
                    printfn "[Recieve Action String] send SUBSCRIBE to client actor with userID: %s" myUserObj.userId
                    
                    if myUserObj.userId = "" then
                        printfn "[Invalid Action] Error Code 401 - Unable SUBSCRIBE, please CONNECT or REGISTER first!"
                    else 
                        let inputData: SUBSCRIBEDATA = {
                            targeUserId = targetUserId
                            userId = myUserObj.userId
                        }

                        let sendRequest: MessageType = {
                            action = "SUBSCRIBE"
                            data = Json.serializeEx JsonConfig inputData
                        }
                        clientActor <! Json.serializeEx JsonConfig sendRequest

                | "TWEET" ->
                    let content = if inputStrings.Length > 1 then inputStrings.[1] else ""
                    printfn "[Recieve Action String] send TWEET to client actor with userID: %s" myUserObj.userId
                    
                    if myUserObj.userId = "" then
                        printfn "[Invalid Action] Error Code 401 - Unable SUBSCRIBE, please CONNECT or REGISTER first!"
                    else if content = "" then
                        printfn "[Invalid Action] Error Code 400 - Missing content"
                    else 
                        let inputData: TWEET_RAW_DATA = {
                            userId = myUserObj.userId
                            content = content
                        }

                        let sendRequest: MessageType = {
                            action = "TWEET"
                            data = Json.serializeEx JsonConfig inputData
                        }
                        clientActor <! Json.serializeEx JsonConfig sendRequest
                | "" -> ()
                | _ -> printfn "[Invalid Action] Error Code 403 - no action match: %s" setActionStr
        else 
            printfn "%s" line
    readLinesFromConsole ()

[<EntryPoint>]
let main argv =
    printfn "Client Start"
    clientMainActor <- spawn clientApp "client" clientFunction
    clientActor <- select ("/user/client") clientApp
    readLinesFromConsole()
    0 // return an integer exit code