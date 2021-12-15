namespace twitter_client

open FSharp.Data
open FSharp.Json
open FSharp.Data.JsonExtensions
open ClientTypes

module MessageHandler = 
    let mutable myUserObj: UserObject = {
        userId = ""
        account = ""
        subscribedList = []
        subscribers = []
        tweets = []
    }

    let mutable errFlag = false
    let mutable errorMsgObj: ErrorType = {
        action = "Error"
        code = 0
        data = ""
    }

    let mutable browsingTweetList: TweetObject list = []
    let mutable ownTweetList: TweetObject list = []
    let JsonConfig = JsonConfig.create(allowUntyped = true)

    let clientWSActionDecoder(msg: string) =
        try
            let actionObj = Json.deserializeEx<MessageType> JsonConfig msg
            printfn "Received actionObj from server: %A" actionObj
            match actionObj.action with
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

            | "ERROR" ->
                printfn "[Recv Error response]"
                errFlag <- true
                errorMsgObj <- Json.deserializeEx<ErrorType> JsonConfig msg

            | _ -> printfn "[Invalid Action] client no action match %s" actionObj.action
        with ex ->
            printfn "exeption decoding ws actions, ex: %A" ex
    
    let clearErrorFlag() =
        errFlag <- false
    