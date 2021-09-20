#time "on"
#load "packages.fsx"
#load "ProjectModules.fsx"

open System
open Akka.Actor
open Akka.FSharp
open ProjectModules

let config =
    Configuration.parse
        @"akka {
            actor.provider = ""Akka.Remote.RemoteActorRefProvider, Akka.Remote""
            remote.helios.tcp {
                hostname = localhost
                port = 9001
            }
        }"

let system = ActorSystem.Create("proj1Slave", config)
system.WhenTerminated.Wait()