#time "on"
#load "packages.fsx"

open System
open Akka.Actor
open Akka.Configuration
open Akka.FSharp

let config =
    Configuration.parse
        @"akka {
            log-config-on-start : on
            stdout-loglevel : DEBUG
            loglevel : ERROR
            actor {
                provider = ""Akka.Remote.RemoteActorRefProvider, Akka.Remote""
                debug : {
                    receive : on
                    autoreceive : on
                    lifecycle : on
                    event-stream : on
                    unhandled : on
                }
                remote.helios.tcp {
                    hostname = localhost
                    port = 0
                }
            }
        }"

let system = ActorSystem.Create("proj1Clients", config)