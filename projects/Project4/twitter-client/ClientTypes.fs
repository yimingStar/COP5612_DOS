module ClientTypes


type ClientActions = 
    | RequestCONNECTED of string // connect to server
    | NeedREGISTER
    | RequestREGISTER of string // enter a account 

type MessageType = {
    action: string
    data: obj
}

// using object to simulate getting data from database
type UserObject = {
    acount: string
    userID: int
    subscribedList: System.Array // Array of userID user subscribed
    subsribers: System.Array // Array of userID who subscibed user
}      

