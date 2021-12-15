namespace twitter_client

open WebSharper
open WebSharper.UI
open WebSharper.UI.Templating
open WebSharper.UI.Notation
open WebSharper.UI.Html
open WebSharper.UI.Client
open WebSharper.JavaScript

[<JavaScript>]
module Templates =
    type MainTemplate = Templating.Template<"./html/Main.html", ClientLoad.FromDocument, ServerLoad.WhenChanged>

[<JavaScript>]
module Client =
    let SignUpComponent () =
        let rvResponse = Var.Create ""
        Templates.MainTemplate.RegisterForm()
            .OnSend(fun e ->
                async {
                    let! res = CallApi.RequestRegister e.Vars.Account.Value
                    rvResponse := res
                }
                |> Async.StartImmediate
            )
            .RESPONSE(rvResponse.View)
            .Doc()

    let SignInComponent () =
        let rvResponse = Var.Create ""
        Templates.MainTemplate.SignInForm()
            .OnSend(fun e ->
                async {
                    let! res = CallApi.RequestSignIn e.Vars.TextUserId.Value
                    rvResponse := res
                }
                |> Async.StartImmediate
            )
            .RESPONSE(rvResponse.View)
            .Doc()

    let TweetComponent () =
        let rvResponse = Var.Create ""
        Templates.MainTemplate.MainForm()
            .OnSend(fun e ->
                async {
                    let! res = CallApi.SendTweet e.Vars.TextTweet.Value
                    rvResponse := res
                }
                |> Async.StartImmediate
            )
            .RESPONSE(rvResponse.View)
            .Doc()

    let SubscribeComponent () =
        let rvResponse = Var.Create ""
        Templates.MainTemplate.SubScribeForm()
            .OnSend(fun e ->
                async {
                    let! res = CallApi.Subscribe e.Vars.TextSubscribeId.Value
                    rvResponse := res
                }
                |> Async.StartImmediate
            )
            .RESPONSE(rvResponse.View)
            .Doc()

    let SubscribedListComponent (inputList) =
        let rvResponse = Var.Create inputList
        
        Templates.MainTemplate.SubscribedList()
            .RESPONSE(rvResponse.View)
            .Doc()


    let SubscriberListComponent (inputList) =
        let rvResponse = Var.Create inputList
        
        Templates.MainTemplate.SubscriberList()
            .RESPONSE(rvResponse.View)
            .Doc()


    let OwnTweetListComponent (inputList) =
        let rvResponse = Var.Create inputList
        
        Templates.MainTemplate.OwnTweetList()
            .RESPONSE(rvResponse.View)
            .Doc()

    let BrowseTweetListComponent (inputList) =
        let rvResponse = Var.Create inputList

        Templates.MainTemplate.BrowseTweetList()
            .RESPONSE(rvResponse.View)
            .Doc()
        

        
    



        
