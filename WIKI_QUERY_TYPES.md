# Query Types Guide

DataQueryExplorer supports 6 query strategies for different analysis scenarios. Choose the one that matches your use case.

## 1. Single Container Query

**When to use:** Query a single container with optional parameterization.

### Without Parameters

```
Select: Single container query
Container: orders
Query: SELECT c.id, c.date, c.total FROM c WHERE c.year = 2024
```

Results are streamed page-by-page to Excel. Progress bar shows how many records have been processed.

### With Parameters

If your query contains `@parameters`, the tool will prompt for an **input Excel file**:

```
Query: SELECT c.id, c.detail FROM c WHERE c.orderId = @orderId
```

The input file should have:
- **Row 1:** Column headers (e.g., `orderId`, `productId`)
- **Rows 2+:** Data values

The query runs once per input row, substituting the parameter values. Results from all executions are combined in the output Excel file.

**Example input file:**
```
orderId
ORD-001
ORD-002
ORD-003
```

**Output:** 3 separate result sets, one per input row.

---

## 2. Two-Level Join — All Results

**When to use:** Find all parent-child relationships, including parents with no children.

### Scenario

```
Database: Sales
Parent container: orders (documents: order_001, order_002, ...)
Child container: order_items (documents with field: orderId)

Query 1 (parent):  SELECT c.id, c.date FROM c
Query 2 (child):   SELECT c.itemId, c.qty FROM c WHERE c.orderId = @id
```

### Output Excel

**Sheet 1: ParentResult**
```
id         | date       | IsChildFound
order_001  | 2024-01-15 | true
order_002  | 2024-01-16 | false
order_003  | 2024-01-17 | true
```

**Sheet 2: ChildResult**
```
itemId  | qty
item-A  | 3
item-B  | 2
item-C  | 5
```

**Logic:**
- For each parent, the child query is executed with the parent's `@id` value
- All child results are combined into one sheet
- Parent sheet includes `IsChildFound` (true if at least one child exists)

---

## 3. Two-Level Join — Orphans Only

**When to use:** Find parent records with **no matching children** (data integrity checks).

### Scenario

```
Scenario: Find orders with no order_items
Parent: orders
Child: order_items
```

### Output Excel

**Sheet 1: OrphanedParentResult**
```
id         | date
order_002  | 2024-01-16
order_004  | 2024-01-18
```

**Logic:**
- For each parent, child query is executed
- **Only parents with zero children are written** to the output
- Useful for detecting dangling references or incomplete data

---

## 4. Two-Level Join — Duplicate Finder

**When to use:** Detect child records that appear multiple times for the same parent (e.g., duplicate invoice items).

### Prerequisites

Your `QueryExecutionRequest` must include:
- `GroupByProperty` — The child field to group on
- `GroupByPropertyThreshold` — Minimum count to flag as "duplicate"

### Scenario

```
Parent: orders
Child: order_items
GroupByProperty: productId
Threshold: 2 (flag products appearing 2+ times in the same order)

Data:
Order ORD-001 has items:
  - productId: PROD-X, qty: 1
  - productId: PROD-X, qty: 2  ← duplicate!
  - productId: PROD-Y, qty: 1
  - productId: PROD-Y, qty: 1  ← duplicate!
```

### Output Excel

**Sheet 1: ParentResult**
```
id         | IsChildFound
order_001  | true
```

**Sheet 2: DuplicateChildResult**
```
productId | qty
PROD-X    | 1
PROD-X    | 2
PROD-Y    | 1
PROD-Y    | 1
```

**Logic:**
- Child records are grouped by `GroupByProperty`
- Groups with count > threshold are flagged as duplicates
- Only parents that have at least one duplicate child group are written
- Duplicate child results include **all matching items** (not deduplicated)

---

## 5. Three-Level Join — All Results

**When to use:** Query across 3 containers (parent → child → grandchild) and keep all relationships.

### Scenario

```
Parent: orders
Child: order_items
Grandchild: item_details

Query 1 (parent):     SELECT c.id, c.date FROM c
Query 2 (child):      SELECT c.itemId FROM c WHERE c.orderId = @id
Query 3 (grandchild): SELECT c.detailId FROM c WHERE c.itemId = @id
```

### Output Excel

**Sheet 1: ParentResult**
```
id         | IsSecondChildFound
order_001  | true
order_002  | false
```

**Sheet 2: SecondLevelResult**
```
itemId     | IsThirdChildFound
item-A     | true
item-B     | false
```

**Sheet 3: ThirdLevelResult**
```
detailId
detail-1
detail-2
```

**Logic:**
- Parent + all children written (even without grandchildren)
- Child rows include `IsThirdChildFound` flag
- Grandchild results are combined into one sheet
- Left-join semantics (keeps parents/children even if no grandchildren)

---

## 6. Three-Level Join — Inner Match Only

**When to use:** Find only **complete chains** where parent → child → grandchild all have results.

### Scenario

Same as Three-Level Join above, but:
- Parent is **only written if at least one complete parent→child→grandchild chain exists**
- Child is **only written if its grandchildren exist**

### Example Data Before Join

```
order_001 → item-A → detail-1, detail-2  ✅ complete chain
order_001 → item-B → (no details)        ❌ incomplete
order_002 → (no items)                   ❌ incomplete
```

### Output Excel

**Only complete chain is written:**

**ParentResult:**
```
id
order_001
```

**SecondLevelResult:**
```
itemId
item-A
```

**ThirdLevelResult:**
```
detailId
detail-1
detail-2
```

**Logic:**
- For each parent, fetch all children
- For each child, fetch grandchildren
- **Collect children that have grandchildren**
- **Only write parent + collected children if any were found**
- Grandchildren written only if parent was written

---

## Parameter Substitution

All queries accept `@paramName` placeholders. Parameters are extracted and substituted from:

1. **Parent document values** (for child/grandchild queries)
2. **Input Excel file** (for parameterized single-container queries)

### Example

```
Parent Query:  SELECT c.id, c.region FROM c
Child Query:   SELECT c.name FROM c WHERE c.parentId = @id AND c.region = @region
```

The `@id` comes from parent.id, and `@region` comes from parent.region.

**Note:** Parameter names are case-insensitive but must match the query syntax.

---

## Performance Tips

### Large Datasets

- **Page size:** Default is 2000 items per Cosmos request (configurable via `AppConstants.DefaultMaxItemsPerPage`)
- **Memory:** Results are held in-memory and written to Excel at the end
- **RUs:** Cosmos DB charges per query. Larger queries with joins cost more RUs

### Optimization

- Filter in SQL: `WHERE c.type = 'order'` → fewer items to join
- Narrow column selection: `SELECT c.id, c.date` → less data transferred
- Run during off-peak hours → lower contention, faster queries

### Monitoring

- Console shows progress: `Found X record(s). Fetching...`
- Log file location displayed at startup
- Check `./Logs - DataQueryExplorer/` for detailed execution logs

---

## Next Steps

- **[Troubleshooting](Troubleshooting)** — Debug common query issues
- **[Architecture](Architecture)** — Learn how strategies work internally
- **[API Reference](API-Reference)** — Details on query models and enums
