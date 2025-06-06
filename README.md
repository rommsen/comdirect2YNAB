# Comdirect to YNAB Importer

This tool facilitates the transfer of transactions from your Comdirect account to YNAB (You Need A Budget), allowing for automated categorization based on user-defined rules.

## Features

*   Fetches recent transactions from your Comdirect account.
*   Imports new transactions into a specified YNAB budget and account.
*   Allows automatic categorization of transactions using a `rules.yml` file.

## Configuration

1.  **`appsettings.json`**: This file stores your API keys and account identifiers.
    *   `Comdirect_Api`: Contains `Client_Id` and `Client_Secret` for Comdirect.
    *   `YNAB_Api`: Contains your YNAB Personal Access Token (`Secret`).
    *   `Transfer`: Specifies `YNAB_Budget` (name or ID), `YNAB_Account` (ID), `Comdirect_Account` (ID), and `Days` (number of past days to fetch transactions for).
    *   You can find your YNAB Budget and Account IDs by running the "YNAB Infos" option in the tool, which will list them.

2.  **`rules.yml` (Optional - For Automatic Categorization)**:
    Create a `rules.yml` file in the root directory of the application to define rules for automatically assigning categories to your transactions during import.

    ### Purpose
    The `rules.yml` file allows you to map payee names or patterns from your bank transactions to specific YNAB category IDs. This helps automate the categorization process, saving you time.

    ### Schema
    The file should contain a list of rules under a top-level `rules:` key. Each rule is an object with two properties:
    *   `payeePattern`: A string that is treated as a regular expression (case-insensitive) to match against the payee name of the transaction.
    *   `categoryId`: The YNAB Category ID that should be assigned if the `payeePattern` matches.

    ### How it Works
    *   On import, the tool checks each transaction's payee against the `payeePattern` of each rule in `rules.yml`.
    *   **The first rule that matches is applied.** The order of rules in your `rules.yml` file matters.
    *   If no rule matches, the transaction will be imported without a category (you can categorize it manually in YNAB).
    *   If `rules.yml` is not found or is empty, all transactions will be imported without automatic categorization.
    *   If `rules.yml` has formatting or schema errors (e.g., a rule is missing `payeePattern` or `categoryId`), the application will report an error and exit at startup.

    ### Example `rules.yml`
    ```yaml
    rules:
      - payeePattern: "REWE" # Simple string match
        categoryId: "your-ynab-grocery-category-id"
      - payeePattern: ".*esso.*" # Regex to match 'Esso' anywhere, case-insensitive
        categoryId: "your-ynab-fuel-category-id"
      - payeePattern: "Amazon Marketplace"
        categoryId: "your-ynab-shopping-category-id"
      - payeePattern: "^(Netflix|Spotify)" # Regex to match if payee starts with Netflix or Spotify
        categoryId: "your-ynab-subscriptions-category-id"
    ```

    ### Finding YNAB Category IDs
    You can find your YNAB category IDs by:
    1.  Using the YNAB API directly (e.g., via `curl` or a tool like Postman) by querying the `/budgets/{budget_id}/categories` endpoint.
    2.  Often, the easiest way is to click on a category in the YNAB web application; the category ID is usually visible in the URL in your browser's address bar (e.g., `.../categories/THE_CATEGORY_ID`).

## Usage

Run the application and choose an option from the menu:
*   **YNAB Test**: Posts a hardcoded test transaction (useful for initial setup verification).
*   **Transfer Comdirect Transactions to YNAB**: Fetches new transactions from Comdirect and adds them to YNAB, applying rules from `rules.yml` if present.
*   **YNAB Infos**: Displays your YNAB budget and account names and IDs, which are needed for `appsettings.json`.

## Error Handling
*   If `rules.yml` is present but contains errors (e.g., incorrect format, missing required fields in a rule), the application will print an error message and exit at startup.
*   If `rules.yml` is not found, a message will be printed, and the application will proceed without applying any rules.
