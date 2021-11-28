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
let userDataPath = __SOURCE_DIRECTORY__ + "/data/users.json"
let JsonConfig = JsonConfig.create(allowUntyped = true)
let mutable userDataMap = Map.empty
let loadedUserData = JsonValue.Load(userDataPath).Properties()

let deserializeUserData() =
    for user in loadedUserData do
        let key, value = user
        userDataMap <- userDataMap.Add(key, value |> string)

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
                        data = usersData
                    }
                    resp <- newResp
                with :? KeyNotFoundException as ex -> printfn "Exception! %A " (ex.Message) 

                printfn "resp %A" resp
                sender <! Json.serializeEx JsonConfig resp
        | "REGISTER" -> 
            let data = Json.deserializeEx<REGISTERDATA> JsonConfig actionObj.data
            if data.account = "" then
                printfn "Receive REGISTER request without account, request to resend"
                let resp: MessageType = {
                    action = "REQUIRE_ACCOUNT"
                    data = ""
                }
                sender <! Json.serializeEx JsonConfig resp
            else
                printfn "Receive REGISTER request with account %s, create user object and return" data.account
                
                // Create UseId
                let numUser = userDataMap |> Map.find "numUser" |> string |> int
                let newNumUser = numUser + 1
                let newUserId = sprintf "%s%d" userIdPrefix newNumUser

                printfn "check new userId: %s" newUserId
                // Create User Object and Store
                let newUserObj: UserObject = {
                    account = data.account
                    subscribedList = []
                    subscribers = []
                    tweets = []
                }

                let usersData = newUserObj |> string
                printfn "check new newUserObj: %A" newUserObj
                userDataMap <- userDataMap.Add(newUserId, usersData)
                userDataMap <- userDataMap.Add("numUser", newNumUser |> string)

                printfn "check new userDataMap: %A" userDataMap
                let newResp: MessageType = {
                    action = "RESULT_DATA"
                    data = usersData
                }
                sender <! Json.serializeEx JsonConfig newResp

        | _ -> printfn "[Invalid Action] server no action match %s" msg
        return! loop()
    }
    loop()

[<EntryPoint>]
let main argv =
    // create server main actor
    deserializeUserData()
    let serverMainActor = spawn serverSystem "serverEngine" serverEngine
    System.Console.ReadLine() |> ignore
    0 // return an integer exit code