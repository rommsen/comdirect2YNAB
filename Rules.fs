module Rules

open System
open System.IO
open YamlDotNet.Serialization
open YamlDotNet.Serialization.NamingConventions

// Define the record type for a single rule
type Rule = {
    PayeePattern: string
    CategoryId: string
}

// Define the type for the list of rules
type Rules = {
    Rules: Rule list
}

let mutable internal rulesFileSource = "rules.yml" // Default path

// Function to change the source file for rules, e.g., for testing
let setRulesFileSourceForTesting newPath =
    rulesFileSource <- newPath

// Function to reset the source file to default
let resetRulesFileSourceToDefault () =
    rulesFileSource <- "rules.yml"

// Function to load and validate rules.yml
let loadRules () : Rule list =
    let currentRulesFilePath = rulesFileSource // Use the configurable path
    if not (File.Exists(currentRulesFilePath)) then
        printfn "Info: Rules file '%s' not found. No rules will be applied." currentRulesFilePath
        [] // Return empty list, do not exit
    else
        try
            let deserializer =
                DeserializerBuilder()
                    .WithNamingConvention(CamelCaseNamingConvention.Instance)
                    .Build()

            let yamlContent = File.ReadAllText(currentRulesFilePath)
            let loadedRulesOuter = deserializer.Deserialize<Rules>(yamlContent) // Assuming 'Rules' is the outer type { Rules: Rule list }

            // Basic validation: check if Rules list is present and not null
            if isNull loadedRulesOuter || isNull loadedRulesOuter.Rules then
                printfn "Error: Invalid format in '%s'. The 'rules' list is missing or empty." currentRulesFilePath
                Environment.Exit(1) // Exit if validation fails
                [] // Should not be reached due to Exit
            else
                // Further validation for each rule
                loadedRulesOuter.Rules |> List.iter (fun rule ->
                    if String.IsNullOrWhiteSpace(rule.PayeePattern) then
                        printfn "Error: Invalid rule in '%s'. 'payeePattern' cannot be empty." currentRulesFilePath
                        Environment.Exit(1)
                    if String.IsNullOrWhiteSpace(rule.CategoryId) then
                        printfn "Error: Invalid rule in '%s'. 'categoryId' cannot be empty." currentRulesFilePath
                        Environment.Exit(1)
                )
                printfn "Successfully loaded %d rules from '%s'." (loadedRulesOuter.Rules.Length) currentRulesFilePath
                loadedRulesOuter.Rules
        with
        | ex ->
            printfn "Error loading or parsing '%s': %s" currentRulesFilePath ex.Message
            Environment.Exit(1) // Exit if any other error occurs
            [] // Should not be reached

// Make loaded rules available (example: as a global value or through a function)
// For now, other parts of the application will call loadRules directly when needed.
