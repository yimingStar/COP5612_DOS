type SystemParams = {
    NumOfNodes: int
    NumOfRequest: int
    NumOfIdentifier: int
}   


type NodeActions = 
    | INIT
    | STORE of string*string
