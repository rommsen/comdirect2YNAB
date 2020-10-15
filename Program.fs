open System 
open FsToolkit.ErrorHandling

[<EntryPoint>]
let main _ =
  let config = Config.fetch()
  let ynabApi = new YNAB.SDK.API(config.YNAB_Api.Secret)
  Console.Write("Username: ")
  let username = Console.ReadLine()
  Console.Write("Password: ")
  let password = Console.ReadLine()
  let credentials = Comdirect.Credentials.Create username password

  asyncResult {
    let! requestInfo,tokens = Comdirect.login credentials config.Comdirect_Api

    let! transactions =
      Comdirect.Transactions.getLastXDays 
        config.Transfer.Days
        requestInfo 
        tokens 
        config.Transfer.Comdirect_Account

    return!
      YNAB.addNonexistentTransactions 
        config.Transfer.Days 
        config.Transfer.YNAB_Budget
        (Guid.Parse(config.Transfer.YNAB_Account))
        transactions 
        ynabApi

  }
  |> Async.RunSynchronously
  |> function | Ok result -> printfn "Finished %A" result | Error error -> printfn "Error %s" error

  0


