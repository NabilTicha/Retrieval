using System.Data.SQLite;

namespace ConsoleApp1
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello, World!");
            string connectionString = "Data Source=database.db";
            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                connection.Open();


                // Create the table
                using (SQLiteCommand command = new SQLiteCommand("CREATE TABLE IF NOT EXISTS MyTable (Id INTEGER PRIMARY KEY AUTOINCREMENT, Name TEXT)", connection))
                {
                    command.ExecuteNonQuery();
                }

                // Insert an item into the table
                using (SQLiteCommand command = new SQLiteCommand("INSERT INTO MyTable (Name) VALUES (@name)", connection))
                {
                    command.Parameters.AddWithValue("@name", "John Doe");
                    command.ExecuteNonQuery();
                }

                // Query for the item
                using (SQLiteCommand command = new SQLiteCommand("SELECT * FROM MyTable", connection))
                {
                    using (SQLiteDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            int id = reader.GetInt32(0);
                            string name = reader.GetString(1);

                            Console.WriteLine($"Id: {id}, Name: {name}");
                        }
                    }
                }

                connection.Close();
            }
        }
    }
}