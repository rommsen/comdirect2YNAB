namespace Comdirect2YNAB

open System
open System.Text.RegularExpressions
open YNAB.SDK.Api
open YNAB.SDK.Model
open FsToolkit.ErrorHandling

module RulesEngine =

    type CategoryInfo = { Id: Guid; Name: string }

    // Helper to normalize category names for case-insensitive comparison
    // and handling of common variations (trims whitespace, folds umlauts).
    let private normalizeCategoryName (name: string) =
        name.Trim().ToLowerInvariant()
            // Basic umlaut folding, can be expanded if needed
            .Replace("ä", "ae").Replace("ö", "oe").Replace("ü", "ue").Replace("ß", "ss")

    let fetchCategories (ynabApi: YNAB.SDK.API) (budgetId: string) : Async<Result<Map<string, Guid>, string>> =
        asyncResult {
            let! categoriesResponse =
                try
                    ynabApi.Categories.GetCategoriesAsync(budgetId) |> Async.AwaitTask
                with ex ->
                    return! Error $"YNAB API error fetching categories: {ex.Message}"

            let categories =
                categoriesResponse.Data.CategoryGroups
                |> List.collect (fun group -> group.Categories)
                |> List.map (fun category -> { Id = category.Id; Name = category.Name })

            // Build a case-insensitive map of category names to IDs
            // Handle potential duplicates by logging or choosing one, here we take the first encountered.
            let categoryMap =
                categories
                |> List.fold (fun acc category ->
                    let normalizedName = normalizeCategoryName category.Name
                    if acc |> Map.containsKey normalizedName then
                        // Potentially log a warning here if duplicate names are a concern
                        acc
                    else
                        acc |> Map.add normalizedName category.Id
                ) Map.empty

            return categoryMap
        }

    type CompiledRule = {
        Regex: Regex
        CategoryId: Guid
    }

    let compileRules (rulesConfig: YamlConfig.RulesConfig) (categoryMap: Map<string, Guid>) : Result<CompiledRule list * Guid option, string> =
        let mutable errors = []
        let compiledRules =
            rulesConfig.Rules
            |> List.choose (fun rule ->
                let normalizedCategoryName = normalizeCategoryName rule.Category
                match categoryMap |> Map.tryFind normalizedCategoryName with
                | Some categoryId ->
                    try
                        // Case-insensitive regex matching
                        let regex = Regex(rule.Match, RegexOptions.IgnoreCase)
                        Some { Regex = regex; CategoryId = categoryId }
                    with ex ->
                        errors <- $"Invalid regex '{rule.Match}' for category '{rule.Category}': {ex.Message}" :: errors
                        None
                | None ->
                    errors <- $"Category '{rule.Category}' not found or ambiguous in YNAB." :: errors
                    None
            )

        let defaultCategoryId =
            rulesConfig.DefaultCategory
            |> Option.bind (fun defaultCategoryName ->
                let normalizedDefaultCategoryName = normalizeCategoryName defaultCategoryName
                match categoryMap |> Map.tryFind normalizedDefaultCategoryName with
                | Some id -> Some id
                | None ->
                    errors <- $"Default category '{defaultCategoryName}' not found or ambiguous in YNAB." :: errors
                    None
            )

        if List.isEmpty errors then
            Ok (compiledRules, defaultCategoryId)
        else
            Error (String.concat "" (List.rev errors))


    let classify (compiledRules: CompiledRule list) (defaultCategoryId: Guid option) (transactionMemo: string option) : Guid option =
        match transactionMemo with
        | None -> defaultCategoryId // Or None if no default category is set
        | Some memo ->
            match compiledRules |> List.tryFind (fun rule -> rule.Regex.IsMatch(memo)) with
            | Some matchingRule -> Some matchingRule.CategoryId
            | None -> defaultCategoryId
