using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using StargateAPI.Business.Data;

namespace StargateAPI.Tests
{
    internal static class DbContextFactory
    {
        public static StargateContext CreateInMemoryContext()
        {
            var connection = new SqliteConnection("DataSource=:memory:");
            connection.Open();

            var options = new DbContextOptionsBuilder<StargateContext>()
                .UseSqlite(connection)
                .Options;

            var context = new StargateContext(options);

            // Ensure database schema is created
            context.Database.EnsureCreated();

            return context;
        }
    }
}
