# TransactionsIngest

A .NET 10 Console application that runs as an hourly ingestion job for retail payment transactions. It fetches a 24-hour snapshot of transactions, upserts records, detects field-level changes, revokes missing transactions, and finalizes old records — all with a full audit trail.

---

## Project Structure
```
TransactionsIngest/
├── TransactionsIngest.App/
│   ├── Data/
│   │   ├── AppDbContext.cs         # DbContext
│   │   └── AppDbContextFactory.cs  # Design-time factory for migrations
│   ├── Models/
│   │   ├── Transaction.cs          # Main transaction entity
│   │   └── TransactionAudit.cs     # Audit trail entity
│   ├── Services/
│   │   ├── ITransactionFetcher.cs  # Fetcher interface
│   │   ├── MockTransactionFetcher.cs # Mock API response
│   │   └── IngestionService.cs     # Core ingestion logic
│   ├── Program.cs                  # Entry point and DI wiring
│   └── appsettings.json            # Configuration
└── TransactionsIngest.Tests/
    ├── Helpers/
    │   └── TestDbHelper.cs         # In-memory SQLite helper
    └── IngestionServiceTests.cs    # Automated tests
```

---

## Build and Run

### Steps
```bash
# Clone the repository
cd TransactionsIngest

# Apply database migrations
cd TransactionsIngest.App
dotnet ef database update
cd ..

# Run the app
cd TransactionsIngest.App
dotnet run
```

### Run Tests
```bash
cd TransactionsIngest.Tests
dotnet test
```

---

## Approach

### Upsert
Each incoming transaction is matched by `TransactionId`. If it does not exist it is inserted. If it exists, each tracked field is compared individually and any changes are recorded in the audit table with the old and new values.

### Revocation
After upserting, the app queries for any `Active` transactions within the last 24 hours that were absent from the current snapshot and marks them as `Revoked`.

### Finalization
Any `Active` transaction older than 24 hours is marked as `Finalized` and will not be modified in future runs.

### Idempotency
The entire run is wrapped in a single database transaction. Repeated runs with unchanged input produce no new rows or audit entries.

### Privacy
Only the last 4 digits of the card number are stored rather than the full card number.

---

## Assumptions

- The external scheduler handles the hourly trigger — the app is single-run only.
- The mock fetcher simulates the real API. Swapping in a real HTTP fetcher requires implementing `ITransactionFetcher` and updating `Program.cs`.
- `TransactionTime` from the API is always in UTC.
- `ProductName` and `LocationCode` are truncated to 20 characters as per the data model spec.

---