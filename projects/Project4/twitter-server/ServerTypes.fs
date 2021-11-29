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

let userIdPrefix = "tweetUser_"
