using Microsoft.EntityFrameworkCore;
using TransactionsIngest.App.Models;
namespace TransactionsIngest.App.Data;

public class AppDbContext:DbContext
{
    public DbSet<Transaction> Transactions{get; set;}
    public DbSet<TransactionAudit> TransactionAudits{get; set;}
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options){}
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Transaction>(entity =>{
            entity.HasKey(t=>t.TransactionId);
            entity.Property(t=>t.CardLast4).HasMaxLength(4);
            entity.Property(t=>t.LocationCode).HasMaxLength(20);
            entity.Property(t=>t.ProductName).HasMaxLength(20);
            entity.Property(t=>t.Amount).HasColumnType("decimal(18,2)");
            entity.Property(t=>t.Status).HasMaxLength(20);
        });
        modelBuilder.Entity<TransactionAudit>(entity =>{
            entity.HasKey(a=>a.Id);
            entity.Property(a=>a.ChangeType).HasMaxLength(20);
            entity.Property(a=>a.FieldName).HasMaxLength(50);
        });
    }
}