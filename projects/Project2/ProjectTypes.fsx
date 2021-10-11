type ArgvInputs = 
    class
        val NumberOfNodes: int
        val Topology: string
        val GossipAlgo: string
        new(numberOfNodes, topology, gossipAlgo) = {
            NumberOfNodes = numberOfNodes
            Topology = topology
            GossipAlgo = gossipAlgo
        }
    end


type NodeParams = {
    NodeIdx: int
    SystemParams: ArgvInputs
    MaxRecieveCount: int
    PushSumS: int
    PushSumW: int
}   


type NodeInfos = {
    NodeIdx: int
    SendCount: int
    RatioChange: double
    RunTime: int64
    StartTime: string
    EndTime: string
}


type GossipMsg = {
    Content: string
}


type PushSumMsg = {
    PushSumS: double
    PushSumW: double
}


type SenderType = 
    | STARTSENDER of int * Set<int> * GossipMsg * string
    | UPDATESET of Set<int>
    | RSEND
    | STOPSEND
    // PushSum
    | SETPSSENDER of int * Set<int> * string
    | PSSEND
    | GAINVALUE of PushSumMsg


type ReceiveType = 
    | INIT of NodeParams
    | WAITING of string
    | STOPRECV of string
    | INFORMFINISH of int
    | GOSSIP of GossipMsg
    | PUSHSUM of PushSumMsg


type MainNodeType = 
    | RECORDNODE of NodeInfos
    | STOPSYSTEM


