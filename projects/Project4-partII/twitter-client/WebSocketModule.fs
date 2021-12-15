namespace twitter_client

open System
open System.Net.WebSockets
open System.Text
open MessageHandler

module WebSocketModule = 

    let serverURI = "localhost:8080"
    let wsUri = sprintf "ws://%s/websocket" serverURI
    let mutable clientWS = null

    let Create() = 
        printfn "create client websocket"
        if clientWS = null then
            clientWS <- new ClientWebSocket()

    let Connect (uid: string) =
        printfn "check websocketstate %A" clientWS.State 
        if clientWS <> null && (clientWS.State = WebSocketState.None || clientWS.State = WebSocketState.Aborted) then 
            let tk = Async.DefaultCancellationToken
            printfn "websocket connect to server"
            Async.AwaitTask(clientWS.ConnectAsync(Uri(wsUri), tk)) |> ignore
    
    let Send (msg: string) =
        printfn "websocket send msg %s to server" msg
        let req = Encoding.UTF8.GetBytes msg
        let tk = Async.DefaultCancellationToken
        Async.AwaitTask(clientWS.SendAsync(ArraySegment(req), WebSocketMessageType.Text, true, tk))
    
    
    let startSocketListener() = 
        let loop = true
        async {
            while loop do
                try 
                    let buf = Array.zeroCreate 4096
                    let buffer = ArraySegment(buf)
                    let tk = Async.DefaultCancellationToken
                    let r =  Async.AwaitTask(clientWS.ReceiveAsync(buffer, tk)) |> Async.RunSynchronously
                    if not r.EndOfMessage then failwith "too lazy to receive more!"
                    let actionStr = Encoding.UTF8.GetString(buf, 0, r.Count)
                    if actionStr <> "" then
                        clientWSActionDecoder(actionStr)
                    else
                        printfn "Received empty response from server"
                with ex -> ()
                    // printfn "exeption receiving in websocket" ex
        } |> Async.Start

