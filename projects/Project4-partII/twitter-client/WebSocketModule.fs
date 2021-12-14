namespace twitter_client

open System
open System.Net.WebSockets

module WebSocketModule = 
    let serverURI = "localhost:8080"
    let wsUri = sprintf "ws://%s/websocket" serverURI

    let Create() = 
        printfn "create client websocket"
        let newWS = new ClientWebSocket()
        newWS

    let Connect (uid: string) (ws: ClientWebSocket) = 
        let tk = Async.DefaultCancellationToken
        printfn "websocket connect to server"
        // Async.AwaitTask(ws.ConnectAsync(Uri(sprintf "%s/%s" serverURI uid), tk))
        Async.AwaitTask(ws.ConnectAsync(Uri(wsUri), tk))

