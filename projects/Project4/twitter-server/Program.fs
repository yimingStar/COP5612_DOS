open System
open Akka.Actor
open Akka.Configuration
open Akka.FSharp
open FSharp.Data
open FSharp.Json
open ServerTypes
open System.Collections.Generic

let hostIP = "localhost"
let port = "5566" 
let config =
    ConfigurationFactory.ParseString(
            @"akka {
                actor.provider = remote
                remote.helios.tcp {
                    hostname = " + hostIP + "
                    port = " + port + "
                }
            }"
        )

let serverSystem = System.create "twitterServer" (config)
let userDataPath = __SOURCE_DIRECTORY__ + "./data/users.json"
let JsonConfig = JsonConfig.create(allowUntyped = true)
let mutable userDataMap = Map.empty
let loadedUserData = JsonValue.Load(userDataPath).Properties()
printfn "loadedUserData: %A" loadedUserData 

let deserializeUserData() =
    for user in loadedUserData do
        printfn "add User: %A" user
        let key, value = user
        userDataMap <- userDataMap.Add(key, value)

let serverEngine (serverMailbox:Actor<String>) =
    let mutable selfActor = select ("") serverSystem
    let rec loop () = actor {
        let! (msg: String) = serverMailbox.Receive()
        let sender = serverMailbox.Sender()
        let actionObj = Json.deserializeEx<MessageType> JsonConfig msg
        printfn "receive obj: %A" actionObj
        match actionObj.action with
        | "CONNECT" ->
            let data = Json.deserializeEx<CONNECTDATA> JsonConfig actionObj.data
            if data.userId = "" then
                printfn "Receive CONNECT request without userId, request to registered"
                let resp: MessageType = {
                    action = "REQUIRE_USERID"
                    data = ""
                }
                sender <! Json.serializeEx JsonConfig resp
            else
                let mutable resp: MessageType = {
                    action = "REQUIRE_USERID"
                    data = ""
                }
                printfn "Receive CONNECT request from userId %s, return user object and main posts" data.userId
                try
                    deserializeUserData()
                    printfn "userDataMap: %A" userDataMap
                    let usersData = userDataMap |> Map.find data.userId 
                    let newResp: MessageType = {
                        action = "RESULT_DATA"
                        data = usersData.ToString()
                    }
                    resp <- newResp
                with :? KeyNotFoundException as ex -> printfn "Exception! %A " (ex.Message) 

                printfn "resp %A" resp
                sender <! Json.serializeEx JsonConfig resp

        | _ -> printfn "[Invalid Action] server no action match %s" msg
        return! loop()
    }
    loop()

[<EntryPoint>]
let main argv =
    // create server main actor
    let serverMainActor = spawn serverSystem "serverEngine" serverEngine
    System.Console.ReadLine() |> ignore
    0 // return an integer exit code