open System 

// 5f96d19dcff6b8bc131974c82a1c13129e84757d84ca20650436f5e61452ff3f
//

let accessToken = "5f96d19dcff6b8bc131974c82a1c13129e84757d84ca20650436f5e61452ff3f";
let budgetId = "e0b4c3e7-d283-423e-8e30-3f37b6328ebf"
let accountId = "a1954b15-f106-4580-99b5-29ea90164e66"

let ynabApi = new YNAB.SDK.API(accessToken);

let date  = new Nullable<DateTime>(new DateTime(2020,10,01))

let listBudgets =
  async {
    let! response = Async.AwaitTask (ynabApi.Transactions.GetTransactionsByAccountAsync(budgetId, accountId, date))
    response.Data.Transactions
    |> Seq.iter (fun budget -> printfn "Tx: %A" budget)
  }

open Thoth.Json.Net
let transactions = 
  """
 {
    "paging": {
        "index": 40,
        "matches": 451
    },
    "aggregated": {
        "account": null,
        "accountId": "9403EAA32D3F473F894D5C025351CB47",
        "bookingDateLatestTransaction": "2020-09-29",
        "referenceLatestTransaction": "5704660619",
        "latestTransactionIncluded": false,
        "pagingTimestamp": "2020-10-09T11:17:40+02"
    },
    "values": [
        {
            "reference": "IT220273K0906056/38506",
            "bookingStatus": "BOOKED",
            "bookingDate": "2020-10-01",
            "amount": {
                "value": "-544.53",
                "unit": "EUR"
            },
            "remitter": {
                "holderName": "Debeka Kranken-Versicherung-Vereina.G"
            },
            "deptor": null,
            "creditor": null,
            "valutaDate": "2020-10-01",
            "directDebitCreditorId": "DE54DBK00000054093",
            "directDebitMandateId": "0005592428",
            "endToEndReference": "0836017700000026416767200000001",
            "newTransaction": false,
            "remittanceInfo": "018360177.9 Kranken 49,11 Riester 7700246285.1 15,00 Riester 77046286.0 16030,42 F.Rente 100246948.7 160,00 F.R04ente 100246949.5 160,00            05End-to-End-Ref.:                   060836017700000026416767200000001    07CORE / Mandatsref.:                080005592428                         09Gläubiger-ID:                      10DE54DBK00000054093                 ",
            "transactionType": {
                "key": "DIRECT_DEBIT",
                "text": "Direct Debit"
            }
        },
        {
            "reference": "HU22027271717830/423",
            "bookingStatus": "BOOKED",
            "bookingDate": "2020-10-01",
            "amount": {
                "value": "-28.33",
                "unit": "EUR"
            },
            "remitter": {
                "holderName": "ARAG SE"
            },
            "deptor": null,
            "creditor": null,
            "valutaDate": "2020-10-01",
            "directDebitCreditorId": "DE35ZZZ00000034259",
            "directDebitMandateId": "DEM00002247882",
            "endToEndReference": "094018151283",
            "newTransaction": false,
            "remittanceInfo": "0111 0036 1776 7497 ARAG Rechtsschutz02 Beitrag 24.10.20-24.11.20         03End-to-End-Ref.:                   04094018151283                       05CORE / Mandatsref.:                06DEM00002247882                     07Gläubiger-ID:                      08DE35ZZZ00000034259                 ",
            "transactionType": {
                "key": "DIRECT_DEBIT",
                "text": "Direct Debit"
            }
        },
        {
            "reference": "HF22027424947662/1978",
            "bookingStatus": "BOOKED",
            "bookingDate": "2020-10-01",
            "amount": {
                "value": "-99.99",
                "unit": "EUR"
            },
            "remitter": {
                "holderName": "A.T.U AUTO-TEILE-UNGER"
            },
            "deptor": null,
            "creditor": null,
            "valutaDate": "2020-10-01",
            "directDebitCreditorId": "DE16ZZZ00000020245",
            "directDebitMandateId": "6562028107612009291513",
            "endToEndReference": "29091513305548022120000301065620281",
            "newTransaction": false,
            "remittanceInfo": "01290915133055480221200003010 ELV6562020281 29.09 15.13 ME3               03End-to-End-Ref.:                   042909151330554802212000030106562028105CORE / Mandatsref.:                066562028107612009291513             07Gläubiger-ID:                      08DE16ZZZ00000020245                 ",
            "transactionType": {
                "key": "DIRECT_DEBIT",
                "text": "Direct Debit"
            }
        },
        {
            "reference": "IE22027473321122/3377",
            "bookingStatus": "BOOKED",
            "bookingDate": "2020-10-01",
            "amount": {
                "value": "-52.5",
                "unit": "EUR"
            },
            "remitter": {
                "holderName": "Norddeutscher Rundfunk"
            },
            "deptor": null,
            "creditor": null,
            "valutaDate": "2020-10-01",
            "directDebitCreditorId": "DE3000100000001272",
            "directDebitMandateId": "4107895561301",
            "endToEndReference": "410789556 2020091101545604",
            "newTransaction": false,
            "remittanceInfo": "01Rundfunk 10.2020 - 12.2020 Beitrags02nr. 410789556 Aenderungen ganz bequ03em: www.rundfunkbeitrag.de         04End-to-End-Ref.:                   05410789556 2020091101545604         06CORE / Mandatsref.:                074107895561301                      08Gläubiger-ID:                      09DE3000100000001272                 ",
            "transactionType": {
                "key": "DIRECT_DEBIT",
                "text": "Direct Debit"
            }
        },
        {
            "reference": "I322027481248653/8699",
            "bookingStatus": "BOOKED",
            "bookingDate": "2020-10-01",
            "amount": {
                "value": "-10.98",
                "unit": "EUR"
            },
            "remitter": {
                "holderName": "ALTE OLDENBURGER Krankenversicherung AG"
            },
            "deptor": null,
            "creditor": null,
            "valutaDate": "2020-10-01",
            "directDebitCreditorId": "DE29ZZZ00000227009",
            "directDebitMandateId": "01MCA3322303311864",
            "endToEndReference": "EIDLASTD8969A3B98EBA000",
            "newTransaction": false,
            "remittanceInfo": "01Vers.-Nr. 332230 AOK-Privat        02End-to-End-Ref.:                   03EIDLASTD8969A3B98EBA000            04CORE / Mandatsref.:                0501MCA3322303311864                 06Gläubiger-ID:                      07DE29ZZZ00000227009                 ",
            "transactionType": {
                "key": "DIRECT_DEBIT",
                "text": "Direct Debit"
            }
        },
        {
            "reference": "IX22027482134396/10452",
            "bookingStatus": "BOOKED",
            "bookingDate": "2020-10-01",
            "amount": {
                "value": "-37.47",
                "unit": "EUR"
            },
            "remitter": {
                "holderName": "Mecklenburgische Lebensversicherungs-Aktiengesellschaft"
            },
            "deptor": null,
            "creditor": null,
            "valutaDate": "2020-10-01",
            "directDebitCreditorId": "DE30MEL00000178200",
            "directDebitMandateId": "56-5090846-LS002",
            "endToEndReference": "56-5090846-LS-Faelligkeit01.10.2020",
            "newTransaction": false,
            "remittanceInfo": "0156-5090846 Renten-Versicherung Beit02rag vom 01.10.20                   03End-to-End-Ref.:                   0456-5090846-LS-Faelligkeit01.10.202005CORE / Mandatsref.:                0656-5090846-LS002                   07Gläubiger-ID:                      08DE30MEL00000178200                 ",
            "transactionType": {
                "key": "DIRECT_DEBIT",
                "text": "Direct Debit"
            }
        },
        {
            "reference": "IX22027483131838/1627",
            "bookingStatus": "BOOKED",
            "bookingDate": "2020-10-01",
            "amount": {
                "value": "-35",
                "unit": "EUR"
            },
            "remitter": {
                "holderName": "DEUTSCHE GESELLSCHAFT FUR ERZIEHUNGSWISSENSCHAFT E.V."
            },
            "deptor": null,
            "creditor": null,
            "valutaDate": "2020-10-01",
            "directDebitCreditorId": "DE22ZZZ00000362159",
            "directDebitMandateId": "DGfE-3035-01",
            "endToEndReference": "20200929-131-1-23834",
            "newTransaction": false,
            "remittanceInfo": "013035-Sachse, Lena DGfE-Mitgliedsbei02trag 2020                          03End-to-End-Ref.:                   0420200929-131-1-23834               05CORE / Mandatsref.:                06DGfE-3035-01                       07Gläubiger-ID:                      08DE22ZZZ00000362159                 ",
            "transactionType": {
                "key": "DIRECT_DEBIT",
                "text": "Direct Debit"
            }
        },
        {
            "reference": "GW22027490735894/20127",
            "bookingStatus": "BOOKED",
            "bookingDate": "2020-10-01",
            "amount": {
                "value": "-21",
                "unit": "EUR"
            },
            "remitter": {
                "holderName": "SC Scholerberg e.V."
            },
            "deptor": null,
            "creditor": null,
            "valutaDate": "2020-10-01",
            "directDebitCreditorId": "DE74ZZZ00000931001",
            "directDebitMandateId": "1469",
            "endToEndReference": "MitglNr-0000001469",
            "newTransaction": false,
            "remittanceInfo": "01Sachse,Klara Mitgliedsbeitrag      02End-to-End-Ref.:                   03MitglNr-0000001469                 04CORE / Mandatsref.:                051469                               06Gläubiger-ID:                      07DE74ZZZ00000931001                 ",
            "transactionType": {
                "key": "DIRECT_DEBIT",
                "text": "Direct Debit"
            }
        },
        {
            "reference": "GW22027490735894/20128",
            "bookingStatus": "BOOKED",
            "bookingDate": "2020-10-01",
            "amount": {
                "value": "-21",
                "unit": "EUR"
            },
            "remitter": {
                "holderName": "SC Scholerberg e.V."
            },
            "deptor": null,
            "creditor": null,
            "valutaDate": "2020-10-01",
            "directDebitCreditorId": "DE74ZZZ00000931001",
            "directDebitMandateId": "1470",
            "endToEndReference": "MitglNr-0000001470",
            "newTransaction": false,
            "remittanceInfo": "01Sachse,Lotta Mitgliedsbeitrag      02End-to-End-Ref.:                   03MitglNr-0000001470                 04CORE / Mandatsref.:                051470                               06Gläubiger-ID:                      07DE74ZZZ00000931001                 ",
            "transactionType": {
                "key": "DIRECT_DEBIT",
                "text": "Direct Debit"
            }
        },
        {
            "reference": "I022027491838181/36872",
            "bookingStatus": "BOOKED",
            "bookingDate": "2020-10-01",
            "amount": {
                "value": "-22.88",
                "unit": "EUR"
            },
            "remitter": {
                "holderName": "AMAZON PAYMENTS EUROPE S.C.A."
            },
            "deptor": null,
            "creditor": null,
            "valutaDate": "2020-10-01",
            "directDebitCreditorId": "DE94ZZZ00000561653",
            "directDebitMandateId": "H2bIgyM6Yd9a,arJkdFDmHT+(Rx?Ta",
            "endToEndReference": "1VXBKOPC8NG3EPYO",
            "newTransaction": false,
            "remittanceInfo": "01302-8261071-1470719 AMZN Mktp DE 1V02XBKOPC8NG3EPYO                     03End-to-End-Ref.:                   041VXBKOPC8NG3EPYO                   05CORE / Mandatsref.:                06H2bIgyM6Yd9a,arJkdFDmHT+(Rx?Ta     07Gläubiger-ID:                      08DE94ZZZ00000561653                 ",
            "transactionType": {
                "key": "DIRECT_DEBIT",
                "text": "Direct Debit"
            }
        },
        {
            "reference": "1HD20274D1353201/10459",
            "bookingStatus": "BOOKED",
            "bookingDate": "2020-09-30",
            "amount": {
                "value": "-240.51",
                "unit": "EUR"
            },
            "remitter": {
                "holderName": "EDEKA KUTSCHE SAGT DANKE"
            },
            "deptor": null,
            "creditor": null,
            "valutaDate": "2020-09-30",
            "directDebitCreditorId": null,
            "directDebitMandateId": null,
            "endToEndReference": null,
            "newTransaction": false,
            "remittanceInfo": "01EDEKA KUTSCHE SAGT DANKE//OSNABRÜCK022020-09-29T13:22:40 KFN 3  VJ 2212 ",
            "transactionType": {
                "key": "DIRECT_DEBIT",
                "text": "Direct Debit"
            }
        },
        {
            "reference": "J3220274C3456863/2472",
            "bookingStatus": "BOOKED",
            "bookingDate": "2020-09-30",
            "amount": {
                "value": "2954.74",
                "unit": "EUR"
            },
            "remitter": {
                "holderName": "ASD PERSONALINFORMATIONS- SYSTEME GMBH"
            },
            "deptor": null,
            "creditor": null,
            "valutaDate": "2020-09-30",
            "directDebitCreditorId": null,
            "directDebitMandateId": null,
            "endToEndReference": "nicht angegeben",
            "newTransaction": false,
            "remittanceInfo": "01Gehalt September 2020              02End-to-End-Ref.:                   03nicht angegeben                    ",
            "transactionType": {
                "key": "TRANSFER",
                "text": "Transfer"
            }
        },
        {
            "reference": "25K20274C2418756/33431",
            "bookingStatus": "BOOKED",
            "bookingDate": "2020-09-30",
            "amount": {
                "value": "-14",
                "unit": "EUR"
            },
            "remitter": {
                "holderName": "MAX AUTOWASCH GMBH"
            },
            "deptor": null,
            "creditor": null,
            "valutaDate": "2020-09-30",
            "directDebitCreditorId": null,
            "directDebitMandateId": null,
            "endToEndReference": null,
            "newTransaction": false,
            "remittanceInfo": "01MAX AUTOWASCH GMBH//Osnabrueck/DE  022020-09-29T07:59:37 KFN 3  VJ 2212 ",
            "transactionType": {
                "key": "DIRECT_DEBIT",
                "text": "Direct Debit"
            }
        },
        {
            "reference": "6IB20274A3148752/18553",
            "bookingStatus": "BOOKED",
            "bookingDate": "2020-09-30",
            "amount": {
                "value": "-15.78",
                "unit": "EUR"
            },
            "remitter": {
                "holderName": "DEUTSCHE POST AG"
            },
            "deptor": null,
            "creditor": null,
            "valutaDate": "2020-09-30",
            "directDebitCreditorId": null,
            "directDebitMandateId": null,
            "endToEndReference": null,
            "newTransaction": false,
            "remittanceInfo": "01OSNABRUECK 38//OSNABRUECK/DE       022020-09-29T12:48:40 KFN 3  VJ 2212 ",
            "transactionType": {
                "key": "DIRECT_DEBIT",
                "text": "Direct Debit"
            }
        },
        {
            "reference": "HK22027481547018/4441",
            "bookingStatus": "BOOKED",
            "bookingDate": "2020-09-30",
            "amount": {
                "value": "1897.69",
                "unit": "EUR"
            },
            "remitter": {
                "holderName": "Niedersachsische Landeshauptkasse"
            },
            "deptor": null,
            "creditor": null,
            "valutaDate": "2020-09-30",
            "directDebitCreditorId": null,
            "directDebitMandateId": null,
            "endToEndReference": "35PCEH00020830718701412000000047370",
            "newTransaction": false,
            "remittanceInfo": "01BEZUEGE 09.20                      02End-to-End-Ref.:                   0335PCEH00020830718701412000000047370",
            "transactionType": {
                "key": "TRANSFER",
                "text": "Transfer"
            }
        },
        {
            "reference": "HK22027481547015/9658",
            "bookingStatus": "BOOKED",
            "bookingDate": "2020-09-30",
            "amount": {
                "value": "272.19",
                "unit": "EUR"
            },
            "remitter": {
                "holderName": "THOMAS KOETTER"
            },
            "deptor": null,
            "creditor": null,
            "valutaDate": "2020-09-30",
            "directDebitCreditorId": null,
            "directDebitMandateId": null,
            "endToEndReference": "nicht angegeben",
            "newTransaction": false,
            "remittanceInfo": "01NEBENKOSTEN ABR. 2019 WALDSTR. 21A 02End-to-End-Ref.:                   03nicht angegeben                    ",
            "transactionType": {
                "key": "TRANSFER",
                "text": "Transfer"
            }
        },
        {
            "reference": "HK22027481202130/23177",
            "bookingStatus": "BOOKED",
            "bookingDate": "2020-09-30",
            "amount": {
                "value": "997.16",
                "unit": "EUR"
            },
            "remitter": {
                "holderName": "Landeshauptkasse Nordrhein-Westfalen fuer LBV"
            },
            "deptor": null,
            "creditor": null,
            "valutaDate": "2020-09-30",
            "directDebitCreditorId": null,
            "directDebitMandateId": null,
            "endToEndReference": "0335720-0900000070-2020091211341056",
            "newTransaction": false,
            "remittanceInfo": "01Q66724933 3-54598399-Bezuege 09/202020                                  03End-to-End-Ref.:                   040335720-0900000070-2020091211341056",
            "transactionType": {
                "key": "TRANSFER",
                "text": "Transfer"
            }
        },
        {
            "reference": "HR220272M0932846/9075",
            "bookingStatus": "BOOKED",
            "bookingDate": "2020-09-30",
            "amount": {
                "value": "-14.61",
                "unit": "EUR"
            },
            "remitter": {
                "holderName": "AMAZON EU S.A R.L., NIEDERLASSUNG DEUTSCHLAND"
            },
            "deptor": null,
            "creditor": null,
            "valutaDate": "2020-09-30",
            "directDebitCreditorId": "DE24ZZZ00000561652",
            "directDebitMandateId": "+k7crt:zX1oeLlZSBYD)iC+.(WdwCa",
            "endToEndReference": "31QJPMX2YNO0STMU",
            "newTransaction": false,
            "remittanceInfo": "01305-3355866-0795507 Amazon.de 31QJP02MX2YNO0STMU                        03End-to-End-Ref.:                   0431QJPMX2YNO0STMU                   05CORE / Mandatsref.:                06+k7crt:zX1oeLlZSBYD)iC+.(WdwCa     07Gläubiger-ID:                      08DE24ZZZ00000561652                 ",
            "transactionType": {
                "key": "DIRECT_DEBIT",
                "text": "Direct Debit"
            }
        },
        {
            "reference": "GW220273G2143015/9918",
            "bookingStatus": "BOOKED",
            "bookingDate": "2020-09-30",
            "amount": {
                "value": "-13.5",
                "unit": "EUR"
            },
            "remitter": {
                "holderName": "AMAZON PAYMENTS EUROPE S.C.A."
            },
            "deptor": null,
            "creditor": null,
            "valutaDate": "2020-09-30",
            "directDebitCreditorId": "DE94ZZZ00000561653",
            "directDebitMandateId": "H2bIgyM6Yd9a,arJkdFDmHT+(Rx?Ta",
            "endToEndReference": "553V7I54PSX82Q78",
            "newTransaction": false,
            "remittanceInfo": "01302-2443543-6720315 AMZN Mktp DE 55023V7I54PSX82Q78                     03End-to-End-Ref.:                   04553V7I54PSX82Q78                   05CORE / Mandatsref.:                06H2bIgyM6Yd9a,arJkdFDmHT+(Rx?Ta     07Gläubiger-ID:                      08DE94ZZZ00000561653                 ",
            "transactionType": {
                "key": "DIRECT_DEBIT",
                "text": "Direct Debit"
            }
        },
        {
            "reference": "4XL20273C2253053/54941",
            "bookingStatus": "BOOKED",
            "bookingDate": "2020-09-29",
            "amount": {
                "value": "-10",
                "unit": "EUR"
            },
            "remitter": {
                "holderName": "Das Buddha Bowl Osnabrueck"
            },
            "deptor": null,
            "creditor": null,
            "valutaDate": "2020-09-29",
            "directDebitCreditorId": null,
            "directDebitMandateId": null,
            "endToEndReference": null,
            "newTransaction": false,
            "remittanceInfo": "01BUDDHA BOWL HANNOVER//HANNOVER/DE  022020-09-28T13:08:50 KFN 3  VJ 2212 ",
            "transactionType": {
                "key": "DIRECT_DEBIT",
                "text": "Direct Debit"
            }
        }
    ]
}
  """ 

open FsToolkit.ErrorHandling


[<EntryPoint>]
let main argv =

  // listBudgets
  // |> Async.RunSynchronously
  // |> ignore





  
  let config = Config.fetch()
  Console.Write("Username: ")
  let username = Console.ReadLine()
  Console.Write("Password: ")
  let password = Console.ReadLine()
  let credentials = Comdirect.Credentials.Create username password

  asyncResult {
    let! requestInfo,tokens = Comdirect.login credentials config.Comdirect_Api

    let! transactions =
      Comdirect.Transactions.get requestInfo tokens "9403EAA32D3F473F894D5C025351CB47"

    return transactions
  }
  
  |> Async.RunSynchronously
  |> function | Ok result -> printfn "Finished %A" result | Error error -> printfn "Error %s" error

  0 // return an integer exit code


