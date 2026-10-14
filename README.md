# NTDLS.SqliteDapperWrapper

NTDLS.SqliteDapperWrapper is a lightweight C# wrapper around SQLite (via Microsoft.Data.Sqlite) and [Dapper](https://github.com/DapperLib/Dapper). It gives you a simple, connection-managed interface to a SQLite database while adding a few conveniences on top: executing SQL from embedded resource scripts, passing parameters, building temporary tables from in-memory value lists (a `STRING_SPLIT` replacement), attaching other databases for cross-database joins, and inspecting schema information.

📦 Be sure to check out the NuGet package: https://www.nuget.org/packages/NTDLS.SqliteDapperWrapper

## When to use this package

Consider using NTDLS.SqliteDapperWrapper when:

- **You want Dapper's ergonomics with SQLite without managing connections yourself.** Every call opens a connection, executes, then closes and disposes it — no leaked or forgotten connections.
- **You keep your SQL in `.sql` files** as embedded resources and would rather pass `"MyScript.sql"` than load and inline the text yourself.
- **You need to pass a list of values into a query** (e.g. "give me all rows whose id is in this list"). `CreateTempTableFrom` builds a session-scoped temp table from strings, integers, or objects so you can simply `JOIN` against it.
- **You have multiple SQLite database files** and want to join across them — `Attach` lets you reference another database file by alias inside a query.
- **You need quick schema introspection** — listing tables, indexes, column definitions, or checking whether a table/column/index exists.

## Usage examples

### Creating a factory

A `SqliteManagedFactory` holds a default connection string and manages opening/closing a connection for every operation. A bare file path is accepted and converted to a `Data Source=` connection string automatically.

```csharp
using NTDLS.SqliteDapperWrapper;

// A factory per database file. A plain path works too: new SqliteManagedFactory(".\\databaseFile.db");
var myConnection = new SqliteManagedFactory("Data Source=.\\databaseFile.db");
var myOtherDatabase = new SqliteManagedFactory("Data Source=.\\otherDatabase.db");
```

### Executing statements and queries

Each call below opens a connection, runs the statement, and closes/disposes the connection. The argument may be raw SQL text or the name of an embedded `.sql` resource.

```csharp
// Inline SQL text.
myConnection.Execute("DROP TABLE IF EXISTS Test");

// An embedded resource script ("*.sql" names are loaded automatically).
myConnection.Execute("CreateTestTable.sql");

// Parameterized insert — note that Guid values are supported out of the box.
for (int i = 0; i < 100; i++)
{
    var param = new
    {
        Name = $"Name #{i}",
        Guid = Guid.NewGuid(),
        Description = Guid.NewGuid().ToString()
    };

    myConnection.Execute(
        "INSERT INTO Test (Name, Guid, Description) VALUES (@Name, @Guid, @Description)", param);
}

// Queries return typed results straight from Dapper.
List<TestModel> results = myConnection.Query<TestModel>("SELECT * FROM Test");
```

### Running multi-step work on one connection with `Ephemeral`

`Ephemeral` opens a single connection, hands it to your lambda, and closes/disposes it afterwards — ideal for transactions or sequences of statements that should share one connection.

```csharp
myConnection.Ephemeral(o =>
{
    // Begin a transaction on the shared connection.
    using var tx = o.BeginTransaction();

    try
    {
        for (int i = 0; i < 100; i++)
        {
            var param = new
            {
                Name = $"Name #{i}",
                Guid = Guid.NewGuid(),
                Description = Guid.NewGuid().ToString()
            };

            o.Execute(
                "INSERT INTO Test (Name, Guid, Description) VALUES (@Name, @Guid, @Description)", param);
        }

        tx.Commit();
    }
    catch
    {
        tx.Rollback();
        throw;
    }
});
```

There is also a generic overload that returns a value, plus async variants (`EphemeralAsync`).

### Passing a list of values into a query with a temp table

`CreateTempTableFrom` creates a session-scoped temp table containing your values. This is a simple replacement for `STRING_SPLIT`: just `JOIN` against the temp table. The returned object drops the temp table when disposed.

```csharp
myConnection.Ephemeral(o =>
{
    List<int> ids = [10, 20, 30, 40, 50, 60, 70, 80];

    // Temp table "id_temp" gets a single Value column holding each id.
    // It is dropped automatically when the variable is disposed.
    using var id_temp = o.CreateTempTableFrom("id_temp", ids);

    // Now select only the rows whose Id is in our list.
    var results = o.Query<TestModel>(
        "SELECT * FROM Test INNER JOIN id_temp ON id_temp.Value = Test.Id");

    foreach (var result in results)
    {
        Console.WriteLine($"{result.Id} {result.Name} {result.Description}");
    }
});
```

Overloads exist for `IEnumerable<string>`, `IEnumerable<int>`, and `IEnumerable<T>` where the temp table columns mirror the properties of `T` (all stored as `TEXT`). You can also use the transaction-aware overload to create the temp table inside an existing transaction.

### Bulk-inserting objects via a temp table

Because the generic `CreateTempTableFrom<T>` mirrors your object's properties, you can stage a whole list of records and bulk-copy it with a single `INSERT ... SELECT`.

```csharp
// Build some records in memory.
var values = new List<TestParamModel>();
for (int i = 0; i < 100; i++)
{
    values.Add(new TestParamModel
    {
        Name = $"Name #{i}",
        Guid = Guid.NewGuid(),
        Description = Guid.NewGuid().ToString()
    });
}

myOtherDatabase.Ephemeral(o =>
{
    // Temp table "temp_values" gets one column per property of TestParamModel.
    using var temp_values = o.CreateTempTableFrom("temp_values", values);

    // Bulk copy from the temp table into the real table.
    o.Execute("INSERT INTO Test (Name, Guid, Description) SELECT Name, Guid, Description FROM temp_values");
});
```

### Joining across two database files with `Attach`

`Attach` attaches another database file (from the same directory as the current one) under an alias so you can reference its tables in queries. The returned object detaches the database when disposed.

```csharp
myConnection.Ephemeral(o =>
{
    // Attach ".\\otherDatabase.db" under the alias "MyOtherDatabase".
    // It is detached automatically when the variable is disposed.
    using var otherDatabase = o.Attach("otherDatabase.db", "MyOtherDatabase");

    // Cross-database join: Test lives in both databases.
    var results = o.Query<JoinedModel>(
        "SELECT A.Id, A.Name AS NameA, B.Name AS NameB " +
        "FROM Test AS A INNER JOIN MyOtherDatabase.Test AS B ON A.Id = B.Id");

    foreach (var result in results)
    {
        Console.WriteLine($"{result.Id} {result.NameA} {result.NameB}");
    }
});
```

An async variant, `AttachAsync`, is also available.

### Inspecting the schema

Both `SqliteManagedFactory` and `SqliteManagedInstance` expose helpers for schema introspection:

```csharp
// List all tables in the database.
List<TableInfo> tables = myConnection.GetTables();

// List indexes, either for one table or for the whole database.
List<IndexInfo> indexesForTable = myConnection.GetIndexes("Test");
List<IndexInfo> allIndexes = myConnection.GetIndexes();

// Column-level details for a table (name, type, NOT NULL, default, primary key).
List<TableSchemaInfo> schema = myConnection.GetTableSchema("Test");

// Quick existence checks.
bool hasTable = myConnection.DoesTableExist("Test");
bool hasColumn = myConnection.DoesColumnExist("Test", "Name");
bool hasIndex = myConnection.DoesIndexExist("Test", "IX_Test_Name");
```

## License

[MIT](https://choosealicense.com/licenses/mit/)
