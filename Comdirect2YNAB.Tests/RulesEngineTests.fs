module Comdirect2YNAB.Tests

open System
open Expecto
open Comdirect2YNAB // Access to RulesEngine and YamlConfig
open Comdirect2YNAB.RulesEngine // To use CompiledRule, CategoryInfo etc.
open Comdirect2YNAB.YamlConfig // To use Rule, RulesConfig

// Helper to create a category map for tests
let private normalizeCategoryNameTest (name: string) = // Renamed to avoid conflict if RulesEngine.normalizeCategoryName is not public
    name.Trim().ToLowerInvariant()
        .Replace("ä", "ae").Replace("ö", "oe").Replace("ü", "ue").Replace("ß", "ss")

let private createCategoryMap (categories: (string * Guid) list) : Map<string, Guid> =
    categories
    |> List.map (fun (name, id) -> (normalizeCategoryNameTest name, id))
    |> Map.ofList

// Sample Category IDs
let groceriesId = Guid.NewGuid()
let travelId = Guid.NewGuid()
let shoppingId = Guid.NewGuid()
let uncategorizedId = Guid.NewGuid()

let tests =
    testList "RulesEngine Tests" [
        testCase "Exact Memo Matching" <| fun _ ->
            let rules = [{ Match = "REWE"; Category = "Groceries" }]
            let rulesConfig = { DefaultCategory = None; Rules = rules }
            let categoryMap = createCategoryMap [("Groceries", groceriesId)]

            let compiledResult = RulesEngine.compileRules rulesConfig categoryMap
            Expect.isOk compiledResult "Rule compilation should succeed"
            let compiledRules, defaultCatId =
                match compiledResult with
                | Ok res -> res
                | Error e -> failwith $"Expected Ok but got Error: {e}"

            let memo = "Shopping at REWE"
            let classifiedCategoryId = RulesEngine.classify compiledRules defaultCatId (Some memo)
            Expect.equal classifiedCategoryId (Some groceriesId) "Should classify 'REWE' memo to Groceries"

        testCase "Regex Memo Matching" <| fun _ ->
            let rules = [{ Match = ".*Amazon.*"; Category = "Shopping" }]
            let rulesConfig = { DefaultCategory = None; Rules = rules }
            let categoryMap = createCategoryMap [("Shopping", shoppingId)]

            let compiledResult = RulesEngine.compileRules rulesConfig categoryMap
            Expect.isOk compiledResult "Rule compilation should succeed"
            let compiledRules, defaultCatId =
                match compiledResult with
                | Ok res -> res
                | Error e -> failwith $"Expected Ok but got Error: {e}"

            let memo = "Order from Amazon Marketplace"
            let classifiedCategoryId = RulesEngine.classify compiledRules defaultCatId (Some memo)
            Expect.equal classifiedCategoryId (Some shoppingId) "Should classify 'Amazon' memo using regex to Shopping"

        testCase "Default Category Fallback - No Rules Match" <| fun _ ->
            let rules = [{ Match = "NON_MATCHING_RULE"; Category = "Shopping" }]
            let rulesConfig = { DefaultCategory = Some "Uncategorized"; Rules = rules }
            let categoryMap = createCategoryMap [("Shopping", shoppingId); ("Uncategorized", uncategorizedId)]

            let compiledResult = RulesEngine.compileRules rulesConfig categoryMap
            Expect.isOk compiledResult "Rule compilation should succeed"
            let compiledRules, defaultCatId =
                match compiledResult with
                | Ok res -> res
                | Error e -> failwith $"Expected Ok but got Error: {e}"

            Expect.isSome defaultCatId "Default category ID should be resolved"
            Expect.equal (Option.get defaultCatId) uncategorizedId "Default category ID should be Uncategorized"

            let memo = "Some other transaction"
            let classifiedCategoryId = RulesEngine.classify compiledRules defaultCatId (Some memo)
            Expect.equal classifiedCategoryId (Some uncategorizedId) "Should fall back to default category"

        testCase "Default Category Fallback - No Memo" <| fun _ ->
            let rulesConfig = { DefaultCategory = Some "Uncategorized"; Rules = [] }
            let categoryMap = createCategoryMap [("Uncategorized", uncategorizedId)]

            let compiledResult = RulesEngine.compileRules rulesConfig categoryMap
            Expect.isOk compiledResult "Rule compilation should succeed"
            let compiledRules, defaultCatId =
                match compiledResult with
                | Ok res -> res
                | Error e -> failwith $"Expected Ok but got Error: {e}"

            let classifiedCategoryId = RulesEngine.classify compiledRules defaultCatId None // No memo
            Expect.equal classifiedCategoryId (Some uncategorizedId) "Should fall back to default category when no memo"

        testCase "No Default Category - No Match" <| fun _ ->
            let rules = [{ Match = "NON_MATCHING_RULE"; Category = "Shopping" }]
            let rulesConfig = { DefaultCategory = None; Rules = rules }
            let categoryMap = createCategoryMap [("Shopping", shoppingId)]

            let compiledResult = RulesEngine.compileRules rulesConfig categoryMap
            Expect.isOk compiledResult "Rule compilation should succeed"
            let compiledRules, defaultCatId =
                match compiledResult with
                | Ok res -> res
                | Error e -> failwith $"Expected Ok but got Error: {e}"

            Expect.isNone defaultCatId "Default category ID should be None"

            let memo = "Some other transaction"
            let classifiedCategoryId = RulesEngine.classify compiledRules defaultCatId (Some memo)
            Expect.isNone classifiedCategoryId "Should result in no category if no match and no default"

        testCase "Category Name Resolution Error - Category Not Found" <| fun _ ->
            let rules = [{ Match = "REWE"; Category = "NonExistentCategory" }]
            let rulesConfig = { DefaultCategory = None; Rules = rules }
            let categoryMap = createCategoryMap [("Groceries", groceriesId)]

            let compiledResult = RulesEngine.compileRules rulesConfig categoryMap
            
            match compiledResult with
            | Ok _ -> failwith "Rule compilation should fail due to missing category"
            | Error errorMsg ->
                Expect.stringContains errorMsg "NonExistentCategory' not found" "Error message should indicate missing category"


        testCase "Category Name Resolution Error - Default Category Not Found" <| fun _ ->
            let rulesConfig = { DefaultCategory = Some "MissingDefault"; Rules = [] }
            let categoryMap = createCategoryMap [("Groceries", groceriesId)]

            let compiledResult = RulesEngine.compileRules rulesConfig categoryMap

            match compiledResult with
            | Ok _ -> failwith "Rule compilation should fail due to missing default category"
            | Error errorMsg ->
                Expect.stringContains errorMsg "MissingDefault' not found" "Error message should indicate missing default category"

        testCase "Invalid Regex in Rule" <| fun _ ->
            let rules = [{ Match = "["; Category = "Groceries" }] // Invalid regex
            let rulesConfig = { DefaultCategory = None; Rules = rules }
            let categoryMap = createCategoryMap [("Groceries", groceriesId)]

            let compiledResult = RulesEngine.compileRules rulesConfig categoryMap

            match compiledResult with
            | Ok _ -> failwith "Rule compilation should fail due to invalid regex"
            | Error errorMsg ->
                Expect.stringContains errorMsg "Invalid regex '['" "Error message should indicate invalid regex"

        testCase "Umlaut and Case Insensitive Category Name Resolution" <| fun _ ->
            let rules = [{ Match = "Test"; Category = "Reisen · Bahn" }] // User input for rule
            let rulesConfig = { DefaultCategory = Some "Lebensmittel"; Rules = rules }
            // YNAB category names
            let categoryMap = createCategoryMap [
                ("Reisen · Bahn", travelId);
                ("lebensmittel", groceriesId)
            ]

            let compiledResult = RulesEngine.compileRules rulesConfig categoryMap
            Expect.isOk compiledResult "Rule compilation with umlaut/case variant should succeed"
            let compiledRules, defaultCatId =
                match compiledResult with
                | Ok res -> res
                | Error e -> failwith $"Expected Ok but got Error: {e}"

            Expect.isSome defaultCatId "Default category ID should be resolved"
            Expect.equal (Option.get defaultCatId) groceriesId "Default category should be Lebensmittel"

            let classifiedCategoryId = RulesEngine.classify compiledRules defaultCatId (Some "Test")
            Expect.equal classifiedCategoryId (Some travelId) "Should classify 'Test' to Reisen · Bahn"

        // Placeholder for YamlConfig specific tests - these would ideally be in their own file
        // and require file I/O mocking or temporary file creation.
        testCase "Placeholder: YamlConfig - Invalid YAML Structure" <| fun _ ->
            Expect.isTrue true "This test would validate YamlConfig.parseRulesFile with bad YAML."

        testCase "Placeholder: YamlConfig - Rule Validation (e.g. missing 'match')" <| fun _ ->
            Expect.isTrue true "This test would validate YamlConfig.parseRulesFile schema checks."
    ]

// If the test project has a Program.fs with an EntryPoint, it would call this.
// For 'dotnet test' with MSTest adapters, this function might not be directly called by the runner,
// but Expecto's own runners might use it.
let run (args: string array) : int =
    Tests.runTestsInAssembly defaultConfig args
