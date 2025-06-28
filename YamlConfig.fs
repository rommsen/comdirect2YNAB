namespace Comdirect2YNAB

open System
open System.IO
open YamlDotNet.Serialization
open YamlDotNet.Serialization.NamingConventions

module YamlConfig =

    [<CLIMutable>]
    type Rule = {
        Match: string
        Category: string
    }

    [<CLIMutable>]
    type RulesConfig = {
        DefaultCategory: string option
        Rules: System.Collections.Generic.List<Rule>
    }
    with
        static member create(rules: List<Rule>) : RulesConfig =
            { DefaultCategory = None; Rules = System.Collections.Generic.List(rules) }

        static member createWithDefaultCategory(defaultCategory: string, rules: List<Rule>) : RulesConfig =
            { DefaultCategory = Some defaultCategory; Rules = System.Collections.Generic.List(rules) }

    let private deserializeRules (yamlContent: string) : RulesConfig option =
        // Enable F# record, option and array support by registering F# type extensions
        let deserializer =
            DeserializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .Build()

        try
            Some (deserializer.Deserialize<RulesConfig>(yamlContent))
        with
        | :? YamlDotNet.Core.YamlException as ex ->
            Console.Error.WriteLine($"Error deserializing YAML: {ex.Message}")
            None
        | ex ->
            Console.Error.WriteLine($"Unexpected error deserializing YAML: {ex.Message}")
            None

    let private validateRulesConfig (config: RulesConfig) : Result<RulesConfig, string> =
        if config.Rules |> Seq.exists (fun rule -> String.IsNullOrWhiteSpace rule.Match) then
            Error "Invalid 'match' field in one or more rules: cannot be empty."
        else if config.Rules |> Seq.exists (fun rule -> String.IsNullOrWhiteSpace rule.Category) then
            Error "Invalid 'category' field in one or more rules: cannot be empty."
        else
            Ok config

    let parseRulesFile (filePath: string) : Result<RulesConfig, string> =
        try
            if not (File.Exists filePath) then
                Error $"Rules file not found at '{filePath}'"
            else
                let yamlContent = File.ReadAllText filePath
                printfn "%s" yamlContent
                match deserializeRules yamlContent with
                | None -> Error "Failed to deserialize rules.yml. The file might be empty or malformed."
                | Some rulesConfig -> validateRulesConfig rulesConfig
        with
        | exn -> Error $"Error parsing rules file: {exn.Message}"

    let exitWithError (errorMessage: string) =
        Console.Error.WriteLine($"Error: {errorMessage}")
        Environment.Exit(1)
        // Return a dummy value to satisfy the compiler, actual exit happens above
        { DefaultCategory = None; Rules = System.Collections.Generic.List<Rule>() }
