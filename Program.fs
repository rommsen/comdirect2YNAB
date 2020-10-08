open System 

[<EntryPoint>]
let main argv =
  let config = Config.fetch()
  Console.Write("Username: ")
  let username = Console.ReadLine()
  Console.Write("Password: ")
  let password = Console.ReadLine()
  let credentials = Comdirect.Credentials.Create username password

  Comdirect.login credentials config.Comdirect_Api
  
  |> Async.RunSynchronously
  |> function | Ok result -> printfn "Finished %A" result | Error error -> printfn "Error %s" error

  0 // return an integer exit code


