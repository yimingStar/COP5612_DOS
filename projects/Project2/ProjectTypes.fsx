// type ArgvInputs = {
//     NumberOfNodes: int
//     Topology: string
//     GossipAlgo: string
// }

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

// type NodeParams = {
//     NodeIdx: int
//     SystemParams: ArgvInputs
//     MaxRecieveCount: int
//     PushSumS: int
//     PushSumW: int
// }

module TopologyType =
    let LINE = "Line"
    let FULL = "Full"
    let ThreeD = "3Dgrid"
    let ImPThreeD = "Imperfect"

type NodeParams = 
    class
        val NodeIdx: int
        val SystemParams: ArgvInputs
        val MaxRecieveCount: int
        val PushSumS: int
        val PushSumW: int
        new() = {
            NodeIdx = -1
            SystemParams = ArgvInputs(-1, "", "")
            MaxRecieveCount = 0
            PushSumS = 0
            PushSumW = 0
        }
        new(nodeIdx, argvInputs, count, s, w) = {
            NodeIdx = nodeIdx
            SystemParams = argvInputs
            MaxRecieveCount = count
            PushSumS = s
            PushSumW = w
        }
    end

type GossipMsg = {
    Content: string
}