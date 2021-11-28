module ClientTypes

// Client RECEIVE
// REQUIRE_USERID
// DATA

// Client to Server Actions
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

type MessageType = {
    action: string
    data: string
}

// using object to simulate getting data from database
type UserObject = {
    acount: string
    userID: int
    subscribedList: System.Array // Array of userID user subscribed
    subsribers: System.Array // Array of userID who subscibed user
}      
