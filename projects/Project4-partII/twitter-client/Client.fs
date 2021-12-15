namespace twitter_client

open WebSharper
open WebSharper.UI
open WebSharper.UI.Templating
open WebSharper.UI.Notation
open WebSharper.UI.Html
open WebSharper.UI.Client

[<JavaScript>]
module Templates =
    type MainTemplate = Templating.Template<"Main.html", ClientLoad.FromDocument, ServerLoad.WhenChanged>

[<JavaScript>]
module Client =
    let SignInComponent () =
        Templates.MainTemplate.SignInForm()
            .OnSend(fun e ->
                async {
                    CallApi.RequestSignIn e.Vars.TextUserId.Value |> ignore
                }
                |> Async.StartImmediate
            )
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
    
    let ResponseComponet () =
        let cls s = attr.``class`` s
        let rvText = Var.Create ""
        let inputField =
            div [cls "panel panel-default"] [
                div [cls "panel-heading"] [
                    h3 [ cls "panel-title"] [
                        text "Input"
                    ]
                ]
                div [cls "panel-body"] [
                    form [ cls "form-horizontal"; Attr.Create "role" "form"] [
                        div [ cls "form-group"] [
                            label [ cls "col-sm-4 control-label"; attr.``for`` "inputBox"] [
                                Doc.TextNode "Write something: "
                            ]
                            div [ cls "col-sm-8"] [
                                Doc.Input [cls "form-control"; attr.id "inputBox"] rvText
                            ]
                        ]
                    ]
                ]
            ]

        let view = View.FromVar rvText

        let viewCaps =
            view |> View.Map (fun s -> s.ToUpper())
        let viewReverse =
            view |> View.Map (fun s -> new string(Array.rev(s.ToCharArray())))
        let viewWordCount =
            view |> View.Map (fun s -> s.Split([| ' ' |]).Length)
        let viewWordCountStr =
            View.Map string viewWordCount
        let viewWordOddEven =
            View.Map (fun i -> if i % 2 = 0 then "Even" else "Odd") viewWordCount
        let viewMouseCoordinates =
            Input.Mouse.Position.Map (fun (x,y) -> sprintf "%d:%d" y x)
            
        let views =
            [
                ("Entered Text", view)
                ("Capitalised", viewCaps)
                ("Reversed", viewReverse)
                ("Word Count", viewWordCountStr)
                ("Word count odd or even?", viewWordOddEven)
                ("Mouse coordinates", viewMouseCoordinates)
            ]

        let tableRow (lbl, view) =
            tr [] [
                td [] [text lbl]
                td [ attr.style "width:66%"] [
                    textView view
                ]
            ]

        let tbl =
            div [ cls "panel panel-default"] [
                div [ cls "panel-heading"] [
                    h3 [ cls "panel-title"] [
                        text "Output"
                    ]
                ]
                div [ cls "panel-body"] [
                    table [ cls "table"] [
                        tbody [] [
                            List.map tableRow views |> Doc.Concat
                        ]
                    ]
                ]
            ]

        let content =
            div [] [
                inputField
                tbl
            ]

        content




        
