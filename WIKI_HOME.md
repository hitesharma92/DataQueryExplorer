# DataQueryExplorer Wiki

Welcome to the **DataQueryExplorer** project — a powerful .NET 8 console utility for querying Azure Cosmos DB across multiple containers, detecting orphaned records, finding duplicates, and exporting results to Excel.

## Quick Links

- **[Home](Home)** — Overview & key features
- **[Installation & Setup](Installation-&-Setup)** — Build, configure, run
- **[Query Types Guide](Query-Types-Guide)** — Deep dive into each query strategy
- **[Architecture](Architecture)** — Design patterns, Clean Architecture, code organization
- **[API Reference](API-Reference)** — Interfaces, models, key classes
- **[Troubleshooting](Troubleshooting)** — Common errors & solutions
- **[Contributing](Contributing)** — Code review standards, testing, Git workflow

---

## Overview

### What is DataQueryExplorer?

DataQueryExplorer is a specialized tool for Azure Cosmos DB that fills a gap: **Cosmos DB's native SQL API has no JOIN across containers**, but real-world applications often need to:

- Query data across multiple containers and correlate results
- Detect orphaned parent records (parent exists, child doesn't)
- Find duplicate child entries for a given parent
- Export multi-level results in a structured, human-readable format (Excel)

### Key Features

✅ **6 Query Strategies**
- Single-container queries (with optional parameterization from Excel)
- Two-level left join (parent → child)
- Two-level orphan finder (parent with NO matching child)
- Two-level duplicate finder (child records grouped by property)
- Three-level left join (parent → child → grandchild)
- Three-level inner join (only complete chains)

✅ **Flexible Input**
- Ad-hoc SQL queries (with @parameter placeholders)
- Batch execution from Excel input file
- Interactive database/container selection

✅ **Excel Export**
- One worksheet per data level
- Proper column headers (including relationship status)
- One-click save to .xlsx file

✅ **Clean Architecture**
- Domain layer (models, interfaces, enums)
- Application layer (query strategies, SQL parsing)
- Infrastructure layer (Cosmos DB SDK, Excel writer)
- Console layer (UI, logging, orchestration)
- Comprehensive unit tests

---

## Project Statistics

- **Language:** C# 12 / .NET 8
- **Build status:** ✅ 0 errors, 0 warnings
- **Test coverage:** ✅ 43/43 tests passing
- **Code quality:** Global usings, explicit types, IDisposable patterns, static methods

---

## Quick Start

### Prerequisites
- .NET 8 SDK or later
- Azure Cosmos DB account (SQL API)
- Endpoint URL and read/write key

### Run
```powershell
cd src/DataQueryExplorer.Console
dotnet run --configuration Release
```

### First Query
1. Enter your Cosmos database endpoint (e.g., `https://myaccount.documents.azure.com:443/`)
2. Provide your account key
3. Select a database
4. Choose "Single container query"
5. Select a container
6. Enter a SQL query: `SELECT c.id, c.name FROM c`
7. View results in Excel at the output path shown

---

## Architecture at a Glance

```
User Input (Console UI)
    ↓
AppRunner (Orchestrator)
    ↓
Query Strategy (OneOf 6)
    ↓
CosmosDbRepository (Pagination, paging)
    ↓
Azure Cosmos DB SDK
    
Excel Export (ClosedXML)
    ↓
Output .xlsx file
```

**Each strategy inherits from `QueryStrategyBase`:**
- `SingleContainerQueryStrategy` — Single-level queries
- `TwoLevelJoinStrategy` — Parent + child (all results)
- `TwoLevelOrphanFinderStrategy` — Parent with NO child
- `TwoLevelDuplicateFinderStrategy` — Duplicate child detection
- `ThreeLevelJoinStrategy` — Three-level left join
- `ThreeLevelInnerJoinStrategy` — Three-level inner join (complete chains only)

---

## Code Quality & Best Practices

This project follows industry best practices:

✅ **SOLID Principles**
- Single Responsibility (each strategy handles one join pattern)
- Dependency Injection (all services injected via DI container)
- Interface Segregation (minimal interfaces like `IDatabaseClient`, `IQueryStrategy`)

✅ **Resource Management**
- All disposable resources properly managed via `using` and `IDisposable`
- Cosmos DB client singleton pattern
- Transaction-scope locking for thread-safe logging

✅ **Code Style**
- No `var`; all types explicit
- No `using var`; explicit `using()` blocks
- Global usings for reduced boilerplate
- XML doc comments on public APIs
- Static methods marked `static` (not instance methods)

✅ **Testing**
- 43 unit tests covering domain logic, parsing, disposal, and guards
- NSubstitute mocking for isolated tests
- xUnit test framework

---

## Repository Contents

```
DataQueryExplorer/
├── src/
│   ├── DataQueryExplorer.Domain/          # Models, interfaces, enums
│   ├── DataQueryExplorer.Infrastructure/  # Cosmos SDK, Excel I/O
│   ├── DataQueryExplorer.Application/     # Query strategies, parsing
│   └── DataQueryExplorer.Console/         # UI, orchestration, logging
├── tests/
│   └── DataQueryExplorer.Tests/           # 43 passing unit tests
├── publish/                               # Release binaries
└── README.md, CONTRIBUTING.md             # Project docs
```

---

## Getting Help

- 📖 **[Installation & Setup](Installation-&-Setup)** — Step-by-step build & configuration
- 🔍 **[Query Types Guide](Query-Types-Guide)** — Detailed walkthroughs for each strategy
- 🏗️ **[Architecture](Architecture)** — Design decisions and component interactions
- 🐛 **[Troubleshooting](Troubleshooting)** — Common issues and fixes
- 💻 **[Contributing](Contributing)** — How to submit PRs

---

## License

See the LICENSE file in the repository.

---

## Latest Updates

**April 7, 2026** — Code review fixes applied
- ✅ Critical fixes: resource disposal (IStorageWriterFactory, CosmosClient)
- ✅ High-priority: null guards, garbage collection, clean exit handling
- ✅ Test coverage expanded: 4 new test files, 43 tests passing
- ✅ All changes pushed to GitHub with comprehensive commit message

---

**Last Updated:** April 7, 2026
