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


type GossipMsg = {
    Content: string
}


type PushSumMsg = {
    PushSumS: int
    PushSumW: int
}


type SenderType = 
    | STARTSENDER of string * Set<int> * GossipMsg * string


type ReceiveType = 
    | INIT of NodeParams
    | GOSSIP of GossipMsg
    | WAITING of string
    | STOPRECV of string



