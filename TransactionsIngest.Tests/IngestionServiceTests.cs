using TransactionsIngest.App.Models;
using TransactionsIngest.App.Services;
using TransactionsIngest.Tests.Helpers;
namespace TransactionsIngest.Tests;

public class IngestionServiceTests
{
    // Test 1:New transactions
    [Fact]
    public async Task NewTransactions_AreInserted()
    {
        var db=TestDbHelper.CreateInMemoryDb();
        var fetcher=new StubFetcher(new List<Transaction>
        {
            MakeTransaction(1001, "Wireless Mouse", 19.99m)
        });
        var service=new IngestionService(db, fetcher);
        await service.RunAsync();
        Assert.Equal(1, db.Transactions.Count());
        Assert.Equal(1, db.TransactionAudits.Count(a=>a.ChangeType=="Inserted"));
    }
    // Test 2:Changed fields are detected and recorded in audit
    [Fact]
    public async Task UpdatedTransaction_RecordsFieldChanges()
    {
        var db=TestDbHelper.CreateInMemoryDb();
        // Insert original
        var fetcher1 = new StubFetcher(new List<Transaction>
        {
            MakeTransaction(1001,"Wireless Mouse",19.99m)
        });
        await new IngestionService(db,fetcher1).RunAsync();
        // Insert same ID, different amount
        var fetcher2 = new StubFetcher(new List<Transaction>
        {
            MakeTransaction(1001,"Wireless Mouse",29.99m)
        });
        await new IngestionService(db,fetcher2).RunAsync();
        var auditRows=db.TransactionAudits
            .Where(a=>a.ChangeType=="Updated" && a.FieldName=="Amount")
            .ToList();
        Assert.Single(auditRows);
        Assert.Equal("19.99",auditRows[0].OldValue);
        Assert.Equal("29.99",auditRows[0].NewValue);
    }
    // Test 3:Missing transactions within 24 hours are revoked
    [Fact]
    public async Task MissingTransaction_IsRevoked()
    {
        var db=TestDbHelper.CreateInMemoryDb();
        // insert two transactions
        var fetcher1=new StubFetcher(new List<Transaction>
        {
            MakeTransaction(1001,"Wireless Mouse",19.99m),
            MakeTransaction(1002,"USB-C Cable",25.00m)
        });
        await new IngestionService(db,fetcher1).RunAsync();

        // 1002 is missing from snapshot
        var fetcher2=new StubFetcher(new List<Transaction>
        {
            MakeTransaction(1001,"Wireless Mouse",19.99m)
        });
        await new IngestionService(db,fetcher2).RunAsync();
        var revoked=db.Transactions.FirstOrDefault(t=>t.TransactionId==1002);
        Assert.NotNull(revoked);
        Assert.Equal("Revoked",revoked.Status);
        Assert.Equal(1,db.TransactionAudits.Count(a=>a.ChangeType=="Revoked"));
    }
    // Test 4:Repeated runs with same data produce no duplicates
    [Fact]
    public async Task RepeatedRuns_AreIdempotent()
    {
        var db=TestDbHelper.CreateInMemoryDb();
        var transactions=new List<Transaction>
        {
            MakeTransaction(1001,"Wireless Mouse",19.99m)
        };
        // Run three times with same data
        await new IngestionService(db,new StubFetcher(transactions)).RunAsync();
        await new IngestionService(db,new StubFetcher(transactions)).RunAsync();
        await new IngestionService(db,new StubFetcher(transactions)).RunAsync();

        Assert.Equal(1,db.Transactions.Count());
        Assert.Equal(1,db.TransactionAudits.Count(a=>a.ChangeType=="Inserted"));
    }
    // Test 5:Finalized transactions are not changed
    [Fact]
    public async Task FinalizedTransaction_IsNotChanged()
    {
        var db=TestDbHelper.CreateInMemoryDb();
        // Insert a transaction older than 24 hours directly
        var oldTx=MakeTransaction(1001, "Wireless Mouse", 19.99m);
        oldTx.TransactionTime=DateTime.UtcNow.AddHours(-25);
        oldTx.Status="Finalized";
        db.Transactions.Add(oldTx);
        await db.SaveChangesAsync();
        // Run ingestion with updated amount for same ID
        var fetcher=new StubFetcher(new List<Transaction>
        {
            MakeTransaction(1001, "Wireless Mouse", 99.99m)
        });
        await new IngestionService(db,fetcher).RunAsync();
        var tx=db.Transactions.First(t=>t.TransactionId==1001);
        Assert.Equal(19.99m,tx.Amount);
        Assert.Equal("Finalized",tx.Status);
    }
    private static Transaction MakeTransaction(int id,string product,decimal amount) =>
        new Transaction
        {
            TransactionId=id,
            CardLast4="1111",
            LocationCode="STO-01",
            ProductName=product,
            Amount=amount,
            TransactionTime=DateTime.UtcNow.AddHours(-2)
        };
}

public class StubFetcher : ITransactionFetcher
{
    private readonly List<Transaction> _data;
    public StubFetcher(List<Transaction> data)=>_data = data;
    public Task<List<Transaction>> FetchAsync()=>Task.FromResult(_data);
}
