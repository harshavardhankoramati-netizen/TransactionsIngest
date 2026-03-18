using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
namespace TransactionsIngest.App.Data;

public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var options=new DbContextOptionsBuilder<AppDbContext>().UseSqlite("Data Source=transactions.db").Options;
        return new AppDbContext(options);
    }
}