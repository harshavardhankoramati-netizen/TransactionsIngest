using TransactionsIngest.App.Models;
namespace TransactionsIngest.App.Services;
public interface ITransactionFetcher
{
    Task<List<Transaction>>FetchAsync();
}