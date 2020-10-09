module Comdirect

open System
open Thoth.Json.Net
open FsHttp
open FsHttp.DslCE
open FsToolkit.ErrorHandling
open Config

type RequestInfo =
  {
    Request_Id : string 
    Session_Id : string
  }

  member __.Encode () =
    let request_info = {|clientRequestId = {|sessionId = __.Session_Id; requestId = __.Request_Id |} |}
    Encode.Auto.toString(0, request_info)


type Credentials =
  {
    Username : string
    Password : string
  }

  static member Create username password =
    {
    Username = username
    Password = password
  }

type Tokens =
    { Access : string
      Refresh : string }

    static member Decoder : Decoder<Tokens> =
      Decode.map2
        (fun access refresh -> { Access = access ;  Refresh = refresh } )
        (Decode.field "access_token" Decode.string)
        (Decode.field "refresh_token" Decode.string)


type Challenge =
  {
    Id : string 
    Typ : string
  }

  static member Decoder : Decoder<Challenge> =
      Decode.map2
        (fun challenge_id challenge_typ -> { Id = challenge_id ;  Typ = challenge_typ } )
        (Decode.field "id" Decode.string)
        (Decode.field "typ" Decode.string)



[<Literal>]
let endpoint = "https://api.comdirect.de/"

let private errorWithCodeAndMessage response =
  async {
    let! error =  response |> Response.toTextAsync
    return Error (sprintf "Code: %i; Message: %s" (int response.statusCode) error) 
  }

let private initOauth credentials apiKeys =
  // request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
  // request.AddHeader("Accept", "application/json");
  // request.AddParameter("client_id", "*");
  // request.AddParameter("client_secret", "*");
  // request.AddParameter("grant_type", "password");
  // request.AddParameter("username", "*");
  // request.AddParameter("password", "*");

  let credentials =
    sprintf "client_id=%s&client_secret=%s&username=%s&password=%s&grant_type=password"
      apiKeys.Client_Id
      apiKeys.Client_Secret
      credentials.Username
      credentials.Password

  async {
    let! response = httpAsync {
      POST (sprintf "%soauth/token" endpoint)
      Accept "application/json"
      body
      ContentType "application/x-www-form-urlencoded"
      text credentials
    } 

    match response |> Response.toResult with 
    | Ok response ->
        let! json_string =  response |> Response.toTextAsync
        let tokens = Decode.fromString Tokens.Decoder json_string
        return tokens

    | Error response -> 
        return! errorWithCodeAndMessage response
  }

let private getTokens (requestInfo: RequestInfo) tokens =
  // request.AddHeader("Accept", "application/json");
  // request.AddHeader("Authorization", "Bearer 700a7cfe-3009-4723-ac46-9264e2f4047a");
  // request.AddHeader("x-http-request-info", "{\"clientRequestId\":{\"sessionId\":\"f4ab0faf-0e6a-be73-f008-46152ac4d1a9\",\"requestId\":\"888617186\"}}");
  // request.AddHeader("Content-Type", "application/json");

  async {
    let! response = httpAsync {
      GET (sprintf "%sapi/session/clients/user/v1/sessions" endpoint)
      Accept "application/json"
      Authorization (sprintf "Bearer %s" tokens.Access)
      Header "x-http-request-info" (requestInfo.Encode())
      body
      ContentType "application/json"
    }

    (* 
      [{
        "identifier": "67BC081F90DD4AD1B39AB099717AF6E0",
        "sessionTanActive": false,
        "activated2FA": false
      }]
    *)
    let identifier_decoder =
      Decode.index 0 (Decode.field "identifier" Decode.string)

    match response |> Response.toResult with 
    | Ok response ->
        let! json_string =  response |> Response.toTextAsync
        let tokens = Decode.fromString identifier_decoder json_string
        return tokens

    | Error response -> 
        return! errorWithCodeAndMessage response
  }

let private validationChallenge (requestInfo: RequestInfo) tokens session_identifier =
  // request.AddHeader("Accept", "application/json");
  // request.AddHeader("Authorization", "Bearer 700a7cfe-3009-4723-ac46-9264e2f4047a");
  // request.AddHeader("x-http-request-info", "{\"clientRequestId\":{\"sessionId\":\"f4ab0faf-0e6a-be73-f008-46152ac4d1a9\",\"requestId\":\"888617186\"}}");
  // request.AddHeader("Content-Type", "application/json");
  // request.AddParameter("application/json", "{\r\n        \"identifier\" : \"672713410880480CB04CC7F25BAD5824\",\r\n        \"sessionTanActive\": true,\r\n        \"activated2FA\": true\r\n}",  ParameterType.RequestBody);

  let session = {| identifier = session_identifier ; sessionTanActive = true;  activated2FA = true |}
  
  async {
    let! response = httpAsync {
      POST (sprintf "%sapi/session/clients/user/v1/sessions/%s/validate" endpoint session_identifier)
      Accept "application/json"
      Authorization (sprintf "Bearer %s" tokens.Access)
      Header "x-http-request-info" (requestInfo.Encode())
      body
      ContentType "application/json"
      json (Encode.Auto.toString(0, session))
    }

    match response |> Response.toResult with 
    | Ok response ->
        let header = "x-once-authentication-info"
        
        let tryGetChallengeHeaders response =
          if response.headers.Contains(header) 
          then response.headers.GetValues(header) |> Seq.tryHead
          else None

        let decodeChallenge = function
          | Some json_string -> Decode.fromString Challenge.Decoder json_string 
          | None -> Error (sprintf "Could not extract challenge info from header: %s" header)

        let validateTanMethod challenge =
          if challenge.Typ = "P_TAN_PUSH" 
          then Ok challenge
          else Error "This client can only validate push tans (P_TAN_PUSH)"         
          
        return 
          response
          |> tryGetChallengeHeaders
          |> decodeChallenge 
          |> Result.bind validateTanMethod
       

    | Error response -> 
        return! errorWithCodeAndMessage response
  }

let private activateSessionTan (requestInfo: RequestInfo) tokens sessionIdentifier challenge =
  // request.AddHeader("Accept", "application/json");
  // request.AddHeader("Authorization", "Bearer 700a7cfe-3009-4723-ac46-9264e2f4047a");
  // request.AddHeader("x-http-request-info", "{\"clientRequestId\":{\"sessionId\":\"f4ab0faf-0e6a-be73-f008-46152ac4d1a9\",\"requestId\":\"888617186\"}}");
  // request.AddHeader("Content-Type", "application/json");
  // request.AddHeader("x-once-authentication-info", "{\"id\":\"44991682\"}");
  // request.AddHeader("x-once-authentication", "000000");
  // request.AddParameter("application/json", "    {\r\n        \"identifier\" : \"672713410880480CB04CC7F25BAD5824\",\r\n        \"sessionTanActive\" : true,\r\n        \"activated2FA\": true\r\n    }\r\n",  ParameterType.RequestBody);
  
  

  let session = {| identifier = sessionIdentifier ; sessionTanActive = true;  activated2FA = true |}

  let authentication_info = {| id = challenge.Id  |}
  
  async {
    let! response = httpAsync {
      PATCH (sprintf "%sapi/session/clients/user/v1/sessions/%s" endpoint sessionIdentifier)
      Accept "application/json"
      Authorization (sprintf "Bearer %s" tokens.Access)
      Header "x-http-request-info" (requestInfo.Encode())
      Header "x-once-authentication-info" (Encode.Auto.toString(0, authentication_info))
      Header "x-once-authentication" "000000"
      body
      ContentType "application/json"
      json (Encode.Auto.toString(0, session))
    }

    match response |> Response.toResult with 
    | Ok response ->
        return Ok response

    | Error response -> 
        return! errorWithCodeAndMessage response
  }

let private grantExtendedAccountPermission tokens apiKeys =
  // request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
  // request.AddHeader("Accept", "application/json");
  // request.AddParameter("client_id", "*");
  // request.AddParameter("client_secret", "*");
  // request.AddParameter("grant_type", "cd_secondary");
  // request.AddParameter("token", "700a7cfe-3009-4723-ac46-9264e2f4047a");

  let credentials =
    sprintf "client_id=%s&client_secret=%s&token=%s&grant_type=cd_secondary"
      apiKeys.Client_Id
      apiKeys.Client_Secret
      tokens.Access

  async {
    let! response = httpAsync {
      POST (sprintf "%soauth/token" endpoint)
      Accept "application/json"
      body
      ContentType "application/x-www-form-urlencoded"
      text credentials
    } 

    match response |> Response.toResult with 
    | Ok response ->
        let! json_string =  response |> Response.toTextAsync
        let tokens = Decode.fromString Tokens.Decoder json_string
        return tokens

    | Error response -> 
        return! errorWithCodeAndMessage response
  }


let login credentials apiKeys =  
  let requestInfo =
    {
      Request_Id = (new DateTimeOffset(DateTime.Now)).ToUnixTimeSeconds().ToString().Substring(0,9)
      Session_Id = Guid.NewGuid().ToString()
    }

  asyncResult {
    printfn "Start login"
    let! tokens = initOauth credentials apiKeys
    // printfn "Tokens: %A" tokens
    let! session_identifier = getTokens requestInfo tokens
    // printfn "session_identifier: %s" session_identifier
    let! challenge = validationChallenge requestInfo tokens session_identifier
    // printfn "Validation challenge: %A" challenge
    printfn "Press key when push tan accepted"
    System.Console.ReadKey() |> ignore
    let! _ = activateSessionTan requestInfo tokens session_identifier challenge
    // printfn "grantExtendedAccountPermission"
    let! tokens = grantExtendedAccountPermission tokens apiKeys
    
    return (requestInfo, tokens)
  }



module Transactions =
  type Transaction =
    {
      Reference : string 
      Booking_Date : DateTime
      Amount : decimal
      Remitter : string
    }

  let txDecoder : Decoder<Transaction> =
    Decode.map4
      (fun reference bookingDate remitter amount  -> { Reference = reference ;  Booking_Date = bookingDate ; Remitter= remitter ; Amount = amount } )
      (Decode.field "reference" Decode.string)
      (Decode.field "bookingDate" Decode.datetime)
      (Decode.at ["remitter" ; "holderName"] Decode.string)
      (Decode.at ["amount"; "value"] Decode.decimal)

  let txsDecoder : Decoder<Transaction list> =
    Decode.field "values" (Decode.list txDecoder)


  let get (requestInfo: RequestInfo) tokens accountId =
  // request.AddHeader("Accept", "application/json");
  // request.AddHeader("Authorization", "Bearer 700a7cfe-3009-4723-ac46-9264e2f4047a");
  // request.AddHeader("x-http-request-info", "{\"clientRequestId\":{\"sessionId\":\"f4ab0faf-0e6a-be73-f008-46152ac4d1a9\",\"requestId\":\"888617186\"}}");
  // request.AddHeader("Content-Type", "application/json");

    async {
      let! response = httpAsync {
        GET (sprintf "%sapi/banking/v1/accounts/%s/transactions?transactionState=BOOKED" endpoint accountId)
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