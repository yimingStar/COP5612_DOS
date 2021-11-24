open System
open System.Security.Cryptography
open Akka.Actor
open Akka.Configuration
open Akka.FSharp
open FSharp.Data
open ServerTypes

let hostIP = "localhost"
let port = "9988" 
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

let serverSystem = System.create "twitter-server" (config)
let userDataPath = __SOURCE_DIRECTORY__ + "./data/users.json"
let userData = JsonValue.Load(userDataPath)

let serverEngine (serverMailbox:Actor<ServerActions>) =
    let mutable selfActor = select ("") serverSystem
    let rec loop () = actor {
        let! (msg: ServerActions) = serverMailbox.Receive()
        match msg with
        | START -> 
            printfn "Sever Engine Start"
            printfn "user data %A" userData
            ()
        | SIGNIN(userID: string) ->
            printfn "UserID: %s has signed in" userID 

        | REGISTER(account: string) ->
            printfn "User with account: %s has register for new accoun" account 
            
        // | SUBSCRIBE of int // client subscribe to userID
        // | StopSUBSCRIBE of int
        // | PostTWEET of int*string*System.Array*System.Array // userID, tweet content, hashtags<string>, metioned<userID>
        | CONNECTED(userID: string) ->
            printfn "UserID: %s has connected" userID

        return! loop()
    }
    loop()

[<EntryPoint>]
let main argv =
    // create server main actor
    let serverMainActor = spawn serverSystem "server-engine" serverEngine
    serverMainActor <! START
    System.Console.ReadLine() |> ignore
    0 // return an integer exit code