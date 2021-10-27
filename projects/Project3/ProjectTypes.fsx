type SystemParams = {
    NumOfNodes: int
    NumOfRequest: int
    PowM: int
    NumOfIdentifier: int
}   


type NodeActions = 
    | INIT of int
    | FixFinger
    | Stabilize
    | AskPredecessor
    | GetPredecessor of int
    | FindSuccesor of int*int
    | ConfirmSUCCESSOR of int*int
    | Notify of int
    | WAITING
    


type FingerCol = {
    Idx: int
    KeyId: int // id+2^i
    Succesor: int
}
