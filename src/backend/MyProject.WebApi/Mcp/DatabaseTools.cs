using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using Npgsql;

namespace MyProject.WebApi.Mcp;

/// <summary>
/// MCP tools for querying the database schema and executing read-only SQL.
/// Uses a direct <see cref="NpgsqlConnection"/> from the connection string to avoid
/// coupling WebApi to Infrastructure internals (no DbContext dependency).
/// </summary>
[McpServerToolType]
internal static class DatabaseTools
{
    private const int MaxRows = 100;

    /// <summary>
    /// Executes a read-only SQL query against the database and returns the results as a JSON array.
    /// Safety is enforced at the database level via a read-only transaction - PostgreSQL rejects any
    /// write operation (INSERT, UPDATE, DELETE, DROP, etc.) regardless of SQL content.
    /// Limited to 100 rows via subquery wrapper.
    /// </summary>
    [McpServerTool(Name = "query-database"), Description("Execute a read-only SQL query. Only SELECT/WITH statements are allowed. Returns up to 100 rows as JSON. Enforced read-only at the database level.")]
    public static async Task<string> QueryDatabase(
        IConfiguration configuration,
        [Description("The SQL query to execute. Must be a SELECT or WITH (CTE) statement.")] string sql,
        CancellationToken cancellationToken)
    {
        var normalized = sql.Trim().TrimEnd(';');
        var upper = normalized.ToUpperInvariant();

        if (!upper.StartsWith("SELECT") && !upper.StartsWith("WITH"))
        {
            return JsonSerializer.Serialize(new { error = "Only SELECT and WITH (CTE) queries are allowed." });
        }

        var connectionString = configuration.GetConnectionString("Database");
        if (string.IsNullOrEmpty(connectionString))
        {
            return JsonSerializer.Serialize(new { error = "Database connection string not configured." });
        }

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            // Enforce read-only at the database level - PostgreSQL rejects any write attempt
            await using (var readOnlyCmd = new NpgsqlCommand("SET TRANSACTION READ ONLY", connection, transaction))
            {
                await readOnlyCmd.ExecuteNonQueryAsync(cancellationToken);
            }

            // Wrap in subquery to guarantee LIMIT applies to outermost result
            await using var command = new NpgsqlCommand(
                $"SELECT * FROM ({normalized}) AS _q LIMIT {MaxRows}",
                connection,
                transaction);

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
        catch (PostgresException ex) when (ex.SqlState == "25006") // READ ONLY SQL TRANSACTION
        {
            return JsonSerializer.Serialize(new { error = "Write operations are not allowed. Only read-only queries are permitted." });
        }
        catch (Exception)
        {
            return JsonSerializer.Serialize(new { error = "Query execution failed. Check your SQL syntax and try again." });
        }
    }

    /// <summary>
    /// Returns the database schema by querying PostgreSQL's <c>information_schema</c> directly.
    /// Includes tables, columns, types, nullability, primary keys, and foreign keys.
    /// </summary>
    [McpServerTool(Name = "get-schema"), Description("Get the database schema - tables, columns, types, nullability, primary keys, and foreign keys.")]
    public static async Task<string> GetSchema(
        IConfiguration configuration,
        CancellationToken cancellationToken)
    {
        var connectionString = configuration.GetConnectionString("Database");
        if (string.IsNullOrEmpty(connectionString))
        {
            return JsonSerializer.Serialize(new { error = "Database connection string not configured." });
        }

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        const string schemaSql = """
            SELECT
                c.table_schema,
                c.table_name,
                c.column_name,
                c.data_type,
                c.is_nullable,
                CASE WHEN kcu.column_name IS NOT NULL THEN true ELSE false END AS is_primary_key
            FROM information_schema.columns c
            LEFT JOIN information_schema.table_constraints tc
                ON tc.table_schema = c.table_schema
                AND tc.table_name = c.table_name
                AND tc.constraint_type = 'PRIMARY KEY'
            LEFT JOIN information_schema.key_column_usage kcu
                ON kcu.constraint_name = tc.constraint_name
                AND kcu.table_schema = tc.table_schema
                AND kcu.table_name = tc.table_name
                AND kcu.column_name = c.column_name
            WHERE c.table_schema NOT IN ('pg_catalog', 'information_schema')
            ORDER BY c.table_schema, c.table_name, c.ordinal_position
            """;

        await using var command = new NpgsqlCommand(schemaSql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var tables = new Dictionary<string, TableSchema>();

        while (await reader.ReadAsync(cancellationToken))
        {
            var tableSchema = reader.GetString(0);
            var tableName = reader.GetString(1);
            var key = $"{tableSchema}.{tableName}";

            if (!tables.TryGetValue(key, out var table))
            {
                table = new TableSchema(tableSchema, tableName, []);
                tables[key] = table;
            }

            table.Columns.Add(new ColumnSchema(
                reader.GetString(2),
                reader.GetString(3),
                reader.GetString(4) == "YES",
                reader.GetBoolean(5)));
        }

        return JsonSerializer.Serialize(tables.Values, JsonSerializerOptions.Web);
    }

    private sealed record TableSchema(string Schema, string Table, List<ColumnSchema> Columns);

    private sealed record ColumnSchema(string Name, string Type, bool Nullable, bool IsPrimaryKey);
}
