module ServerTypes

type MessageType = 
    | SYSTEM
    | DATA

type ServerActions = 
    | START
    | SIGNIN of string
    | REGISTER of string // client create -> account -> return userID
    | SUBSCRIBE of int // client subscribe to userID
    | StopSUBSCRIBE of int
    | PostTWEET of string*string*System.Array*System.Array // userID, tweet content, hashtags<string>, metioned<userID>
    | CONNECTED of string // connect by userId 


// using object to simulate getting data from database
type UserObject = {
    acount: string
    userID: int
    subscribedList: System.Array // Array of userID user subscribed
    subsribers: System.Array // Array of userID who subscibed user
}      