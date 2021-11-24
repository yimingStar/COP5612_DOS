// Learn more about F# at http://docs.microsoft.com/dotnet/fsharp
open System
open Akka.Actor
open Akka.Configuration
open Akka.FSharp
open FSharp.Data
open FSharp.Json
open ClientTypes

let config = 
    Configuration.parse
        @"akka {
            actor.provider = remote
            remote {
                helios.tcp {
                    hostname = ""localhost""
                    port = 0
                }
            }
        }"


let mutable clientMainActor = null
let clientApp = System.create "twitterClient" (config)
let mutable clientActor = null
let JsonConfig = JsonConfig.create(allowUntyped = true)

let clientFunction (clientMailbox:Actor<ClientActions>) =
    let server = clientApp.ActorSelection("akka.tcp://twitterServer@localhost:5566/user/serverEngine")

    let mutable selfActor = select ("") clientApp
    let rec loop () = actor {
        let! (actionStr: ClientActions) = clientMailbox.Receive()
        match actionStr with
        | RequestCONNECTED(userID: string) ->
            printfn "[Send action to server] CONNECT with userID: %s" userID
            let request: MessageType = {
                action = "CONNECT"
                data = Null
            }
            server <! Json.serializeEx JsonConfig request
        return! loop()
    }
    loop()

let rec readLinesFromConsole() = 
    let line = Console.ReadLine()
    if line <> null then
        let inputStrings = line.Split [|' '|]
        printfn "Your input is: %A" inputStrings
        if inputStrings.Length > 0 then
            let setActionStr = inputStrings.[0]
            match setActionStr with
                | "CONNECT" ->
                    let userId = if inputStrings.Length > 1 then inputStrings.[1] else ""
                    printfn "[Recieve Action String] send CONNECT to client actor with userID: %s" userId
                    clientActor <! RequestCONNECTED(userId)

                | _ -> printfn "[Invalid Action] no action match: %s" setActionStr
        else 
            printfn "%s" line
    readLinesFromConsole ()

[<EntryPoint>]
let main argv =
    printfn "Client Start"
    clientMainActor <- spawn clientApp "client" clientFunction
    clientActor <- select ("/user/client") clientApp
    readLinesFromConsole()
    0 // return an integer exit code