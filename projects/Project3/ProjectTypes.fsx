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
    | FixFinger // start FixFinger timer
    | Stabilize // start Stabilize timer
    | AskPredecessor of int
    | GetPredecessor of int
    | FindSuccesor of int*int*MessageType
    | ConfirmSUCCESSOR of int*int*MessageType
    | Notify of int
    | LOOKUP of int*int
    | CheckPredecessor // start CheckPredecessor timer
    | StartRequestTask // start Request timer
    | WAITING of int
    | PING of int // ping for alive
    | STOP
    
type FingerCol = {
    Idx: int
    KeyId: int // id+2^i
    Succesor: int
}


