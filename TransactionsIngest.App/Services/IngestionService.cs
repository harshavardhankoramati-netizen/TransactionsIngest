using Microsoft.EntityFrameworkCore;
using TransactionsIngest.App.Data;
using TransactionsIngest.App.Models;
namespace TransactionsIngest.App.Services;

public class IngestionService
{
    private readonly AppDbContext _db;
    private readonly ITransactionFetcher _fetcher;
    public IngestionService(AppDbContext db, ITransactionFetcher fetcher)
    {
        _db=db;
        _fetcher=fetcher;
    }
    public async Task RunAsync()
    {
        var incoming=await _fetcher.FetchAsync();
        var cutoff=DateTime.UtcNow.AddHours(-24);
        var incomingIds=incoming.Select(t=>t.TransactionId).ToHashSet();
        await using var dbTransaction=await _db.Database.BeginTransactionAsync();
        try
        {
            // Upsert each incoming transaction
            foreach (var incomingTx in incoming)
            {
                var existing=await _db.Transactions
                    .FirstOrDefaultAsync(t=>t.TransactionId==incomingTx.TransactionId);
                if (existing==null)
                {
                    // New transaction
                    incomingTx.Status="Active";
                    incomingTx.LastUpdatedAt=DateTime.UtcNow;
                    _db.Transactions.Add(incomingTx);
                    _db.TransactionAudits.Add(new TransactionAudit
                    {
                        TransactionId=incomingTx.TransactionId,
                        ChangeType="Inserted",
                        ChangedAt=DateTime.UtcNow
                    });
                }
                else
                {
                    // Existing transaction
                    if (existing.Status=="Finalized")
                        continue;
                    // Detect changes
                    var changes=DetectChanges(existing, incomingTx);
                    if (changes.Any())
                    {
                        existing.CardLast4=incomingTx.CardLast4;
                        existing.LocationCode=incomingTx.LocationCode;
                        existing.ProductName=incomingTx.ProductName;
                        existing.Amount=incomingTx.Amount;
                        existing.TransactionTime=incomingTx.TransactionTime;
                        existing.Status="Active";
                        existing.LastUpdatedAt = DateTime.UtcNow;
                        // Store each changed field
                        foreach (var change in changes)
                        {
                            _db.TransactionAudits.Add(new TransactionAudit
                            {
                                TransactionId=existing.TransactionId,
                                ChangeType="Updated",
                                FieldName=change.FieldName,
                                OldValue=change.OldValue,
                                NewValue=change.NewValue,
                                ChangedAt=DateTime.UtcNow
                            });
                        }
                    }
                }
            }
            await _db.SaveChangesAsync();
            // Revoke transactions that are missing from snapshot but still within 24 hours
            var toRevoke=await _db.Transactions
                .Where(t=>t.TransactionTime>=cutoff
                         && t.Status=="Active"
                         && !incomingIds.Contains(t.TransactionId))
                .ToListAsync();
            foreach (var tx in toRevoke)
            {
                tx.Status = "Revoked";
                tx.LastUpdatedAt=DateTime.UtcNow;
                _db.TransactionAudits.Add(new TransactionAudit
                {
                    TransactionId=tx.TransactionId,
                    ChangeType="Revoked",
                    ChangedAt=DateTime.UtcNow
                });
            }
            await _db.SaveChangesAsync();
            // Finalize transactions which are older than 24 hours
            var toFinalize=await _db.Transactions
                .Where(t=>t.TransactionTime<cutoff
                         && t.Status=="Active")
                .ToListAsync();
            foreach (var tx in toFinalize)
            {
                tx.Status="Finalized";
                tx.LastUpdatedAt=DateTime.UtcNow;
                _db.TransactionAudits.Add(new TransactionAudit
                {
                    TransactionId=tx.TransactionId,
                    ChangeType="Finalized",
                    ChangedAt=DateTime.UtcNow
                });
            }
            await _db.SaveChangesAsync();
            await dbTransaction.CommitAsync();
            Console.WriteLine($"Ingestion complete.");
            Console.WriteLine($"Processed:{incoming.Count} transactions");
            Console.WriteLine($"Revoked:{toRevoke.Count}");
            Console.WriteLine($"Finalized:{toFinalize.Count}");
        }
        catch (Exception ex)
        {
            await dbTransaction.RollbackAsync();
            Console.WriteLine($"Ingestion failed:{ex.Message}");
            throw;
        }
    }

    private List<(string FieldName, string OldValue, string NewValue)> DetectChanges(Transaction existing, Transaction incoming)
    {
        var changes=new List<(string,string,string)>();
        if(existing.CardLast4!=incoming.CardLast4)
            changes.Add(("CardLast4",existing.CardLast4,incoming.CardLast4));
        if(existing.LocationCode!=incoming.LocationCode)
            changes.Add(("LocationCode",existing.LocationCode,incoming.LocationCode));
        if(existing.ProductName != incoming.ProductName)
            changes.Add(("ProductName", existing.ProductName, incoming.ProductName));
        if(existing.Amount!=incoming.Amount)
            changes.Add(("Amount",existing.Amount.ToString(),incoming.Amount.ToString()));
        if (existing.TransactionTime!=incoming.TransactionTime)
            changes.Add(("TransactionTime",existing.TransactionTime.ToString("o"),incoming.TransactionTime.ToString("o")));
        return changes;
    }
}