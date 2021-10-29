type SystemParams = {
    NumOfNodes: int
    NumOfRequest: int
    PowM: int
    NumOfIdentifier: int
}   

type MessageType = 
    | SYSTEM
    | DATA

type NodeActions = 
    | INIT of int
    | FixFinger
    | Stabilize
    | AskPredecessor
    | GetPredecessor of int
    | FindSuccesor of int*int*MessageType
    | ConfirmSUCCESSOR of int*int*MessageType
    | Notify of int
    | LOOKUP of int*int
    // | CheckPredecessor
    | StartRequestTask
    | WAITING
    | STOP
    
type FingerCol = {
    Idx: int
    KeyId: int // id+2^i
    Succesor: int
}


