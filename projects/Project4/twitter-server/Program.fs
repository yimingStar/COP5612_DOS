open System
open Akka.Actor
open Akka.Configuration
open Akka.FSharp
open FSharp.Data
open FSharp.Json
open FSharp.Data.JsonExtensions
open ServerTypes
open System.Collections.Generic

let hostIP = "localhost"
let port = "5566" 
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

let apiSizeLimit = 5
let serverSystem = System.create "twitterServer" (config)
let userDataPath = __SOURCE_DIRECTORY__ + "/data/users.json"
let tweetDataPath = __SOURCE_DIRECTORY__ + "/data/tweets.json"
let settingPath = __SOURCE_DIRECTORY__ + "/data/setting.json"

let JsonConfig = JsonConfig.create(allowUntyped = true)
let mutable userDataMap = Map.empty
let mutable tweetDataMap = Map.empty
let loadedUserData = JsonValue.Load(userDataPath).Properties
let loadedTweets = JsonValue.Load(tweetDataPath).Properties
let loadedSetting = JsonValue.Load(settingPath) |> string
let settingObj = Json.deserializeEx<ServerSettings> JsonConfig loadedSetting
let mutable userConnectedMap = Map.empty // <userId, Akka path>

let deserializeUserData() =
    for user in loadedUserData do
        let key, value = user
        userDataMap <- userDataMap.Add(key, value |> string)


let deserializeTweetData() =
    for tweet in loadedTweets do
        let key, value = tweet
        tweetDataMap <- tweetDataMap.Add(key, value |> string)

// let updateSubsribers
let getUserTweets(getUserJson) =
    let mutable tweetDataArr = []
    for tweedId in getUserJson?tweets do
        tweetDataArr <- tweetDataArr @ [tweetDataMap |> Map.find (tweedId.AsString())]
    
    let mutable tweetsStr = (", ", tweetDataArr) |> System.String.Join
    tweetsStr <- "[" + tweetsStr + "]"
    tweetsStr

let getBrowseTweets() =
    let mutable tweetDataArr = [] 
    tweetDataMap |> Map.iter (fun key value -> tweetDataArr <- tweetDataArr @ [value])

    let mutable tweetsStr = (", ", tweetDataArr) |> System.String.Join
    tweetsStr <- "[" + tweetsStr + "]"
    tweetsStr


let serverEngine (serverMailbox:Actor<String>) =
    let rec loop () = actor {
        let! (msg: String) = serverMailbox.Receive()
        let sender = serverMailbox.Sender()
        let actionObj = Json.deserializeEx<MessageType> JsonConfig msg
        printfn "receive obj: %A" actionObj
        match actionObj.action with
        | "CONNECT" ->
            let data = Json.deserializeEx<CONNECTDATA> JsonConfig actionObj.data
            if data.userId = "" then
                printfn "Receive %s request without userId, request to registered" actionObj.action
                let resp: MessageType = {
                    action = "REQUIRE_USERID"
                    data = ""
                }
                sender <! Json.serializeEx JsonConfig resp
            else
                let mutable resp: MessageType = {
                    action = "REQUIRE_USERID"
                    data = ""
                }
                printfn "Receive CONNECT request from userId %s, return user object and main posts" data.userId
                let mutable usersDataStr = ""
                try
                    usersDataStr <- userDataMap |> Map.find data.userId 
                    let newResp: MessageType = {
                        action = "USER_DATA"
                        data = usersDataStr
                    }
                    resp <- newResp
                    printfn "Receive CONNECT from userId %s with path %s" data.userId (sender.Path.ToString())
                    userConnectedMap <- userConnectedMap.Add(data.userId, sender.Path.ToString())
                with :? KeyNotFoundException as ex -> printfn "Exception! %A " (ex.Message) 

                printfn "resp %A" resp
                sender <! Json.serializeEx JsonConfig resp

                let getUserJson = JsonValue.Parse(usersDataStr)
                
                let respTweet: MessageType = {
                    action = "OWN_TWEET_DATA"
                    data = getUserTweets(getUserJson)
                }
                sender <! Json.serializeEx JsonConfig respTweet

                let respTweet: MessageType = {
                    action = "BROWSE_TWEET_DATA"
                    data = getBrowseTweets()
                }
                sender <! Json.serializeEx JsonConfig respTweet
                

        | "REGISTER" -> 
            let data = Json.deserializeEx<REGISTERDATA> JsonConfig actionObj.data
            if data.account = "" then
                printfn "Invalid Action %s, request to resend" actionObj.action
                let resp: MessageType = {
                    action = "REQUIRE_ACCOUNT"
                    data = ""
                }
                sender <! Json.serializeEx JsonConfig resp
            else
                printfn "Receive %s request with account %s, create user object and return" actionObj.action data.account
                
                // Create UseId
                let newNumUsers = settingObj.numUsers + 1
                let newUserId = sprintf "%s%d" userIdPrefix (newNumUsers)

                printfn "check new userId: %s" newUserId
                // Create User Object and Store
                let newUserObj: UserObject = {
                    userId = newUserId
                    account = data.account
                    subscribedList = []
                    subscribers = []
                    tweets = []
                }

                let usersDataStr = Json.serializeEx JsonConfig newUserObj
                // printfn "check new newUserObj: %A" newUserObj
                userDataMap <- userDataMap.Add(newUserId, usersDataStr)
                settingObj.numUsers <- newNumUsers

                // printfn "check new userDataMap: %A" userDataMap
                let mutable resp: MessageType = {
                    action = "USER_DATA"
                    data = usersDataStr
                }
                sender <! Json.serializeEx JsonConfig resp
                
        | "SUBSCRIBE" -> 
            let data = Json.deserializeEx<SUBSCRIBEDATA> JsonConfig actionObj.data
            if data.targeUserId = "" || data.userId = "" then
                printfn "Invalid Action %s, request to resend" actionObj.action
                let resp: MessageType = {
                    action = "REQUIRE_USERID"
                    data = ""
                }
                sender <! Json.serializeEx JsonConfig resp
            else
                printfn "Receive %s request to userId %s" actionObj.action data.targeUserId

                let mutable usersDataStr = ""
                let mutable targetDataStr = ""
                try
                    // update user subscribedList and update target user subscribers
                    usersDataStr <- userDataMap |> Map.find data.userId
                    let userObj = Json.deserializeEx<UserObject> JsonConfig (usersDataStr) 
                    userObj.subscribedList <- userObj.subscribedList @ [data.targeUserId]
                    
                    targetDataStr <- userDataMap |> Map.find data.targeUserId 
                    let mutable targetUserObj = Json.deserializeEx<UserObject> JsonConfig (targetDataStr) 
                    targetUserObj.subscribers <- targetUserObj.subscribers @ [data.userId]


                    targetDataStr <- Json.serializeEx JsonConfig targetUserObj
                    usersDataStr <- Json.serializeEx JsonConfig userObj

                    userDataMap <- userDataMap.Add(data.targeUserId, targetDataStr)
                    userDataMap <- userDataMap.Add(data.userId, usersDataStr)

                    usersDataStr <- Json.serializeEx JsonConfig userObj
                with :? KeyNotFoundException as ex -> printfn "Exception! %A " (ex.Message) 
                
                let mutable resp: MessageType = {
                    action = "USER_DATA"
                    data = usersDataStr
                }
                sender <! Json.serializeEx JsonConfig resp
                
        | "TWEET" -> 
            // 1. Create new Tweet obj, update tweet list
            // 2. Update user tweet list
            // 3. Broadcast to all suscribers

            let data = Json.deserializeEx<TWEET_RAW_DATA> JsonConfig actionObj.data

            // 1. Create new Tweet obj, update tweet list
            let newNumTweets = settingObj.numTweets + 1 
            let newTweetId = sprintf "%s%d" tweetIdPrefix newNumTweets

            let newTweetObj: TweetObject = {
                userId = data.userId
                tweetId = newTweetId
                content = data.content
                hashTag = []
                mention = []
            }
            
            printfn ""
            let tweetDataStr = Json.serializeEx JsonConfig newTweetObj 
            tweetDataMap <- tweetDataMap.Add(newTweetId, tweetDataStr)
            settingObj.numTweets <- newNumTweets 
            
            let mutable usersDataStr = ""
            try
                // 2. Update user tweet list
                usersDataStr <- userDataMap |> Map.find data.userId
                let userObj = Json.deserializeEx<UserObject> JsonConfig (usersDataStr) 
                userObj.tweets <- userObj.tweets @ [newTweetId]
                usersDataStr <- Json.serializeEx JsonConfig userObj
                
                // 3. Broadcast to subscribers
                for subsribersId in userObj.subscribers do
                    printfn "%s" subsribersId
                    if userConnectedMap.ContainsKey(subsribersId) then
                        let subscriberPath = userConnectedMap.[subsribersId]
                        let subsriberActor = select (subscriberPath) serverSystem

                        let mutable tweetMsg: MessageType = {
                            action = "NEW_TWEET_DATA"
                            data = tweetDataStr
                        }
                        subsriberActor <! Json.serializeEx JsonConfig tweetMsg

            with :? KeyNotFoundException as ex -> printfn "Exception! %A " (ex.Message) 

            let mutable resp: MessageType = {
                action = "USER_DATA"
                data = usersDataStr
            }
            sender <! Json.serializeEx JsonConfig resp

        | _ -> printfn "[Invalid Action] server no action match %s" msg
        return! loop()
    }
    loop()

[<EntryPoint>]
let main argv =
    // create server main actor
    deserializeUserData()
    deserializeTweetData()
    let serverMainActor = spawn serverSystem "serverEngine" serverEngine
    System.Console.ReadLine() |> ignore
    0 // return an integer exit code