// Learn more about F# at http://docs.microsoft.com/dotnet/fsharp
open Suave
open Suave.Http
open Suave.Operators
open Suave.Filters
open Suave.Successful
open Suave.Files
open Suave.RequestErrors
open Suave.Logging
open Suave.Utils

open System
open System.Net

open Suave.Sockets
open Suave.Sockets.Control
open Suave.WebSocket

open FSharp.Data
open FSharp.Json
open FSharp.Data.JsonExtensions

open ServerTypes
open System.Collections.Generic
open System.Security.Cryptography

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
let mutable rsaPublicKey = ""
let mutable rsaPrivateKey = ""

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
    
let createErrors(errCode, content) = 
    let respError: ErrorType = {
        action = "error"
        code = errCode
        data = content
    }
    respError

let createKey() =
    let rsaProvider = new RSACryptoServiceProvider(2048)
    rsaPublicKey <- Convert.ToBase64String(rsaProvider.ExportCspBlob(false));
    rsaPrivateKey <- Convert.ToBase64String(rsaProvider.ExportCspBlob(true));

let sendByWebSocket(msg: string, webSocket: WebSocket) = 
    // the response needs to be converted to a ByteSegment
    let byteResponse =
      msg
      |> System.Text.Encoding.ASCII.GetBytes
      |> ByteSegment

    // the `send` function sends a message back to the client
    webSocket.send Suave.WebSocket.Opcode.Text byteResponse true |> Async.RunSynchronously
    

let serverWSActionDecoder(msg: string, webSocket: WebSocket) =
    let mutable response = ""
    try
        let actionObj = Json.deserializeEx<MessageType> JsonConfig msg
        printfn "Receive Message %A" actionObj
        match actionObj.action with
        | "REGISTER" ->
            let data = Json.deserializeEx<REGISTERDATA> JsonConfig actionObj.data
            if data.account = "" then
                printfn "Invalid Action %s, request to resend" actionObj.action
                let errResp: ErrorType = {
                    action = "ERROR"
                    code = 403
                    data = "Missing account, invalid registration"
                }
                response <- Json.serializeEx JsonConfig errResp
                sendByWebSocket(response, webSocket) |> ignore
            else
                printfn "Receive %s request with account %s, create user object and return" actionObj.action data.account
                
                // Create UseId
                let newNumUsers = settingObj.numUsers + 1
                // let newUserId = sprintf "%s%d" userIdPrefix (newNumUsers)
                let newUserId = data.account

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
                response <- Json.serializeEx JsonConfig resp
                sendByWebSocket(response, webSocket) |> ignore

                let mutable keyMsg: MessageType = {
                    action = "KEY_DATA"
                    data = rsaPublicKey
                }
                response <- Json.serializeEx JsonConfig keyMsg
                sendByWebSocket(response, webSocket) |> ignore

                let getUserJson = JsonValue.Parse(usersDataStr)
                
                let respTweet: MessageType = {
                    action = "OWN_TWEET_DATA"
                    data = getUserTweets(getUserJson)
                }

                response <- Json.serializeEx JsonConfig respTweet
                sendByWebSocket(response, webSocket) |> ignore

                let respTweet: MessageType = {
                    action = "BROWSE_TWEET_DATA"
                    data = getBrowseTweets()
                }
                response <- Json.serializeEx JsonConfig respTweet
                sendByWebSocket(response, webSocket) |> ignore

        | "CONNECT" ->
            let data = Json.deserializeEx<CONNECTDATA> JsonConfig actionObj.data
            if data.userId = "" then
                printfn "Receive %s request without userId, request to registered" actionObj.action
                let errResp: ErrorType = {
                    action = "ERROR"
                    code = 401
                    data = "Request without userId"
                }
                response <- Json.serializeEx JsonConfig errResp
                sendByWebSocket(response, webSocket) |> ignore
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
                    printfn "Receive CONNECT from userId %s on by web socket" data.userId 
                    userConnectedMap <- userConnectedMap.Add(data.userId, webSocket)
                with :? KeyNotFoundException as ex -> printfn "Exception! %A " (ex.Message) 

                response <- Json.serializeEx JsonConfig resp
                sendByWebSocket(response, webSocket) |> ignore

                let mutable keyMsg: MessageType = {
                    action = "KEY_DATA"
                    data = rsaPublicKey
                }
                response <- Json.serializeEx JsonConfig keyMsg
                sendByWebSocket(response, webSocket) |> ignore

                let getUserJson = JsonValue.Parse(usersDataStr)
                
                let respTweet: MessageType = {
                    action = "OWN_TWEET_DATA"
                    data = getUserTweets(getUserJson)
                }

                response <- Json.serializeEx JsonConfig respTweet
                sendByWebSocket(response, webSocket) |> ignore

                let respTweet: MessageType = {
                    action = "BROWSE_TWEET_DATA"
                    data = getBrowseTweets()
                }
                response <- Json.serializeEx JsonConfig respTweet
                sendByWebSocket(response, webSocket) |> ignore

         | "SUBSCRIBE" -> 
            let data = Json.deserializeEx<SUBSCRIBEDATA> JsonConfig actionObj.data
            if data.targeUserId = "" || data.userId = "" then
                if data.userId = "" then
                    printfn "Receive %s request without userId, request to registered" actionObj.action
                    let errResp: ErrorType = {
                        action = "ERROR"
                        code = 401
                        data = "Request without userId"
                    }
                    response <- Json.serializeEx JsonConfig errResp
                    sendByWebSocket(response, webSocket) |> ignore
                else if data.targeUserId = "" then 
                    printfn "Receive %s request without targeUserId, bad request" actionObj.action
                    let errResp: ErrorType = {
                        action = "ERROR"
                        code = 403
                        data = "Missing subscribe userid"
                    }
                    response <- Json.serializeEx JsonConfig errResp
                    sendByWebSocket(response, webSocket) |> ignore
            else
                printfn "Receive %s request to sucribe to userId %s" actionObj.action data.targeUserId

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

                response <- Json.serializeEx JsonConfig resp
                sendByWebSocket(response, webSocket) |> ignore

        | "TWEET" -> 
            // 1. Create new Tweet obj, update tweet list
            // 2. Update user tweet list
            // 3. Broadcast to all suscribers
            
            let data = Json.deserializeEx<TWEET_RAW_DATA> JsonConfig actionObj.data
            if data.userId = "" then
                printfn "Receive %s request without userId, request to registered" actionObj.action
                let errResp: ErrorType = {
                    action = "ERROR"
                    code = 401
                    data = "Request without userId"
                }
                response <- Json.serializeEx JsonConfig errResp
                sendByWebSocket(response, webSocket) |> ignore
            else
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
                            let subscriberWS = userConnectedMap.[subsribersId]

                            let mutable tweetMsg: MessageType = {
                                action = "NEW_TWEET_DATA"
                                data = tweetDataStr
                            }

                            response <- Json.serializeEx JsonConfig tweetMsg
                            sendByWebSocket(response, subscriberWS) |> ignore

                with :? KeyNotFoundException as ex -> printfn "Exception! %A " (ex.Message) 

                let mutable resp: MessageType = {
                    action = "USER_DATA"
                    data = usersDataStr
                }

                response <- Json.serializeEx JsonConfig resp
                sendByWebSocket(response, webSocket) |> ignore

                let getUserJson = JsonValue.Parse(usersDataStr)   
                let respTweet: MessageType = {
                    action = "OWN_TWEET_DATA"
                    data = getUserTweets(getUserJson)
                }
                response <- Json.serializeEx JsonConfig respTweet
                sendByWebSocket(response, webSocket) |> ignore

        | _ -> printfn "[Invalid Action] server no action match %s" msg
    with ex ->
        printfn "exeption decoding client actions, ex: %A" ex
        let errResp: ErrorType = {
            action = "ERROR"
            code = 403
            data = ""
        }
        response <- Json.serializeEx JsonConfig errResp
        sendByWebSocket(response, webSocket) |> ignore

let ws (webSocket : WebSocket) (context: HttpContext) =
    
    socket {
    // if `loop` is set to false, the server will stop receiving messages
      let mutable loop = true

      while loop do
        // the server will wait for a message to be received without blocking the thread
        let! msg = webSocket.read()

        match msg with
        // the message has type (Opcode * byte [] * bool)
        //
        // Opcode type:
        //   type Opcode = Continuation | Text | Binary | Reserved | Close | Ping | Pong
        //
        // byte [] contains the actual message
        //
        // the last element is the FIN byte, explained later
        | (Suave.WebSocket.Opcode.Text, data, true) ->
          // the message can be converted to a string
          let str = UTF8.toString data
          serverWSActionDecoder(str, webSocket) |> ignore

        | (Close, _, _) ->
          let emptyResponse = [||] |> ByteSegment
          do! webSocket.send Close emptyResponse true

          // after sending a Close message, stop the loop
          loop <- false

        | _ -> ()
    }

/// An example of explictly fetching websocket errors and handling them in your codebase.
let wsWithErrorHandling (webSocket : WebSocket) (context: HttpContext) = 
   
   let exampleDisposableResource = { new IDisposable with member __.Dispose() = printfn "Resource needed by websocket connection disposed" }
   let websocketWorkflow = ws webSocket context
   
   async {
    let! successOrError = websocketWorkflow
    match successOrError with
    // Success case
    | Choice1Of2() -> ()
    // Error case
    | Choice2Of2(error) ->
        // Example error handling logic here
        printfn "Error: [%A]" error
        exampleDisposableResource.Dispose()
        
    return successOrError
   }


let getArgsFromJsonString jsonStr =
  Json.deserializeEx<MessageType> JsonConfig jsonStr

let getString (rawForm: byte[]) = System.Text.Encoding.UTF8.GetString(rawForm)

let parseHttpArgs (req : HttpRequest) =
    req.rawForm |> getString |> getArgsFromJsonString

let ConnectUser (req: HttpRequest) = 
    printfn "REST API Hit: %s" req.url.OriginalString
    let actionObj = parseHttpArgs req
    if actionObj.action = "CONNECT" then
        let resp: MessageType = {
            action = "REQUIRE_USERID"
            data = ""
        }
        OK (Json.serializeEx JsonConfig resp)
    else
      printfn "Receive %s request without userId, request to registered" actionObj.action
      let resp: MessageType = {
          action = "REQUIRE_USERID"
          data = ""
      }
      NOT_ACCEPTABLE (Json.serializeEx JsonConfig resp) 


let app : WebPart = 
  choose [
    path "/websocket" >=> handShake ws
    path "/websocketWithSubprotocol" >=> handShakeWithSubprotocol (chooseSubprotocol "test") ws
    path "/websocketWithError" >=> handShake wsWithErrorHandling
    GET >=> choose [ path "/" >=> file "index.html"; browseHome ]
    POST >=> choose [ 
      path "/test" >=> OK "Hello POST!"
      path "/connect" >=> request ConnectUser
    ]
    NOT_FOUND "Found no handlers." ]


[<EntryPoint>]
let main argv =
    printfn "Twitter Server engine started. Now listening..."
    createKey()
    deserializeUserData()
    deserializeTweetData()
    startWebServer { defaultConfig with logger = Targets.create Verbose [||] } app
    0 // return an integer exit code