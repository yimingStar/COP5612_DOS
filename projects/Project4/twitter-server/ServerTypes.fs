module ServerTypes

type MessageType = 
    | SYSTEM
    | DATA

type ServerActions = 
    | START
    | REGISTER of string // client create -> account -> return userID
    | SUBSCRIBE of int // client subscribe to userID
    | StopSUBSCRIBE of int
    | PostTWEET of int*string*System.Array*System.Array // userID, tweet content, hashtags<string>, metioned<userID>
    | CONNECTED of int
