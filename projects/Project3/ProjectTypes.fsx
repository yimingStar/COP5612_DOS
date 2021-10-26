type SystemParams = {
    NumOfNodes: int
    NumOfRequest: int
    PowM: int
    NumOfIdentifier: int
}   


type NodeActions = 
    | INIT
    | STORE of string*string
    | FixFinger
    | FindSuccesor of int*int
    | IsSUCCESSOR
    | ConfirmSUCCESSOR of int*int


type FingerCol = {
    Idx: int
    KeyId: int // id+2^i
    Succesor: int
}
