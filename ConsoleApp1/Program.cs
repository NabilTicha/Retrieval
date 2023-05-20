using System;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Xml.Linq;

namespace ConsoleApp1
{
    internal class Program
    {
        static SQLiteConnection autompgDatabaseConnection;
        static SQLiteConnection metadbConnection;
        static string
            autompgDatabaseFilepath = "../../../../autompg.db",
            autompgSourceFilepath = "../../../../autompg.sql",
            metadbFilepath = "../../../../metadb.db";
            

        
        private static int Bin(int input, int histowidth)
        {
            return input / histowidth;
        }
        private static int Bin(float input, float histowidth)
        {
            return (int)(input / histowidth);
        }
        private static string CleanNewlines(string input)
        {
            List<char> newstr = new List<char>();
            for (int i = 0; i < input.Length; i++)
                if (input[i] != '\n')
                    newstr.Add(input[i]);
            return new string(newstr.ToArray());
        }


        static void Main(string[] args)
        {
            // for testing, i first delete all the files affected by a previous run of the program, this keeps things clean
            //File.Delete(autompgDatabaseFilepath);

            // now we create and fill the files (perhaps again)
            //File.Create(autompgDatabaseFilepath);
            if (!File.Exists(metadbFilepath))
                File.Create(metadbFilepath);
            if (!File.Exists(autompgDatabaseFilepath))
                File.Create(autompgDatabaseFilepath);
            File.WriteAllText(autompgDatabaseFilepath, String.Empty);
            File.WriteAllText(metadbFilepath, String.Empty);
            autompgDatabaseConnection = new SQLiteConnection("Data Source=" + autompgDatabaseFilepath);
            metadbConnection = new SQLiteConnection("Data Source=" + metadbFilepath);
            // open the connection(s)
            autompgDatabaseConnection.Open();
            metadbConnection.Open();
            
            // connect with the sources required for filling autompg
            StreamReader autompgSourceStream = new StreamReader(autompgSourceFilepath);
            string autompgSourceContents = autompgSourceStream.ReadToEnd();
            string[] sqlCommands = autompgSourceContents.Split(';', StringSplitOptions.None);
            for (int i = 0; i < sqlCommands.Length; i++)
                sqlCommands[i] = CleanNewlines(sqlCommands[i]);
            // autompg is now full, we can return the tuples if we know which ones we want, time to fill up metadb
            // storing the values of autompg internally for preprocessing
            int n = sqlCommands.Length - 1; // the -1 is because command 0 just sets up the table
            int[] autompg_id = new int[n];
            float[] autompg_mpg = new float[n]; float autompg_mpg_histowidth = 6;// histogram bin width:6
            int[] autompg_cylinders = new int[n]; int autompg_cylinders_histowidth = 3;// histo width:3
            float[] autompg_displacement = new float[n]; float autompg_displacement_histowidth = 70;// histo width:70
            float[] autompg_horsepower = new float[n]; float autompg_horsepower_histowidth = 60;// histo width: 60
            float[] autompg_weight = new float[n]; float autompg_weight_histowidth = 600;// histo width: 600
            float[] autompg_acceleration = new float[n]; float autompg_acceleration_histowidth = 4;// histo width: 4
            int[] autompg_model_year = new int[n];// histo width: 1 (no histogram)
            int[] autompg_origin = new int[n];// histo width: 1
            string[] autompg_brand = new string[n];
            string[] autompg_model = new string[n];
            string[] autompg_type = new string[n];

            for (int i = 0; i < sqlCommands.Length; i++)
            {
                SQLiteCommand autompgCommand = new SQLiteCommand(sqlCommands[i], autompgDatabaseConnection);

                if (i > 0 && i == 1)
                {
                    Console.WriteLine("first command: \n" + sqlCommands[i]);
                    int i_ = i - 1;
                    // to store the tuple internally for our preprocessing
                    autompg_id[i_] = i;
                    autompg_mpg[i_] = (float)autompgCommand.Parameters["@p10"].Value;
                    Console.WriteLine("first value of mpg in first tuple: " + autompg_mpg[i_].ToString());
                }
                autompgCommand.ExecuteNonQuery();
            }


            // calculating IDFs
            for (int i = 0; i < n; i++)
            {

            }

            

            // we need the following, a table of IDFs and and a table of QFs, for numerical values we will create a 
            
















            SQLiteCommand command;
            SQLiteDataReader reader;

            return;
            while (true)
            {
                reader = (command = new SQLiteCommand(Console.ReadLine(), autompgDatabaseConnection)).ExecuteReader();
                    
                // Get the column names
                var columns = new string[reader.FieldCount];
                for (int i = 0; i < reader.FieldCount; i++)
                    columns[i] = reader.GetName(i);   

                // Display the column names
                Console.WriteLine(string.Join(", ", columns));

                if (reader.HasRows)
                {
                    // Concatenate the result into a single string
                    string result = string.Empty;

                    while (reader.Read())
                    {
                        for (int i = 0; i < reader.FieldCount; i++)
                            result += reader[i] + "\t";       

                        result += Environment.NewLine;
                    }

                    // Print the result
                    Console.WriteLine(result);
                }
                else Console.WriteLine("No rows found.");

                    
                

                // Query for the item

                autompgDatabaseConnection.Close();
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

            // Create the table
            Console.WriteLine("done reading db");

            string query = "SELECT * FROM autompg";
            SQLiteCommand command = new SQLiteCommand(query, autompgDatabaseConnection);

            // Execute the query and obtain a data reader
            SQLiteDataReader reader = command.ExecuteReader();

            // Alles inlezen en daarna gelijk van alles IDF berekenen
            // Stap 1: alles inlezen in een of ander datastructuur
            while (reader.Read())
            {
                

            }
            // Stap 2: van alles de idf berekenen


            // stap 3: deze idf's uploaden
            
            

            //Load in workload here
            //Alles inlezen en dan die andere dingen van de workload termen berekenen
        }
    }








    // zelfgemaakte datastructuur

    class Map<Key, Value>
    {
        public List<(Key, Value)> tuples = new List<(Key, Value)>();
        Dictionary<Key, int> indices = new Dictionary<Key, int>();
        public void Add(Key key, Value value)
        {
            indices.Add(key, tuples.Count);
            tuples.Add((key, value));
            
        }
        public bool TryGetValue(Key key, out Value value)
        {
            if (indices.TryGetValue(key, out int index))
            {
                value = tuples[index].Item2;
                return true;
            }
            value = default(Value);
            return false;
            
        }
    }
}