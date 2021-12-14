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
// let mutable userConnectedMap = Map.empty // <userId, Akka path>


let deserializeUserData() =
    for user in loadedUserData do
        let key, value = user
        userDataMap <- userDataMap.Add(key, value |> string)


let deserializeTweetData() =
    for tweet in loadedTweets do
        let key, value = tweet
        tweetDataMap <- tweetDataMap.Add(key, value |> string)

let createErrors(errCode, content) = 
    let respError: ErrorType = {
        action = "error"
        code = errCode
        data = content
    }
    respError

let serverActionDecoder(msg) =
    let mutable response = ""
    let mutable errorCode = 0
    try
        let actionObj = Json.deserializeEx<MessageType> JsonConfig msg
        match actionObj.action with
        | "CONNECT" ->
            let data = Json.deserializeEx<CONNECTDATA> JsonConfig actionObj.data
            if data.userId = "" then
                printfn "Receive %s request without userId, request to registered" actionObj.action
                let resp: MessageType = {
                    action = "REQUIRE_USERID"
                    data = ""
                }
                response <- Json.serializeEx JsonConfig resp
        | _ -> printfn "[Invalid Action] server no action match %s" msg
    with ex ->
        printfn "exeption decoding client actions, ex: %A" ex
        errorCode <- 403
    
    if response.Length = 0 then
        response <- Json.serializeEx JsonConfig (createErrors(errorCode, ""))
    response

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
          let response = serverActionDecoder(str)
          printfn "%s" response

          // the response needs to be converted to a ByteSegment
          let byteResponse =
            response
            |> System.Text.Encoding.ASCII.GetBytes
            |> ByteSegment

          // the `send` function sends a message back to the client
          do! webSocket.send Suave.WebSocket.Opcode.Text byteResponse true

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


let app : WebPart = 
  choose [
    path "/websocket" >=> handShake ws
    path "/websocketWithSubprotocol" >=> handShakeWithSubprotocol (chooseSubprotocol "test") ws
    path "/websocketWithError" >=> handShake wsWithErrorHandling
    GET >=> choose [ path "/" >=> file "index.html"; browseHome ]
    NOT_FOUND "Found no handlers." ]


[<EntryPoint>]
let main argv =
    printfn "Twitter Server engine started. Now listening..."
    deserializeUserData()
    deserializeTweetData()
    startWebServer { defaultConfig with logger = Targets.create Verbose [||] } app
    0 // return an integer exit code