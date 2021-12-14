namespace twitter_client

open WebSharper

module CallApi =
    [<Rpc>]
    let DoSomething input =
        let R (s: string) = System.String(Array.rev(s.ToCharArray()))
        WebSocketModule.Send(R input) |> ignore
        async {
            return R input
        }
