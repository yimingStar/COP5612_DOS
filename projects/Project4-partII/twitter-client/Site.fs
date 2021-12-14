namespace twitter_client

open WebSharper
open WebSharper.Sitelets
open WebSharper.UI
open WebSharper.UI.Server

type EndPoint =
    | [<EndPoint "/">] Home
    | [<EndPoint "/tweet-list">] TweetList

module Templating =
    open WebSharper.UI.Html

    // Compute a menubar where the menu item for the given endpoint is active
    let MenuBar (ctx: Context<EndPoint>) endpoint : Doc list =
        let ( => ) txt act =
             li [if endpoint = act then yield attr.``class`` "active"] [
                a [attr.href (ctx.Link act)] [text txt]
             ]
        [
            "Home" => EndPoint.Home
            "TweetList" => EndPoint.TweetList
        ]

    let Main ctx action (title: string) (body: Doc list) =
        Content.Page(
            Templates.MainTemplate()
                .Title(title)
                .MenuBar(MenuBar ctx action)
                .Body(body)
                .Doc()
        )

module Site =
    open WebSharper.UI.Html
    let HomePage ctx =
        // let clientWS = WebSocketModule.Create()
        WebSocketModule.Connect "1" |> ignore
        WebSocketModule.startSocketListener()
        
        Templating.Main ctx EndPoint.Home "Twitter Client" [
            h1 [] [text "Send Tweet"]
            div [] [client <@ Client.Main() @>]
        ]
        

    let TweetListPage ctx =
        Templating.Main ctx EndPoint.TweetList "Tweet List" [
            h1 [] [text "Tweet List"]
            p [] [text "This is a template WebSharper client-server application."]
        ]
    

    [<Website>]
    let Main =
        Application.MultiPage (fun ctx endpoint ->
            match endpoint with
            | EndPoint.Home -> HomePage ctx
            | EndPoint.TweetList -> TweetListPage ctx
        )
