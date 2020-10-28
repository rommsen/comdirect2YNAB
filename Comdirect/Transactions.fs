module Comdirect.Transactions

open System
open Thoth.Json.Net
open FsHttp
open FsHttp.DslCE
open FsToolkit.ErrorHandling
open Comdirect.API

type Transaction =
  {
    Reference : string 
    Booking_Date : DateTime
    Amount : decimal
    Name : string
  }

let txDecoder : Decoder<Transaction> =
  // Decode.map4
  //   (fun reference bookingDate remitter amount  -> { Reference = reference ;  Booking_Date = bookingDate ; Remitter= remitter ; Amount = amount } )
  //   (Decode.field "reference" Decode.string)
  //   (Decode.field "bookingDate" Decode.datetime)
  //   (Decode.at ["remitter" ; "holderName"] Decode.string)
  //   (Decode.at ["amount"; "value"] Decode.decimal)

  // Decode.object
  //   (fun get  -> 
  //     // let name =
  //     //   match get.Optional.At ["remitter" ; "holderName"] Decode.string with 
  //     //   | Some x -> printfn "yo" ; x 
  //     //   | None -> printfn "no" ; get.Required.At ["creditor" ; "holderName"] Decode.string

  //     { 
  //       Reference = get.Required.Field "reference" Decode.string 
  //       Booking_Date = get.Required.Field "bookingDate" Decode.datetime 
  //       Remitter = get.Optional.At ["remittemr" ; "holderName"] Decode.string 
  //       Creditor = get.Optional.At ["creditor" ; "holderName"] Decode.string 
  //       Amount = get.Required.At ["amount"; "value"] Decode.decimal 
  //     } 
    // )

  let remitterDecoder =
    Decode.optional "remitter" (Decode.optional "holderName" Decode.string) 
    |> Decode.map (fun res ->
      match res with
      | Some (Some v) -> Some v
      | Some None -> None
      | None -> None
    )

  let creditorDecoder =
    Decode.optional "creditor" (Decode.optional "holderName" Decode.string) 
    |> Decode.map (fun res ->
      match res with
      | Some (Some v) -> Some v
      | Some None -> None
      | None -> None
    )

  Decode.object
    (fun get  -> 
      let name =
        match get.Optional.Raw remitterDecoder with 
        | Some (Some x) -> x 
        | _ ->
            match get.Required.Raw creditorDecoder with
            | Some x -> x 
            | _ -> "no name available"

      
      // printfn "date %A" (get.Required.Field "bookingDate" Decode.datetime)
      { 
        Reference = get.Required.Field "reference" Decode.string 
        Booking_Date = (get.Required.Field "bookingDate" Decode.datetime).ToLocalTime()
        Name = name
        Amount = get.Required.At ["amount"; "value"] Decode.decimal 
      } 
    )

let txsDecoder : Decoder<Transaction list> =
  Decode.field "values" (Decode.list txDecoder)

let get (requestInfo: RequestInfo) tokens accountId startPagingAt =
// request.AddHeader("Accept", "application/json");
// request.AddHeader("Authorization", "Bearer 700a7cfe-3009-4723-ac46-9264e2f4047a");
// request.AddHeader("x-http-request-info", "{\"clientRequestId\":{\"sessionId\":\"f4ab0faf-0e6a-be73-f008-46152ac4d1a9\",\"requestId\":\"888617186\"}}");
// request.AddHeader("Content-Type", "application/json");

  async {
    printfn "Get transactions, starting at %i" startPagingAt
    let! response = httpAsync {
      GET (sprintf "%sapi/banking/v1/accounts/%s/transactions?transactionState=BOOKED&paging-first=%i" endpoint accountId startPagingAt)
      Accept "application/json"
      Authorization (sprintf "Bearer %s" tokens.Access)
      Header "x-http-request-info" (requestInfo.Encode())
      body
      ContentType "application/json"
    }

    match response |> Response.toResult with 
    | Ok response ->
        let! json_string =  response |> Response.toTextAsync
        let tokens = Decode.fromString txsDecoder json_string
        return tokens

    | Error response -> 
        return! errorWithCodeAndMessage response
  }

let getLastXDays (days : int) (requestInfo: RequestInfo) tokens accountId =
  let date = DateTime.Today.Subtract(TimeSpan.FromDays(float days))
  printfn "date %A" date
  let rec startWithPagingAt startPagingAt =
    asyncResult {
      let! transactions = 
        get requestInfo tokens accountId startPagingAt

      let txInUse = 
        transactions 
        |> List.takeWhile (fun tx -> tx.Booking_Date >= date)

      let len = List.length transactions
      if len = List.length txInUse then
        return! asyncResult {
          let! newTx = startWithPagingAt (len + startPagingAt)
          return txInUse @ newTx
        }
      else return txInUse  
    }
  startWithPagingAt 0