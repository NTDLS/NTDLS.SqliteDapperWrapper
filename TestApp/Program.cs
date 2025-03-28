using NTDLS.SqliteDapperWrapper;

namespace TestApp
{
    internal class Program
    {
        public static SqliteManagedFactory MyConnection { get; set; } = new("Data Source=.\\databaseFile.db");
        public static SqliteManagedFactory MyOtherDatabase { get; set; } = new("Data Source=.\\otherDatabase.db");

        static void Main()
        {
            //Each time a statement/query is executed, the NTDLS.SqliteDapperWrapper will
            //  open a connection, execute then close & dispose the connection. 

            MyConnection.Execute("DROP TABLE IF EXISTS Test");
            MyOtherDatabase.Execute("DROP TABLE IF EXISTS Test");

            //Creates a table in two different databases from a script that is an embedded resource in the project.
            MyConnection.Execute("CreateTestTable.sql");
            MyOtherDatabase.Execute("CreateTestTable.sql");

            //Deletes the data from the table "Test".
            MyConnection.Execute("DELETE FROM Test");
            MyOtherDatabase.Execute("DELETE FROM Test");

            //Insert some records using an inline statement and parameters.
            for (int i = 0; i < 100; i++)
            {
                var param = new
                {
                    Name = $"Name #{i}",
                    Guid = Guid.NewGuid(),
                    Description = Guid.NewGuid().ToString()
                };

                MyConnection.Execute("INSERT INTO Test (Name, Guid, Description) VALUES (@Name, @Guid, @Description)", param);
            }

            //We can use "Ephemeral" to perform multiple steps on the same connection, such as here where we
            //  begin a transaction, insert data and then optionally commit or rollback the transaction.
            //  The connection is closed and disposed after Ephemeral() executes.
            MyConnection.Ephemeral(o =>
            {
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

                        o.Execute("INSERT INTO Test (Name, Guid, Description) VALUES (@Name, @Guid, @Description)", param);
                    }

                    tx.Commit();
                }
                catch
                {
                    tx.Rollback();
                    throw;
                }
            });

            MyConnection.Ephemeral(o =>
            {
                List<int> ids = [10, 20, 30, 40, 50, 60, 70, 80];

                //By calling CreateTempTableFrom, we can create a temp table with the given name that
                //  contains the given values. This allows us to pass ranges of values to a script.
                //  The temp table is automatically dropped them the variable is disposed.
                using var id_temp = o.CreateTempTableFrom("id_temp", ids);

                //Here we are going to get the values with the id of 10, 20, 30, etc.
                var results = o.Query<TestModel>("SELECT * FROM Test INNER JOIN id_temp ON id_temp.Value = Test.Id");

                //Print the results.
                foreach (var result in results)
                {
                    Console.WriteLine($"{result.Id} {result.Guid} {result.Name} {result.Description}");
                }
            });

            //Make some dummy records so we can pass them as a temp table.
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

            //Insert some data into the "Other Database".
            MyOtherDatabase.Ephemeral(o =>
            {
                //By calling CreateTempTableFrom, we can create a temp table with the given name that
                //  contains the given values from a list of anonymous or well defined objects.
                //  This allows us to pass ranges of values to a script. The temp table is automatically
                //  dropped them the variable is disposed.
                using var temp_values = o.CreateTempTableFrom("temp_values", values);

                //Here we are going to insert the data from the temp table called "temp_values".
                o.Execute("INSERT INTO Test (Name, Guid, Description) SELECT Name, Guid, Description FROM temp_values");
            });


            MyConnection.Ephemeral(o =>
            {
                //We can "attach" another database and access data from it like they are both attached.
                //The database is automatically "unattached" when the variable is disposed.
                using var otherDatabase = o.Attach("otherDatabase.db", "MyOtherDatabase");

                //Here we are going join to another database and select some values.
                var results = o.Query<JoinedModel>
                        ("SELECT A.Id, a.Name as NameA, b.Name as NameB FROM Test as A INNER JOIN MyOtherDatabase.Test as B ON A.Id = B.Id");

                //Print the results.
                foreach (var result in results)
                {
                    Console.WriteLine($"{result.Id} {result.NameA} {result.NameB}");
                }
            });
        }
    }
}
