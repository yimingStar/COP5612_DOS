module ServerTypes
// SERVER RECEIVE
// CONNECT
// REGISTER
// SUBSCRIBE
// TWEET

// Server to Client Actions
// REQUIRE_USERID
// DATA


type REGISTERDATA = {
    account: string
}

// using object to simulate getting data from database
type UserObject = {
    account: string
    subscribedList: string list // Array of userID user subscribed
    subscribers: string list
    tweets: string list // Array of userID who subscibed user
}   
type CONNECTDATA = {
    userId: string
}

type MessageType = {
    action: string
    data: string
}

type ServerActions = 
    | START
    | SIGNIN of string
    | REGISTER of string // client create -> account -> return userID
    | SUBSCRIBE of int // client subscribe to userID
    | StopSUBSCRIBE of int
    | PostTWEET of string*string*System.Array*System.Array // userID, tweet content, hashtags<string>, metioned<userID>
    | CONNECTED of string // connect by userId 

let userIdPrefix = "tweetUser_"
