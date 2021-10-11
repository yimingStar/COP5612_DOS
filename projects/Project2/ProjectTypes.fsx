type ArgvInputs = 
    class
        val NumberOfNodes: int
        val Topology: string
        val Algo: string
        new(numberOfNodes, topology, gossipAlgo) = {
            NumberOfNodes = numberOfNodes
            Topology = topology
            Algo = gossipAlgo
        }
    end


type SenderInfos = 
    class
        val SendCount: int
        val RatioChange: double
        new(sendCount, ratioChange) = {
            SendCount = sendCount
            RatioChange = ratioChange
        }
    end


type NodeParams = {
    NodeIdx: int
    SystemParams: ArgvInputs
    MaxRecieveCount: int
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


type PushSumSenderType = 
    | SETPSSENDER of int * Set<int> * string
    | PSUPDATE of Set<int>
    | GAINVALUE of PushSumMsg
    | PSSEND
    | STOPPSSENDER


type SenderType = 
    | UPDATESET of Set<int>
    | STOPSENDER
    // Random
    | SETRSENDER of int * Set<int> * GossipMsg * string
    | RSEND
    // PushSum
    | SETPSSENDER of int * Set<int> * string
    | PSSEND
    | GAINVALUE of PushSumMsg


type ReceiveType = 
    | INIT of NodeParams
    | GOSSIP of GossipMsg
    | PUSHSUM of PushSumMsg
    | WAITING of string
    | STOPRECV of string
    | INFORMFINISH of int


type MainNodeType = 
    | RECORDNODE of NodeInfos
    | STOPSYSTEM

