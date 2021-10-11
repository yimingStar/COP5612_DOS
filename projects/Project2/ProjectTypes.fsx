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
    ReceiveCount: int
    RunTime: float
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
    | SEND
    | STOPSEND

type ReceiveType = 
    | INIT of NodeParams
    | GOSSIP of GossipMsg
    | WAITING of string
    | STOPRECV of string
    | INFORMFINISH of int

type MainNodeType = 
    | RECORDSEND of int * int


