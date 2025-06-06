open System 
open FsToolkit.ErrorHandling

open Helper
open Config
open Rules

let getYnabInfo config =
  let ynabApi = new YNAB.SDK.API(config.YNAB_Api.Secret)

  asyncResult {
    let! budgetInfo = YNAB.Infos.budgetsWithAccounts ynabApi
    Console.WriteLine(System.Environment.NewLine)
    Console.WriteLine("Budgets:")
    budgetInfo
    |> Array.iteri (
        fun i budgetInfo ->
          Console.WriteLine(sprintf ("%i. %s (Id: %s)") (i+1) budgetInfo.Name (budgetInfo.Id.ToString()))
          Console.WriteLine("  Accounts:")
          budgetInfo.Accounts
          |> List.iteri(
              fun j accountInfo -> 
                Console.WriteLine(sprintf ("  %i. %s (Id: %s)") (j+1) accountInfo.Name (accountInfo.Id.ToString())) 
        )
    )

    return ()
  }
  |> runAsync
  |> function | Ok _ -> printfn "Copy and paste id to appsettings.json. Press key to continue" | Error error -> printfn "Error %s" error


let transfer config (rules: Rule list) =
  let ynabApi = new YNAB.SDK.API(config.YNAB_Api.Secret)
  Console.Write("Username: ")
  let username = Console.ReadLine()
  Console.Write("Password: ")
  let password = Console.ReadLine()
  let credentials = Comdirect.API.Credentials.Create username password

  asyncResult {
    let! requestInfo,tokens = Comdirect.Login.login credentials config.Comdirect_Api

    let! transactions =
      Comdirect.Transactions.getLastXDays 
        config.Transfer.Days
        requestInfo 
        tokens 
        config.Transfer.Comdirect_Account

    return!
      YNAB.Transactions.addNonexistent 
        config.Transfer.Days 
        config.Transfer.YNAB_Budget
        (Guid.Parse(config.Transfer.YNAB_Account))
        transactions 
        ynabApi
        rules

  }
  |> runAsync
  |> function | Ok result -> printfn "Finished %A" result | Error error -> printfn "Error %s" error

let test config (rules: Rule list) =
  let ynabApi = new YNAB.SDK.API(config.YNAB_Api.Secret)

  asyncResult {
    let transactions : Comdirect.Transactions.Transaction list =
      [
        {
          Reference = System.Guid.NewGuid().ToString()
          Booking_Date = DateTime.Now
          Amount = 20M
          Name = Some "test"
          Info = "Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam erat, sed diam voluptua. At vero eos et accusam et justo duo dolores et ea rebum. Stet clita kasd gubergren, no sea takimata sanctus est Lorem ipsum dolor sit amet. Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam erat, sed diam voluptua. At vero eos et accusam et justo duo dolores et ea rebum. Stet clita kasd gubergren, no sea takimata sanctus est Lorem ipsum dolor sit amet. Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam erat, sed diam voluptua. At vero eos et accusam et justo duo dolores et ea rebum. Stet clita kasd gubergren, no sea takimata sanctus est Lorem ipsum dolor sit amet. Duis autem vel eum iriure dolor in hendrerit in vulputate velit esse molestie consequat, vel illum dolore eu f"
        }
      ]

    return!
      YNAB.Transactions.addNonexistent 
        config.Transfer.Days 
        config.Transfer.YNAB_Budget
        (Guid.Parse(config.Transfer.YNAB_Account))
        transactions 
        ynabApi
        rules

  }
  |> runAsync
  |> function | Ok result -> printfn "Finished %A" result | Error error -> printfn "Error %s" error



open Thoth.Json.Net
[<EntryPoint>]
let main _ =
 
  let config = Config.fetch()
  let rules = Rules.loadRules()

  let menuActions = // Changed 'main' to 'menuActions' here for clarity in replace
    [
      ("YNAB Test", fun cfg -> test cfg rules ; waitForAnyKey())
      ("Transfer Comdirect Transactions to YNAB", fun cfg -> transfer cfg rules ; waitForAnyKey())
      ("YNAB Infos", fun cfg -> getYnabInfo cfg ; waitForAnyKey())
    ], ignore

  menuActions // Use 'menuActions'
  |> UI.Menu.initialize config "Comdirect to Ynab"
  |> ignore

  // let decoded =
  //   Decode.fromString
  //     Comdirect.Transactions.txsDecoder
  //     transactions

  // printfn "%A" decoded

  0


