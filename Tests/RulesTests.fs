module Tests.RulesTests

open System
open NUnit.Framework // Using NUnit as an example
open YNAB.SDK.Model
// Ensure main project modules are accessible. Adjust namespaces if necessary.
open Comdirect_to_Ynab
open Rules
open YNAB.Transactions

let private createDummyTransaction payeeOpt =
    {
        Comdirect.Transactions.Reference = Guid.NewGuid().ToString()
        Comdirect.Transactions.Booking_Date = DateTime.Now
        Comdirect.Transactions.Amount = 25.50M
        Comdirect.Transactions.Name = payeeOpt
        Comdirect.Transactions.Info = "Dummy transaction info"
    }

[<TestFixture>]
type RuleApplicationTests () =

    let testRulesSet = [
        { PayeePattern = "ALDI"; CategoryId = "cat_aldi_grocery" };
        { PayeePattern = "(?i)ESSO"; CategoryId = "cat_esso_fuel" };
    ]

    [<Test>]
    member _.``Payee matching rule assigns category``() =
        let transaction = createDummyTransaction (Some "ALDI Nord")
        let ynabAccountId = Guid.NewGuid()
        let resultTransaction = YNAB.Transactions.toSaveTransaction ynabAccountId transaction testRulesSet

        Assert.IsTrue(resultTransaction.CategoryId.HasValue, "Category ID should be assigned for matching rule.")
        Assert.AreEqual(Guid.Parse("cat_aldi_grocery"), resultTransaction.CategoryId.Value, "Assigned category ID is not correct.")

    [<Test>]
    member _.``No matching rule leaves category null``() =
        let transaction = createDummyTransaction (Some "Unknown Vendor")
        let ynabAccountId = Guid.NewGuid()
        let resultTransaction = YNAB.Transactions.toSaveTransaction ynabAccountId transaction testRulesSet

        Assert.IsFalse(resultTransaction.CategoryId.HasValue, "Category ID should be null when no rule matches.")

    [<Test>]
    member _.``Case insensitive regex pattern matches``() =
        let transaction = createDummyTransaction (Some "Esso Station") // Mixed case
        let ynabAccountId = Guid.NewGuid()
        let resultTransaction = YNAB.Transactions.toSaveTransaction ynabAccountId transaction testRulesSet

        Assert.IsTrue(resultTransaction.CategoryId.HasValue, "Category ID should be assigned for case-insensitive regex match.")
        Assert.AreEqual(Guid.Parse("cat_esso_fuel"), resultTransaction.CategoryId.Value, "Assigned category ID for regex match is not correct.")
