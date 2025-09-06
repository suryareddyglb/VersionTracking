using System.Collections.Generic;
using System.Threading.Tasks;
using Npgsql;

public class PostgresInserter
{
    private readonly string _connectionString;

    public PostgresInserter(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task InsertDependenciesAsync(
    IEnumerable<(string Repo, PackageJson? Package)> collection,
    HashSet<string> filterKeys)
    {
        using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();
        int count = 0;

        foreach (var (repo, package) in collection)
        {
            count++;
            if (package?.Dependencies != null)
            {
                string appname = repo;

                string version1 = package.Dependencies.TryGetValue("@angular/compiler", out var v1) ? v1.TrimStart('^', '~') : null;
                string version2 = package.Dependencies.TryGetValue("@angular/core", out var v2) ? v2.TrimStart('^', '~') : null;
                string version3 = package.Dependencies.TryGetValue("@angular/forms", out var v3) ? v3.TrimStart('^', '~') : null;

                string sql = @"
                INSERT INTO version_tracking (lob, appname, packagename, version1, version2, version3)
                VALUES (@lob, @appname, @packagename, @version1, @version2, @version3)
                ON CONFLICT (lob, appname, packagename) DO NOTHING;
            ";

                using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("lob", $"lob{count}");
                cmd.Parameters.AddWithValue("appname", appname);
                cmd.Parameters.AddWithValue("packagename", "angular");
                cmd.Parameters.AddWithValue("version1", version1 ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("version2", version2 ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("version3", version3 ?? (object)DBNull.Value);

                await cmd.ExecuteNonQueryAsync();
            }
        }
    }
}