using System;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Xml.Linq;

namespace ConsoleApp1
{
    internal class Program
    {
        
        static void Main(string[] args)
        {
            Console.WriteLine("Hello, World!");
            string connectionString = "Data Source=autompg.db";
            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                connection.Open();

                // Create the table
                Console.WriteLine("done reading db");

                while (true)
                {
                    using (SQLiteCommand command = new SQLiteCommand(Console.ReadLine(), connection))
                    {
                        using (SQLiteDataReader reader = command.ExecuteReader())
                        {
                            // Get the column names
                            var columns = new string[reader.FieldCount];
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                columns[i] = reader.GetName(i);
                            }

                            // Display the column names
                            Console.WriteLine(string.Join(", ", columns));

                            if (reader.HasRows)
                            {
                                // Concatenate the result into a single string
                                string result = string.Empty;

                                while (reader.Read())
                                {
                                    for (int i = 0; i < reader.FieldCount; i++)
                                    {
                                        result += reader[i] + "\t";
                                    }

                                    result += Environment.NewLine;
                                }

                                // Print the result
                                Console.WriteLine(result);
                            }
                            else
                            {
                                Console.WriteLine("No rows found.");
                            }
                        }
                    }
                }
                // Query for the item

                connection.Close();
            }

            static string FirstN(char[] chars, int n)
            {
                char[] result = new char[n];
                for (int i = 0; i < n; i++)
                    result[i] = chars[i];
                return new string(result);
            }
        }

        static void createMetaDB()
        {
            string connectionString = "Data Source=autompg.db";
            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                connection.Open();

                // Create the table
                Console.WriteLine("done reading db");

                string query = "SELECT * FROM autompg";
                using (SQLiteCommand command = new SQLiteCommand(query, connection))
                {
                    // Execute the query and obtain a data reader
                    using (SQLiteDataReader reader = command.ExecuteReader())
                    {
                        // Read the rows returned by the query
                        while (reader.Read())
                        {
                            // Access the column values for each row
                            //int id = reader.GetInt32(0);  // Assuming the first column is an integer (change the index if needed)
                            //string name = reader.GetString(1);  // Assuming the second column is a string (change the index if needed)

                            // Use the retrieved values as needed

                            // Alles inlezen en daarna gelijk van alles IDF berekenen
                        }
                    }
                }
            }

            //Load in workload here
            //Alles inlezen en dan die andere dingen van de workload termen berekenen
        }
    }
}