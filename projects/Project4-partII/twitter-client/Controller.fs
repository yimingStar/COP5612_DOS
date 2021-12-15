namespace twitter_client

open WebSharper
open FSharp.Data
open FSharp.Json
open FSharp.Data.JsonExtensions
open ClientTypes

module CallApi =
    let JsonConfig = JsonConfig.create(allowUntyped = true)
    [<Rpc>]
    let RequestSignIn input =
        let inputUserId = input

        let inputData: ClientTypes.CONNECTDATA = {
            userId = inputUserId
        }

        let sendRequest: ClientTypes.MessageType = {
            action = "CONNECT"
            data = Json.serializeEx JsonConfig inputData
        }
 
        WebSocketModule.Send(Json.serializeEx JsonConfig sendRequest) |> ignore
        async {
            return WebSocketModule.GetResp()
        }
    
    [<Rpc>]
    let SendTweet input =
        let R (s: string) = System.String(Array.rev(s.ToCharArray()))
        WebSocketModule.Send(R input) |> ignore
        async {
            return R input
        }