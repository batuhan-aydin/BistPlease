module ValueVest.Source.Bist.Program

open Falco
open Falco.Routing
open Falco.HostBuilder

let deneme = 
    use httpClient = new System.Net.Http.HttpClient()
    10

[<EntryPoint>]
let main args =
    webHost args {
        endpoints [
            get "/company/{symbol}" (Response.ofPlainText "Hello world")
        ]
    }
    0