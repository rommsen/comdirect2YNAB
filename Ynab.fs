module YNAB

open System

open YNAB.SDK.Model


open System.Text.RegularExpressions
let private referenceOf input =
  if String.IsNullOrWhiteSpace input 
  then None
  else
    let regex = new Regex("Ref: (.+)")
    let result = (regex.Match input).Groups.[1].Value
    if not (String.IsNullOrWhiteSpace result) 
    then Some result
    else None

let private getExistingTransactions days budgetId accountId (ynabApi : YNAB.SDK.API )=
  let date  = new Nullable<DateTime>(DateTime.Today.Subtract(TimeSpan.FromDays(float days)))

  async {
    let! response = Async.AwaitTask (ynabApi.Transactions.GetTransactionsByAccountAsync(budgetId, accountId.ToString(), date))
    
    return response.Data.Transactions |> Seq.toList
  }

let private toSaveTransaction accountId (transaction : Comdirect.Transactions.Transaction) =
  let tx = 
    new SaveTransaction(
      AccountId = accountId,
      Amount = int64 (transaction.Amount * 1000M),
      Memo = sprintf "%s, Ref: %s" transaction.Name transaction.Reference,
      Date = transaction.Booking_Date,
      Approved = true
    )
  tx


let addNonexistentTransactions days budgetId accountId (bankTransactions : Comdirect.Transactions.Transaction list) (ynabApi : YNAB.SDK.API ) =
  async {
    let! ynabTransactions =
      getExistingTransactions days budgetId accountId ynabApi

    let references =
      ynabTransactions
      |> List.map (fun tx -> tx.Memo)
      |> List.choose referenceOf
   
    let newTransactions =
      bankTransactions
      |> List.filter (fun tx -> not (references |> List.contains tx.Reference))
      |> List.map (toSaveTransaction accountId)

    // printfn "was drin? %A" ( not (newTransactions |> List.isEmpty))
    if not (newTransactions |> List.isEmpty) then
      let wrapped =
        new SaveTransactionsWrapper(transactions = (ResizeArray<SaveTransaction> newTransactions))

      let! response = Async.AwaitTask (ynabApi.Transactions.CreateTransactionAsync(budgetId, wrapped))
      printfn "Response %A" response
    else printfn "No new txs %i" (List.length newTransactions)

    return newTransactions
  }
  
  



