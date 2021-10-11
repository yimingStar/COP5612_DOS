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
    PushSumS: int
    PushSumW: int
}


type SenderType = 
    | STARTSENDER of int * Set<int> * GossipMsg * string
    | UPDATE of Set<int>
    | RSEND
    | STOPSEND


type ReceiveType = 
    | INIT of NodeParams
    | GOSSIP of GossipMsg
    | WAITING of string
    | STOPRECV of string
    | INFORMFINISH of int


type MainNodeType = 
    | RECORDNODE of NodeInfos
    | STOPSYSTEM


