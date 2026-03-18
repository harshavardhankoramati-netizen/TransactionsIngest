namespace TransactionsIngest.App.Models;

public class Transaction
{
    public int TransactionId { get; set; }
    public string CardLast4 { get; set; } = string.Empty;
    public string LocationCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime TransactionTime { get; set; }
    public string Status { get; set; } = "Active";
    public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;
}