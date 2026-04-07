# Installation & Setup

Get DataQueryExplorer up and running in minutes.

## Prerequisites

### System Requirements
- **OS:** Windows, macOS, or Linux
- **.NET SDK:** 8.0 or later ([download](https://dotnet.microsoft.com/download))
- **Git:** For cloning the repository

### Azure Requirements
- **Azure Cosmos DB Account** (SQL API)
  - Database name
  - Container names
  - Account endpoint URL
  - Account key (read/write)

## Step 1: Clone the Repository

```bash
git clone https://github.com/hitesharma92/DataQueryExplorer.git
cd DataQueryExplorer
```

## Step 2: Build the Solution

```bash
dotnet build
```

**Expected output:**
```
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

## Step 3: Run Tests (Optional but Recommended)

```bash
dotnet test
```

**Expected output:**
```
Passed: 43
Failed: 0
Skipped: 0
```

## Step 4: Prepare to Run

### Option A: Run from Source (Development)

```bash
cd src/DataQueryExplorer.Console
dotnet run --configuration Debug
```

### Option B: Publish Release Binary

```bash
dotnet publish --configuration Release --output ./publish
cd publish
./DataQueryExplorer.Console.exe
```

## Step 5: Configure & Connect

When you run the application, you'll be prompted for:

1. **Database Endpoint**
   - Format: `https://{account-name}.documents.azure.com:443/`
   - Example: `https://mycosmosdb.documents.azure.com:443/`

2. **Account Key**
   - Found in Azure Portal → Cosmos DB account → Keys
   - Use the primary or secondary read/write key
   - **Note:** The key is echoed as you type, then sent to Cosmos for verification

3. **Database Name**
   - The name of your target database within the Cosmos account
   - Click "Up/Down" to navigate the list, or "T" for text input

4. **Output Folder**
   - Path where Excel results will be saved
   - Example: `C:\Output\` or `/tmp/cosmosdb-export/`
   - The folder will be created if it doesn't exist

## Step 6: Select Query Type

You'll see 6 options:

```
=== Select Query Type ===
  1. Single container query (with optional @param Excel input)
  2. Two-level join — all results
  3. Two-level join — orphans only (no child found)
  4. Two-level join — find duplicate child records
  5. Three-level join — all results
  6. Three-level join — inner match only (all three levels found)
```

Choose based on your use case (see [Query Types Guide](Query-Types-Guide) for details on each).

## Step 7: Provide Query & Container Details

Depending on your choice:
- **Container name(s)** — Select from the list shown
- **SQL query** — Example: `SELECT c.id, c.name FROM c WHERE c.type = 'order'`
- **Parameter file** — For parameterized queries, provide the .xlsx file path

## Step 8: View Results

After execution:
1. Console shows progress: `Found X record(s). Fetching...`
2. Results are written to Excel at the path shown: `C:\Output\dbname - QueryOutput_...xlsx`
3. Press any key to exit

## Troubleshooting Installation

| Issue | Solution |
|---|---|
| `.NET SDK not found` | Install from https://dotnet.microsoft.com/download |
| `Build failed: CS0246` | Clean: `dotnet clean`, then rebuild: `dotnet build` |
| `Connection timeout` | Check endpoint URL format and firewall rules |
| `403 Unauthorized` | Verify account key is correct; check key expiration |
| `No databases found` | Confirm the key has read/write permissions; verify database exists |

## Next Steps

- **[Query Types Guide](Query-Types-Guide)** — Learn how to run each query type
- **[Architecture](Architecture)** — Understand the codebase
- **[Troubleshooting](Troubleshooting)** — Common runtime issues
