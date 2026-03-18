using Microsoft.EntityFrameworkCore;
using TransactionsIngest.App.Data;
namespace TransactionsIngest.Tests.Helpers;

public static class TestDbHelper
{
    public static AppDbContext CreateInMemoryDb()
    {
        var options=new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;
        var db=new AppDbContext(options);
        db.Database.OpenConnection();
        db.Database.EnsureCreated();
        return db;
    }
}