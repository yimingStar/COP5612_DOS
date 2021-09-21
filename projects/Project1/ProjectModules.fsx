#load "packages.fsx"

open System
open System.Security.Cryptography
open Akka.FSharp

type ArgvInputs = {
    LeadZeros: int
    NumberOfActors: int
    Prefix: string
}

type MiningInputs = {
    LeadZeros: int
    Prefix: string
    ActorName: string
}

type BitCoin = {
    RandomStr: string
    HashedStr: string
}

// ActionsType
module ActionType =
    let Stop = 0
    let StartLocal = 1
    let RemoteArrives = 2

type ActorActions = {
    Cmdtype: int
    Content: string
}

let hashWithSha256(originalStr: string) =
    let hashedBytes = originalStr |> System.Text.Encoding.UTF8.GetBytes |> (new SHA256Managed()).ComputeHash
    let hashedString = hashedBytes |> Array.map (fun (x : byte) -> System.String.Format("{0:X2}", x)) |> String.concat System.String.Empty
    hashedString.ToLower()

let CoinMining(mailbox: Actor<obj>) msg =
    let rec miningLoop() =
        let sender = mailbox.Sender()
        // printfn "actor %A" mailbox.Self.Path
        // printfn "recieve sender %s, msg %s" (sender.Path.Name.ToString()) (msg.ToString())
        match box msg with
        | :? MiningInputs as param ->
            let prefix = param.Prefix
            let checkString = String.replicate param.LeadZeros "0" // use to find the bitCoin
            let mutable randomString = prefix + Guid.NewGuid().ToString()
            let hashedString = randomString |> hashWithSha256
            if hashedString.StartsWith(checkString) then
                let coin: BitCoin = {
                    RandomStr = randomString
                    HashedStr = hashedString
                }
                sender <! coin
            else
                miningLoop()

        | :? ActorActions as param ->
            if param.Cmdtype = ActionType.Stop then ()
        | _ ->  (failwith "unknown mining inputs")
    miningLoop()