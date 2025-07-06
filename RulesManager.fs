module Comdirect2YNAB.RulesManager

open System
open System.IO
open Config
open YamlConfig

let private getDefaultRulesPath () =
    let isWindows = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows)
    let configHome =
        if isWindows then
            Environment.GetFolderPath Environment.SpecialFolder.ApplicationData
        else // Linux, macOS
            let xdgConfigHome = Environment.GetEnvironmentVariable "XDG_CONFIG_HOME"
            if not (String.IsNullOrWhiteSpace xdgConfigHome) then xdgConfigHome
            else Path.Combine(Environment.GetFolderPath Environment.SpecialFolder.UserProfile, ".config")

    let appConfigFolder = Path.Combine(configHome, "comdirect2ynab")
    Directory.CreateDirectory(appConfigFolder) |> ignore
    Path.Combine(appConfigFolder, "rules.yml")

let getRulesPath (args: string array) =
    match args |> Array.tryFindIndex (fun arg -> arg.ToLowerInvariant() = "--rules") with
    | Some index when args.Length > index + 1 -> args.[index + 1]
    | _ -> getDefaultRulesPath()

let ensureRulesFileExists (rulesPath: string) =
    if not (File.Exists rulesPath) then
        let template = """# Default rules for Comdirect2YNAB
rules: []
"""
        File.WriteAllText(rulesPath, template)
        Console.WriteLine($"Info: Created default rules file at '{rulesPath}'. Please edit it to add your rules.")

let private parseRulesConfig (rulesPath: string) =
    match YamlConfig.parseRulesFile rulesPath with
    | Ok (conf: YamlConfig.RulesConfig) ->
        if conf.Rules |> List.isEmpty then
            Console.WriteLine($"Info: No rules defined in '{rulesPath}'. Proceeding without categorization.")
        conf
    | Error errMsg ->
        Console.WriteLine($"Warning: Could not load rules from '{rulesPath}': {errMsg}. Proceeding without rules.")
        { Rules = [] }

let private compileRulesAsync (config: Config.Config) (rulesConfig: YamlConfig.RulesConfig) =
    async {
        if not (rulesConfig.Rules |> List.isEmpty) then
            let! categoryMapResult = RulesEngine.fetchCategories config.YNAB_Api.Secret config.Transfer.YNAB_Budget
            match categoryMapResult with
            | Error errMsg ->
                Console.Error.WriteLine $"Error fetching YNAB categories: {errMsg}"
                return []
            | Ok categoryMap ->
                match RulesEngine.compileRules rulesConfig categoryMap with
                | Ok compiled -> return compiled
                | Error compileErrors ->
                    Console.Error.WriteLine $"Error compiling rules: {compileErrors}"
                    return []
        else
            return []
    }

let loadAndCompileRules (config: Config.Config) (rulesPath: string) =
    async {
        let rulesConfig = parseRulesConfig rulesPath
        let! compiledRules = compileRulesAsync config rulesConfig
        return compiledRules
    }

let showRulesInfo (config: Config.Config) (rulesPath: string) =
    let rulesConfig = parseRulesConfig rulesPath
    
    // Fetch categories for both rules compilation and display
    let categoryMapResult =
        if String.IsNullOrWhiteSpace config.Transfer.YNAB_Budget then
            Error "YNAB budget ID not set in configuration."
        else
            try
                RulesEngine.fetchCategories config.YNAB_Api.Secret config.Transfer.YNAB_Budget
                |> Async.RunSynchronously
            with ex -> Error (sprintf "Exception fetching YNAB categories: %s" ex.Message)
    
    let compiledRules, categoryMap =
        match categoryMapResult with
        | Error errMsg ->
            Console.Error.WriteLine($"Error fetching YNAB categories: {errMsg}")
            ([], Map.empty)
        | Ok categoryMap ->
            let compiled, defaultId =
                if List.isEmpty rulesConfig.Rules then
                    ([], None)
                else
                    match RulesEngine.compileRules rulesConfig categoryMap with
                    | Ok compiled -> (compiled, None)
                    | Error compileErrors ->
                        Console.Error.WriteLine($"Error compiling rules: {compileErrors}")
                        ([], None)
            (compiled, categoryMap)

    Console.WriteLine(Environment.NewLine + "--- Rules Information ---")
    Console.WriteLine($"Rules file path: {rulesPath}")
    Console.WriteLine(Environment.NewLine + "Original Rules Configuration (from rules.yml):")

    if Seq.isEmpty rulesConfig.Rules then
        Console.WriteLine("  No rules defined.")
    else
        Console.WriteLine("  Rules:")
        rulesConfig.Rules
        |> Seq.iteri (fun i rule ->
            Console.WriteLine(sprintf "    %d. Match: '%s' -> Category: '%s'" (i+1) rule.Match rule.Category))

    Console.WriteLine(System.Environment.NewLine + "Compiled Rules & Resolved IDs:")
    if List.isEmpty compiledRules then
        Console.WriteLine("  No rules were successfully compiled.")
    else
        Console.WriteLine("  Compiled Rules:")
        compiledRules
        |> List.iteri (fun i cr ->
            Console.WriteLine(sprintf "    %d. Regex: '%s' -> Category ID: %A" (i+1) (cr.Regex.ToString()) cr.CategoryId))
    
    // // Display all YNAB categories
    // Console.WriteLine(System.Environment.NewLine + "All YNAB Categories:")
    // if Map.isEmpty categoryMap then
    //     Console.WriteLine("  No categories available (could not fetch from YNAB)")
    // else
    //     categoryMap
    //     |> Map.toSeq
    //     |> Seq.sortBy fst
    //     |> Seq.iter (fun (name, guid) ->
    //         Console.WriteLine(sprintf "  %s: %A" name guid))
    
    Console.WriteLine("--- End Rules Information ---")

let initializeRulesPath (args: string array) =
    let rulesPath = getRulesPath args
    ensureRulesFileExists rulesPath
    rulesPath
