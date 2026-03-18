using TransactionsIngest.App.Models;
namespace TransactionsIngest.App.Services;

public class MockTransactionFetcher : ITransactionFetcher
{
    public Task<List<Transaction>> FetchAsync()
    {
        var now=DateTime.UtcNow;
        var transactions=new List<Transaction>
        {
            new Transaction
            {
                TransactionId=1001,
                CardLast4="1111",
                LocationCode="STO-01",
                ProductName="Wireless Mouse",
                Amount=19.99m,
                TransactionTime=now.AddHours(-2)
            },
            new Transaction
            {
                TransactionId=1002,
                CardLast4="0002",
                LocationCode="STO-02",
                ProductName="USB-C Cable",
                Amount=25.00m,
                TransactionTime=now.AddHours(-5)
            },
            new Transaction
            {
                TransactionId = 1003,
                CardLast4="9999",
                LocationCode="STO-03",
                ProductName="Wireless keyboard",
                Amount=14.99m,
                TransactionTime=now.AddHours(-1)
            }
        };
        return Task.FromResult(transactions);
    }
}