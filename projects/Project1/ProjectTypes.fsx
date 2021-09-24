type ArgvInputs = {
    LeadZeros: int
    NumberOfActors: int
    Prefix: string
}

type MiningInputs = {
    LeadZerosStr: string
    Prefix: string
}

type BitCoin = {
    RandomStr: string
    HashedStr: string
}

// ActionsType
module ActionType =
    let Stop = 0
    let StartLocal = 1
    let SendArrive = 2
    let RemoteArrives = 3
    let CoinFound = 4

type ActorActions = {
    Cmdtype: int
    Content: string
}
