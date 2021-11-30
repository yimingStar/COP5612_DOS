module ServerTypes

// [Client Receive]

// return data
// USER_DATA
// OWN_TWEET_DATA
// BROWSE_TWEET_DATA
// ---
// invalid msg
// REQUIRE_USERID
// REQUIRE_ACCOUNT

// [Client Request]
// CONNECT
// REGISTER
// SUBSCRIBE
// TWEET

// using object to simulate getting data from database
type UserObject = {
    userId: string
    account: string
    mutable subscribedList: string list // List of userID user subscribed
    mutable subscribers: string list // List of userID who subscibed user
    mutable tweets: string list 
}  

type SUBSCRIBEDATA = {
    targeUserId: string
    userId: string
}

type REGISTERDATA = {
    account: string
}

type CONNECTDATA = {
    userId: string
}

type MessageType = {
    action: string
    data: string
}

let userIdPrefix = "tweetUser_"
