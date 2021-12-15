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
    type MainTemplate = Templating.Template<"Main.html", ClientLoad.FromDocument, ServerLoad.WhenChanged>

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
                    let! res = CallApi.SendTweet e.Vars.TextToReverse.Value
                    rvResponse := res
                }
                |> Async.StartImmediate
            )
            .RESPONSE(rvResponse.View)
            .Doc()
        

        
    



        
