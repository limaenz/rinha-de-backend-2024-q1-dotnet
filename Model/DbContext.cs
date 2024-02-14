using System.Data;
using System.Data.SQLite;

namespace rinha_de_backend_2024_q1_dotnet.Model;

public class DbContext
{
    private readonly string connectionString;

    public DbContext(string dbFilePath)
        => connectionString = dbFilePath;

    public IDbConnection GetConnection()
    {
        return new SQLiteConnection(connectionString);
    }
}