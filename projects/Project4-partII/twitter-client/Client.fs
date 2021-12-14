namespace twitter_client

open WebSharper
open WebSharper.UI
open WebSharper.UI.Templating
open WebSharper.UI.Notation

[<JavaScript>]
module Templates =
    type MainTemplate = Templating.Template<"Main.html", ClientLoad.FromDocument, ServerLoad.WhenChanged>

[<JavaScript>]
module Client =
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
        




        
