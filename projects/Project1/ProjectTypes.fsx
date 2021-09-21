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
