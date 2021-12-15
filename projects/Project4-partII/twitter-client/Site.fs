namespace twitter_client

open WebSharper
open WebSharper.Sitelets
open WebSharper.UI
open WebSharper.UI.Server

type EndPoint =
    | [<EndPoint "/">] Home
    | [<EndPoint "/tweet-list">] TweetList
    | [<EndPoint "/subscribe-list">] SubscribeList

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
            "SubscribeList" => EndPoint.SubscribeList
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
    open WebSharper.JavaScript

    let HomePage ctx =
        WebSocketModule.Create()
        WebSocketModule.Connect "1" |> ignore
        WebSocketModule.startSocketListener()
        let userData = MessageHandler.myUserObj.ToString()

        Templating.Main ctx EndPoint.Home "Twitter Client" [
            div [] [client <@ Client.UserInfoComponent(userData) @>]

            h1 [] [text "Sign Up"]
            div [] [client <@ Client.SignUpComponent() @>]

            h1 [] [text "Sign In"]
            div [] [client <@ Client.SignInComponent() @>]

            h1 [] [text "SubScribe"]
            div [] [client <@ Client.SubscribeComponent() @>]

            h1 [] [text "Send Tweet"]
            div [] [client <@ Client.TweetComponent() @>]
        ]

    let TweetListPage ctx =
        let showOwnTweets = MessageHandler.ownTweetList.ToString()
        let showBrowseTweets = MessageHandler.browsingTweetList.ToString()
        Templating.Main ctx EndPoint.TweetList "Tweet List" [
            h1 [] [text "Own Tweet List"]
            div [] [client <@ Client.OwnTweetListComponent(showOwnTweets) @>]

            h1 [] [text "Browse Tweet List"]
            div [] [client <@ Client.BrowseTweetListComponent(showBrowseTweets) @>]
        ]
    
    let SubscribeListPage ctx =
        let showSubscribers = MessageHandler.myUserObj.subscribers.ToString()
        let showSubscribed = MessageHandler.myUserObj.subscribedList.ToString()

        Templating.Main ctx EndPoint.SubscribeList "Subscribe List" [
            h1 [] [text "Subscriber List"]
            div [] [client <@ Client.SubscriberListComponent(showSubscribers) @>]

            h1 [] [text "Subscribed List"]
            div [] [client <@ Client.SubscribedListComponent(showSubscribed) @>]
        ]
    

    [<Website>]
    let Main =
        Application.MultiPage (fun ctx endpoint ->
            match endpoint with
            | EndPoint.Home -> HomePage ctx
            | EndPoint.TweetList -> TweetListPage ctx
            | EndPoint.SubscribeList -> SubscribeListPage ctx
        )
