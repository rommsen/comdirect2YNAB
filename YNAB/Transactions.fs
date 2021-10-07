module YNAB.Transactions

open System
open System.Text.RegularExpressions
open YNAB.SDK.Model

let private referenceOf input =
  if String.IsNullOrWhiteSpace input 
  then None
  else
    let regex = new Regex("Ref: (.+)")
    let result = (regex.Match input).Groups.[1].Value
    if not (String.IsNullOrWhiteSpace result) 
    then Some result
    else None

let private getExisting days budgetId accountId (ynabApi : YNAB.SDK.API) =
  let date  = new Nullable<DateTime>(DateTime.Today.Subtract(TimeSpan.FromDays(float days)))

  async {
    let! response = ynabApi.Transactions.GetTransactionsByAccountAsync(budgetId, accountId.ToString(), date) |> Async.AwaitTask
    
    return response.Data.Transactions |> Seq.toList
  }

let private truncateString str maxlength =
  if String.IsNullOrEmpty(str) then str
  elif str.Length <= maxlength then str 
  else str.Substring(0, maxlength)

let private toSaveTransaction accountId (transaction : Comdirect.Transactions.Transaction) =
  let memo =
    sprintf "%s, %s, Ref: %s" 
      (truncateString (transaction.Name |> Option.defaultValue "") 40)
      (truncateString (Regex.Replace(transaction.Info, @"\s+", " ").Substring(2)) 120)
      transaction.Reference


  let tx = 
    new SaveTransaction(
      AccountId = accountId,
      Amount = int64 (transaction.Amount * 1000M),
      Memo = memo,
      Date = transaction.Booking_Date,
      Approved = true
    )
  tx


let addNonexistent days budgetId accountId (bankTransactions : Comdirect.Transactions.Transaction list) (ynabApi : YNAB.SDK.API ) =
  async {

    let! ynabTransactions =
      getExisting days budgetId accountId ynabApi

    let references =
      ynabTransactions
      |> List.map (fun tx -> tx.Memo)
      |> List.choose referenceOf
   
    let newTransactions =
      bankTransactions
      |> List.filter (fun tx -> not (references |> List.contains tx.Reference))
      |> List.map (toSaveTransaction accountId)

    if not (newTransactions |> List.isEmpty) then
      let wrapped =
        new SaveTransactionsWrapper(transactions = (ResizeArray<SaveTransaction> newTransactions))

      try 
        let! response = ynabApi.Transactions.CreateTransactionAsync(budgetId, wrapped) |> Async.AwaitTask
        return Ok "done"
      with 
      | e -> return Error e.Message
    else 
      return Ok "No new transactions"
  }