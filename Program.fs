open System 
open System.IO // Added
open FsToolkit.ErrorHandling
open Comdirect2YNAB // Added
open Helper
open Config
open UI.Menu // Added
open Thoth.Json.Net // Preserved

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
    Directory.CreateDirectory(appConfigFolder) |> ignore // Ensure it exists
    Path.Combine(appConfigFolder, "rules.yml")

let getYnabInfo config =
  let ynabApi = new YNAB.SDK.API(config.YNAB_Api.Secret)

  asyncResult {
    let! budgetInfo = YNAB.Infos.budgetsWithAccounts ynabApi
    Console.WriteLine(System.Environment.NewLine)
    Console.WriteLine("Budgets:")
    budgetInfo
    |> Array.iteri (
        fun i budgetInfo ->
          Console.WriteLine(sprintf ("%i. %s (Id: %s)") (i+1) budgetInfo.Name (budgetInfo.Id.ToString()))
          Console.WriteLine("  Accounts:")
          budgetInfo.Accounts
          |> List.iteri(
              fun j accountInfo -> 
                Console.WriteLine(sprintf ("  %i. %s (Id: %s)") (j+1) accountInfo.Name (accountInfo.Id.ToString())) 
        )
    )

    return ()
  }
  |> runAsync
  |> function | Ok _ -> printfn "Copy and paste id to appsettings.json. Press key to continue" | Error error -> printfn "Error %s" error


let transfer (config: Config.Config) (compiledRules: RulesEngine.CompiledRule list) (defaultCategoryId: Guid option) = // Modified signature
  let ynabApi = new YNAB.SDK.API(config.YNAB_Api.Secret)
  Console.Write("Username: ")
  let username = Console.ReadLine()
  Console.Write("Password: ")
  let password = Console.ReadLine()
  let credentials = Comdirect.API.Credentials.Create username password

  asyncResult {
    let! requestInfo,tokens = Comdirect.Login.login credentials config.Comdirect_Api

    let! transactions =
      Comdirect.Transactions.getLastXDays 
        config.Transfer.Days
        requestInfo 
        tokens 
        config.Transfer.Comdirect_Account

    return!
      YNAB.Transactions.addNonexistent 
        config.Transfer.Days 
        config.Transfer.YNAB_Budget
        (Guid.Parse(config.Transfer.YNAB_Account))
        transactions 
        ynabApi
        compiledRules // New parameter
        defaultCategoryId // New parameter
  }
  |> runAsync
  |> function | Ok result -> printfn "Finished %A" result | Error error -> printfn "Error %s" error

let test (config: Config.Config) (compiledRules: RulesEngine.CompiledRule list) (defaultCategoryId: Guid option) = // Modified signature
  let ynabApi = new YNAB.SDK.API(config.YNAB_Api.Secret)

  asyncResult {
    let transactions : Comdirect.Transactions.Transaction list =
      [
        {
          Reference = System.Guid.NewGuid().ToString()
          Booking_Date = DateTime.Now
          Amount = 20M
          Name = Some "test"
          Info = "Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam erat, sed diam voluptua. At vero eos et accusam et justo duo dolores et ea rebum. Stet clita kasd gubergren, no sea takimata sanctus est Lorem ipsum dolor sit amet. Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam erat, sed diam voluptua. At vero eos et accusam et justo duo dolores et ea rebum. Stet clita kasd gubergren, no sea takimata sanctus est Lorem ipsum dolor sit amet. Duis autem vel eum iriure dolor in hendrerit in vulputate velit esse molestie consequat, vel illum dolore eu f"
        }
      ]

    return!
      YNAB.Transactions.addNonexistent 
        config.Transfer.Days 
        config.Transfer.YNAB_Budget
        (Guid.Parse(config.Transfer.YNAB_Account))
        transactions 
        ynabApi
        compiledRules // New parameter
        defaultCategoryId // New parameter
  }
  |> runAsync
  |> function | Ok result -> printfn "Finished %A" result | Error error -> printfn "Error %s" error

[<EntryPoint>]
let main args = // Added args parameter
 
  let config = Config.fetch()
  // Create YNAB API client for fetching categories for rule compilation
  let ynabApiForRules = new YNAB.SDK.API(config.YNAB_Api.Secret)

  // Determine rules file path from --rules arg or default
  let rulesPath =
      match args |> Array.tryFindIndex (fun arg -> arg.ToLowerInvariant() = "--rules") with
      | Some index when args.Length > index + 1 -> args.[index + 1]
      | _ -> getDefaultRulesPath()

  // Ensure rules file exists; if not, write a default template
  if not (File.Exists rulesPath) then
      let template = """# Default rules for Comdirect2YNAB
# Optional: default_category: "Uncategorized"
rules: []
"""
      File.WriteAllText(rulesPath, template)
      Console.WriteLine($"Info: Created default rules file at '{rulesPath}'. Please edit it to add your rules.")

  // Parse Rules File (now tolerant of missing/malformed files)
  let rulesConfig =
      match YamlConfig.parseRulesFile rulesPath with
      | Ok (conf: YamlConfig.RulesConfig) ->
          if conf.Rules |> Seq.isEmpty && conf.DefaultCategory.IsNone then
              Console.WriteLine($"Info: No rules or default category defined in '{rulesPath}'. Proceeding without categorization.")
          conf
      | Error errMsg ->
          Console.WriteLine($"Warning: Could not load rules from '{rulesPath}': {errMsg}. Proceeding without rules.")
          { DefaultCategory = None; Rules = System.Collections.Generic.List<YamlConfig.Rule>() } // Use empty generic List for fallback

  // Fetch YNAB Categories & Compile Rules
  let compiledRules, defaultCategoryId =
      async {
          // Only fetch categories if there are rules to compile or a default category name to resolve
          if not (rulesConfig.Rules |> Seq.isEmpty) || rulesConfig.DefaultCategory.IsSome then
              let! categoryMapResult = RulesEngine.fetchCategories config.YNAB_Api.Secret config.Transfer.YNAB_Budget
              match categoryMapResult with
              | Error errMsg ->
                  Console.Error.WriteLine $"Error fetching YNAB categories: {errMsg}"
                  Environment.Exit(1)
                  return ([], None) // Dummy return for type compatibility, program exits above
              | Ok categoryMap ->
                  match RulesEngine.compileRules rulesConfig categoryMap with
                  | Ok (compiled, defaultId) -> return (compiled, defaultId)
                  | Error compileErrors ->
                      Console.Error.WriteLine $"Error compiling rules from '{rulesPath}': {compileErrors}"
                      Environment.Exit(1)
                      return ([], None) // Dummy return
          else
              // No rules and no default category name to resolve, so no need to fetch/compile
              return ([], None)
      }
      |> Async.RunSynchronously

  // Function to display rules information
  let showRulesInfo (config: Config.Config) =
    // Re-parse rules file
    let rulesConfig =
        match YamlConfig.parseRulesFile rulesPath with
        | Ok conf ->
            if conf.Rules |> Seq.isEmpty && conf.DefaultCategory.IsNone then
                Console.WriteLine($"Info: No rules or default category defined in '{rulesPath}'. Proceeding without categorization.")
            conf
        | Error errMsg ->
            Console.WriteLine($"Warning: Could not load rules from '{rulesPath}': {errMsg}. Proceeding without rules.")
            { DefaultCategory = None; Rules = System.Collections.Generic.List<YamlConfig.Rule>() } // Use empty generic List for fallback

    // Fetch YNAB categories and compile rules
    let compiledRules, defaultCategoryId =
        if Seq.isEmpty rulesConfig.Rules && rulesConfig.DefaultCategory.IsNone then
            // No rules or default category: skip fetching and compiling
            ([], None)
        else
            let ynabApiForRules = new YNAB.SDK.API(config.YNAB_Api.Secret)
            // Guard against missing YNAB budget ID to avoid NullReferenceException
            let categoryMapResult =
                if String.IsNullOrWhiteSpace config.Transfer.YNAB_Budget then
                    Error "YNAB budget ID not set in configuration."
                else
                    // Attempt to fetch categories safely
                    try
                        RulesEngine.fetchCategories config.YNAB_Api.Secret config.Transfer.YNAB_Budget
                        |> Async.RunSynchronously
                    with ex -> Error (sprintf "Exception fetching YNAB categories: %s" ex.Message)
            match categoryMapResult with
            | Error errMsg ->
                Console.Error.WriteLine($"Error fetching YNAB categories: {errMsg}")
                ([], None)
            | Ok categoryMap ->
                match RulesEngine.compileRules rulesConfig categoryMap with
                | Ok (compiled, defaultId) -> (compiled, defaultId)
                | Error compileErrors ->
                    Console.Error.WriteLine($"Error compiling rules from '{rulesPath}': {compileErrors}")
                    ([], None)

    // Display rules information
    Console.WriteLine(System.Environment.NewLine + "--- Rules Information ---")
    Console.WriteLine($"Rules file path: {rulesPath}")
    Console.WriteLine(System.Environment.NewLine + "Original Rules Configuration (from rules.yml):")
    let defaultCategoryNameStr = rulesConfig.DefaultCategory |> Option.defaultValue "Not set"
    Console.WriteLine($"  Default Category Name: {defaultCategoryNameStr}")
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
    let resolvedDefaultCatIdStr = defaultCategoryId |> Option.map (fun id -> id.ToString()) |> Option.defaultValue "Not set or not found"
    Console.WriteLine($"  Resolved Default Category ID: {resolvedDefaultCatIdStr}")
    Console.WriteLine("--- End Rules Information ---")

  // Define menu actions (formerly part of a tuple 'let main = ([...], ignore)')
  let menuActions =
    [
      ("Show Rules Info", fun cfg -> showRulesInfo cfg ; waitForAnyKey()) // New menu item
      ("YNAB Test", fun cfg -> test cfg compiledRules defaultCategoryId ; waitForAnyKey()) // Pass compiled rules and default category
      ("Transfer Comdirect Transactions to YNAB", fun cfg -> transfer cfg compiledRules defaultCategoryId ; waitForAnyKey()) // Pass compiled rules and default category
      ("YNAB Infos", fun cfg -> getYnabInfo cfg ; waitForAnyKey())
    ]

  // Initialize menu
  // Assumes UI.Menu.initialize expects: action list -> config -> title string -> unit
  UI.Menu.initialize  config "Comdirect to Ynab" (menuActions,ignore)
  |> ignore // Original code had ignore here

  0 // Return success code