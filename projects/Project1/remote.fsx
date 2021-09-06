#time "on"
#load "packages.fsx"

open System
open Akka.Actor
open Akka.Configuration
open Akka.FSharp
open Akka.TestKit

let config =
    Configuration.parse
        @"akka {
            actor.provider = ""Akka.Remote.RemoteActorRefProvider, Akka.Remote""
            remote.helios.tcp {
                hostname = localhost
                port = 9001
            }
        }"

let system = ActorSystem.Create("FSharp")

// start msg
let printStart courseInfo projectInfo =
    sprintf "Course %s %s" courseInfo projectInfo

// start progect1 program
let courseInfo = "[COP5612] DOS"
let projectInfo = "Project 1"
let startMsg = printStart courseInfo projectInfo
let lang = "F#" 
printfn "Start program of %s with %s" startMsg lang

let argv = fsi.CommandLineArgs
printfn "input arguments: %s" (argv.[0]) 
if argv.Length < 2 then 
    printfn "not enough input"
