using System.ComponentModel;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol.Server;
using MyProject.Infrastructure.Persistence;

namespace MyProject.WebApi.Mcp;

/// <summary>
/// MCP tools for querying the database schema and executing read-only SQL.
/// </summary>
[McpServerToolType]
internal static class DatabaseTools
{
    private const int MaxRows = 100;

    private static readonly string[] ForbiddenKeywords =
        ["INSERT", "UPDATE", "DELETE", "DROP", "ALTER", "CREATE", "TRUNCATE", "EXEC", "EXECUTE", "GRANT", "REVOKE"];

    /// <summary>
    /// Executes a read-only SQL query against the database and returns the results as a JSON array.
    /// Only SELECT statements are allowed. Limited to 100 rows.
    /// </summary>
    [McpServerTool(Name = "query-database"), Description("Execute a read-only SQL query. Only SELECT statements are allowed. Returns up to 100 rows as JSON.")]
    public static async Task<string> QueryDatabase(
        MyProjectDbContext dbContext,
        [Description("The SQL query to execute. Must be a SELECT statement.")] string sql,
        CancellationToken cancellationToken)
    {
        var normalized = sql.Trim().TrimEnd(';');
        var upper = normalized.ToUpperInvariant();

        if (!upper.StartsWith("SELECT"))
        {
            return JsonSerializer.Serialize(new { error = "Only SELECT queries are allowed." });
        }

        foreach (var keyword in ForbiddenKeywords)
        {
            if (upper.Contains(keyword))
            {
                return JsonSerializer.Serialize(new { error = $"Forbidden keyword detected: {keyword}. Only SELECT queries are allowed." });
            }
        }

        var connection = dbContext.Database.GetDbConnection();
        await connection.OpenAsync(cancellationToken);

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = $"{normalized} LIMIT {MaxRows}";

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            var results = new List<Dictionary<string, object?>>();

            while (await reader.ReadAsync(cancellationToken))
            {
                var row = new Dictionary<string, object?>();
                for (var i = 0; i < reader.FieldCount; i++)
                {
                    row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                }

                results.Add(row);
            }

            return JsonSerializer.Serialize(new { rowCount = results.Count, rows = results }, JsonSerializerOptions.Web);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Returns the database schema as derived from the EF Core model - tables, columns, types, nullability, and keys.
    /// </summary>
    [McpServerTool(Name = "get-schema"), Description("Get the database schema - tables, columns, types, nullability, and keys.")]
    public static string GetSchema(MyProjectDbContext dbContext)
    {
        var entityTypes = dbContext.Model.GetEntityTypes();

        var schema = entityTypes.Select(et =>
        {
            var tableName = et.GetTableName();
            var schemaName = et.GetSchema();
            var primaryKey = et.FindPrimaryKey();

            return new
            {
                table = tableName,
                schema = schemaName ?? "public",
                columns = et.GetProperties().Select(p => new
                {
                    name = p.GetColumnName(),
                    type = p.GetColumnType(),
                    nullable = p.IsNullable,
                    isPrimaryKey = primaryKey?.Properties.Contains(p) == true
                }),
                foreignKeys = et.GetForeignKeys().Select(fk => new
                {
                    columns = fk.Properties.Select(p => p.GetColumnName()),
                    principalTable = fk.PrincipalEntityType.GetTableName(),
                    principalColumns = fk.PrincipalKey.Properties.Select(p => p.GetColumnName())
                })
            };
        });

        return JsonSerializer.Serialize(schema, JsonSerializerOptions.Web);
    }
}
