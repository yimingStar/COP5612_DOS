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
    RunTime: int64
    StartTime: string
}


type GossipMsg = {
    Content: string
}


type PushSumMsg = {
    PushSumS: double
    PushSumW: double
}


type SenderType = 
    | STARTSENDER of int * Set<int> * GossipMsg * string * string
    | UPDATESET of Set<int>
    | RSEND
    | STOPSEND
    // PushSum
    | SETPSSENDER of int * Set<int> * string * string * PushSumMsg
    | UDATESW of PushSumMsg
    | PSSEND


type ReceiveType = 
    | INIT of NodeParams
    | WAITING of string
    | STOPRECV of string
    | INFORMFINISH of int
    | GOSSIP of GossipMsg
    | PUSHSUM of PushSumMsg


type MainNodeType = 
    | RECORDNODE of NodeInfos


