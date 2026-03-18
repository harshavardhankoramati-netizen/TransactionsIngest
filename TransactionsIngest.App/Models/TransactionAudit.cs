namespace TransactionsIngest.App.Models;

public class TransactionAudit
{
    public int Id { get; set; }
    public int TransactionId { get; set; }
    public string ChangeType { get; set; } = string.Empty;
    public string? FieldName { get; set; }
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
}