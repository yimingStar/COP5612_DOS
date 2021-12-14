module ClientTypes

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

type ClientActions = 
    | RequestCONNECTED of string // connect to server
    | NeedREGISTER
    | RequestREGISTER of string // enter a account 

type CONNECTDATA = {
    userId: string
}

type REGISTERDATA = {
    account: string
}

type TWEET_RAW_DATA = {
    userId: string
    content: string
}

type SUBSCRIBEDATA = {
    targeUserId: string
    userId: string
}

type MessageType = {
    action: string
    data: string
}

// using object to simulate getting data from database
type UserObject = {
    userId: string
    account: string
    mutable subscribedList: string list // List of userID user subscribed
    mutable subscribers: string list // List of userID who subscibed user
    mutable tweets: string list 
}  

type TweetObject = {
    userId: string
    tweetId: string
    content: string
    hashTag: string list
    mention: string list
}