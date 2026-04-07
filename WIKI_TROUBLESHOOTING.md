# Troubleshooting

Common issues and solutions.

## Connection Issues

### "Failed to connect to the database. Check your endpoint and key."

**Causes:**
- Endpoint URL is malformed
- Account key is incorrect or expired
- Cosmos DB account is in a different region or subscription
- Firewall rules block the connection

**Solutions:**

1. **Verify endpoint format:**
   - ✅ Correct: `https://myaccount.documents.azure.com:443/`
   - ❌ Wrong: `https://myaccount.documents.azure.com` (missing port)
   - ❌ Wrong: `https://myaccount/` (incomplete)

2. **Check account key:**
   - Go to **Azure Portal** → **Cosmos DB account** → **Keys**
   - Copy the **Primary Key** or **Secondary Key** (both work)
   - Keys are case-sensitive; paste exactly as shown

3. **Verify account access:**
   - Check account doesn't have IP firewall restrictions that block your IP
   - If using Private Endpoints, ensure you're on the correct vnet

4. **Test connectivity in PowerShell:**
   ```powershell
   $endpoint = "https://myaccount.documents.azure.com:443/"
   $key = "your-key-here"
   $client = [Microsoft.Azure.Cosmos.CosmosClient]::new($endpoint, $key, [Microsoft.Azure.Cosmos.CosmosClientOptions]::new())
   $profile = $client.ReadAccountAsync().Result
   Write-Host "Connected! Account: $($profile.DatabaseProperties.Id)"
   ```

---

### "No databases found. Verify your endpoint and key."

**Cause:** Credentials are valid, but account has no databases.

**Solution:**
- Create a database in Cosmos DB (Azure Portal or Azure CLI)
- Or ensure you're connecting to the right account

---

## Query Execution Issues

### "No results found for query..."

**Causes:**
- Query syntax is incorrect
- Container name is misspelled
- Data matching the WHERE clause doesn't exist
- Parameters are not being substituted correctly

**Solutions:**

1. **Test query in Azure Portal:**
   - Go to **Cosmos DB** → **Data Explorer**
   - Run the same SELECT query directly
   - Verify it returns results

2. **Check SQL syntax:**
   - Cosmos DB SQL is similar to T-SQL, but not identical
   - Reference: [Cosmos DB SQL syntax](https://learn.microsoft.com/azure/cosmos-db/nosql/query/select)
   - Common issue: Alias required (e.g., `SELECT c.id FROM c`, not `SELECT id FROM ...`)

3. **Verify parameters are extracted:**
   - The tool prints extracted parameter names to the console
   - Ensure parameter names match query placeholders (case-insensitive, but must exist)
   - Example: Query `WHERE c.id = @id` expects a parameter named `id` in the parent document

4. **Check property names are case-sensitive:**
   - Cosmos DB is case-sensitive by default
   - If your query uses `c.Name` but property is `c.name`, you'll get no results

---

### "Duplicate found: ... count = X" appears but nothing is written

**Cause:** Threshold is set too high.

**Solutions:**
- If threshold is 3 and duplicates have count = 2, they won't be written
- Lower the threshold to match your data
- The threshold is the **minimum count to flag as a duplicate**

---

## File & Path Issues

### "Output folder 'C:\Output\' does not exist."

**Solution:** The tool offers to create it. Press Enter to proceed, or provide a different path that exists.

---

### "No such file or directory" when loading parameter Excel

**Cause:** The Excel file path you entered doesn't exist or is misspelled.

**Solutions:**
1. Enter the full path: `C:\Data\parameters.xlsx` (not just `parameters.xlsx`)
2. Use forward slashes on Linux/macOS: `/home/user/parameters.xlsx`
3. Check the file extension is `.xlsx` (not `.xls` or `.csv`)

---

### Excel file opens but shows incorrect data

**Cause:** Columns are misaligned or headers are missing.

**Solution:**
- Verify column order matches your query's SELECT clause
- Example: `SELECT c.id, c.date, c.total FROM c` produces columns `id`, `date`, `total` in that order
- Check that headers are shown (row 1 should be bold with column names)

---

## Performance Issues

### "Query is very slow" or "Connection timed out"

**Causes:**
- Large dataset; paging is slow
- Cosmos DB account has limited RUs (Request Units)
- Network latency

**Solutions:**

1. **Add WHERE clause to filter data:**
   ```
   ❌ SELECT c.id FROM c  ← scans entire container
   ✅ SELECT c.id FROM c WHERE c.year = 2024  ← scans only 2024
   ```

2. **Increase RUs on your Cosmos container:**
   - Minimum: 400 RUs/sec
   - Recommended for large queries: 1000+ RUs/sec
   - Auto-scale available (scales up to 4000 RUs during peaks)

3. **Reduce page size (advanced):**
   - Edit `AppConstants.DefaultMaxItemsPerPage` from 2000 to 500-1000
   - Smaller pages = more requests, but each is faster

4. **Run during off-peak hours:**
   - If sharing a Cosmos account, run queries when others aren't

---

### "Out of Memory" exception

**Cause:** Results are too large to hold in memory.

**Solutions:**
1. **Filter aggressively:**
   ```
   SELECT c.id, c.date FROM c  ← fewer columns = less memory
   ```

2. **Exclude large fields:**
   - If container has large objects (e.g., JSON blobs), don't select them
   - Example: `SELECT c.id, c.summary FROM c` instead of `SELECT * FROM c`

3. **Reduce date range:**
   - `WHERE c.date >= 2024-01-01 AND c.date < 2024-02-01` (1 month)
   - Instead of 12 months at once

---

## Logging & Debugging

### "Where is the log file?"

**Location:** Console output shows the path at startup.

**Format:**
```
Log file: C:\JTS-Code-Repository\Utilities\Cosmos\DataQueryExplorer\Logs - DataQueryExplorer\Log-2024-12-31-14-25-33-DataQueryExplorer.txt
```

**Content:** Timestamped entries of every operation, including SQL queries executed and errors.

---

### "How do I enable more verbose logging?"

**Current logging levels:**
- `LogInfo()` — Detailed information messages (written to file only)
- `LogToConsole()` — Shown on screen and in file
- `LogError()` — Exceptions and errors

**To add more verbose logging:**
1. Edit `ConsoleApplicationLogger.cs`
2. Add calls to `logger.LogInfo(...)` at key points
3. Rebuild and run

---

## Resource Cleanup

### "Application didn't shut down cleanly" or "Excel file still locked"

**Cause:** Exception occurred and resources weren't disposed.

**Solution:**
- Check the log file for exceptions
- If you see `ObjectDisposedException` or `IOException`, report it on GitHub

---

### "Log file keeps growing; disk space is limited"

**Solution:** Manually clean up old log files.

```powershell
# Windows
Remove-Item ".\Logs - DataQueryExplorer\*" -OlderThan (Get-Date).AddDays(-7)

# Linux/macOS
find ./Logs\ -\ DataQueryExplorer -type f -mtime +7 -delete
```

---

## Menu & Input Issues

### "Invalid selection. Please enter a number between 1 and 6"

**Cause:** You entered a value outside the range.

**Solution:** Enter only `1`, `2`, `3`, `4`, `5`, or `6` for query strategy selection.

---

### "Arrow keys don't work in database/container selector"

**Cause:** On some terminals (e.g., VS Code integrated terminal), arrow keys may not register.

**Solutions:**
1. Use `T` to switch to **text input mode**
2. Type the name (or number) of the database/container you want
3. Or use Windows Terminal instead of PowerShell ISE

---

### "Escape key exits the app immediately"

**Expected behavior:** Pressing Escape in a menu should close the app with a clean exit message.

**If it crashes instead:** Check the log file for the exception; report on GitHub.

---

## Build & Test Issues

### "dotnet build failed: CS0246 The type or namespace... could not be found"

**Cause:** Missing NuGet package reference or namespace.

**Solution:**
```bash
dotnet clean
dotnet restore
dotnet build
```

---

### "dotnet test failed: Some tests failed"

**Current status:** All 43 tests should pass. If any fail, check:
1. Did you modify source code? Revert or fix your changes.
2. Did you modify test code? Ensure your changes don't break assertions.
3. Run a single test for debugging:
   ```bash
   dotnet test --filter "TestName"
   ```

---

## Report a Bug

If you encounter an issue not listed here:

1. **Gather information:**
   - Full error message (from console or log file)
   - Steps to reproduce
   - Environment (OS, .NET version, Cosmos DB region)

2. **Open a GitHub issue:**
   - Go to https://github.com/hitesharma92/DataQueryExplorer/issues
   - Include the above information
   - Attach the log file (if safe to share)

---

## Next Steps

- **[Installation & Setup](Installation-&-Setup)** — Verify your environment
- **[Query Types Guide](Query-Types-Guide)** — Understand query semantics
- **[Architecture](Architecture)** — Understand the code structure
