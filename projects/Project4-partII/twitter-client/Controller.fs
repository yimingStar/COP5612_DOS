namespace twitter_client

open WebSharper
open FSharp.Data
open FSharp.Json
open FSharp.Data.JsonExtensions
open ClientTypes

module CallApi =
    let JsonConfig = JsonConfig.create(allowUntyped = true)

    [<Rpc>]
    let RequestRegister userAccount =
        let inputData: REGISTERDATA = {
            account = userAccount
        }

        let sendRequest: MessageType = {
            action = "REGISTER"
            data = Json.serializeEx JsonConfig inputData
        }
 
        WebSocketModule.Send(Json.serializeEx JsonConfig sendRequest) |> ignore

        async {
            do! Async.Sleep 500
            let mutable returnStr = ""
            if MessageHandler.errFlag = true then
                MessageHandler.clearErrorFlag() 
                returnStr <- MessageHandler.errorMsgObj.ToString()
            else
                returnStr <- MessageHandler.myUserObj.ToString()
            return returnStr
        }

    [<Rpc>]
    let RequestSignIn userId =
        let inputData: ClientTypes.CONNECTDATA = {
            userId = userId
        }

        let sendRequest: ClientTypes.MessageType = {
            action = "CONNECT"
            data = Json.serializeEx JsonConfig inputData
        }
 
        WebSocketModule.Send(Json.serializeEx JsonConfig sendRequest) |> ignore

        async {
            do! Async.Sleep 500
            let mutable returnStr = ""
            if MessageHandler.errFlag = true then
                MessageHandler.clearErrorFlag() 
                returnStr <- MessageHandler.errorMsgObj.ToString()
            else
                returnStr <- MessageHandler.myUserObj.ToString()
            return returnStr
        }
    
    [<Rpc>]
    let SendTweet input =
        let inputData: TWEET_RAW_DATA = {
            userId = MessageHandler.myUserObj.userId
            content = input
        }

        let sendRequest: MessageType = {
            action = "TWEET"
            data = Json.serializeEx JsonConfig inputData
        }

        WebSocketModule.Send(Json.serializeEx JsonConfig sendRequest) |> ignore
        async {
            do! Async.Sleep 500
            let mutable returnStr = ""
            if MessageHandler.errFlag = true then
                MessageHandler.clearErrorFlag() 
                returnStr <- MessageHandler.errorMsgObj.ToString()
            else
                returnStr <- MessageHandler.myUserObj.ToString()
            return returnStr
        }
    
        
    [<Rpc>]
    let Subscribe targetUserId =
        let inputData: SUBSCRIBEDATA = {
            targeUserId = targetUserId
            userId = MessageHandler.myUserObj.userId
        }

        let sendRequest: MessageType = {
            action = "SUBSCRIBE"
            data = Json.serializeEx JsonConfig inputData
        }

        WebSocketModule.Send(Json.serializeEx JsonConfig sendRequest) |> ignore
        
        async {
            do! Async.Sleep 500
            let mutable returnStr = ""
            if MessageHandler.errFlag = true then
                MessageHandler.clearErrorFlag() 
                returnStr <- MessageHandler.errorMsgObj.ToString()
            else
                returnStr <- MessageHandler.myUserObj.ToString()
            return returnStr
        }