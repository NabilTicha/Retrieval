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
    }
}