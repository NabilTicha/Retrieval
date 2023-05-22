using System;
using System.Data;
using System.Data.Entity.ModelConfiguration.Configuration;
using System.Data.SqlClient;
using System.Data.SQLite;
using System.IO;
using System.Runtime.InteropServices;
using System.Xml.Linq;
using System.Collections.Generic;

namespace ConsoleApp1
{
    internal class Program
    {
        const bool skipDBsetup = true;
        static SQLiteConnection autompgDatabaseConnection;
        static SQLiteConnection metadbConnection;
        static string
            autompgDatabaseFilepath = "../../../../autompg.db",
            autompgSourceFilepath = "../../../../autompg.sql",
            metadbFilepath = "../../../../metadb.db",
            workloadFilepath = "../../../../workload.txt";


        static Dictionary<string, int> attributeIndices;
        private static int Bin(int input, int histowidth)
        {
            return input / histowidth;
        }
        private static int Bin(float input, float histowidth)
        {
            return (int)(input / histowidth);
        }
        private static bool CheckProperAutompgInsertCommand(string input, out string output)
        {
            if (input.Length <= 3)
            {
                output = default(string);
                return false;
            }
            List<char> newstr = new List<char>();
            for (int i = 0; i < input.Length; i++)
                if (input[i] != '\n')
                    newstr.Add(input[i]);
            output = new string(newstr.ToArray());
            return true;
        }
        private static (int, float, int, float, float, float, float, int, int, string, string, string) AutompgInsertCommandValues(string autompgInsertCommand)
        {
            int p = 0, q;
            while (autompgInsertCommand[p++] != '(') ;
            // q stop op de plek van het haakje
            q = autompgInsertCommand.Length - 1;
            while (autompgInsertCommand[q] != ')') q--;
            char[] commandValueString = new char[q - p];
            for (int i = p; i < q; i++)
                commandValueString[i - p] = autompgInsertCommand[i];
            string[] values = new string(commandValueString).Split(',', StringSplitOptions.TrimEntries);
            return (
                int.Parse(values[0]),
                float.Parse(values[1]),
                int.Parse(values[2]),
                float.Parse(values[3]),
                float.Parse(values[4]),
                float.Parse(values[5]),
                float.Parse(values[6]),
                int.Parse(values[7]),
                int.Parse(values[8]),
                values[9],
                values[10],
                values[11]
                );
        }
        private static bool StrEq(string a, string b)
        {
            if (a.Length == b.Length)
            {
                for (int i = 0; i < a.Length; i++)
                    if (a[i] != b[i])
                        return false;
                return true;
            }
            return false;
        }
        private static WorkLoadQuery ReadWorkLoadQuery(string input)
        {
            WorkLoadQuery query = new WorkLoadQuery();
            string[] splitQuery = input.Split(' ', StringSplitOptions.TrimEntries);
            query.times = int.Parse(splitQuery[0]);
            int i;
            for (i = 0; !StrEq(splitQuery[i], "WHERE") ; i++) ;
            i++;
            
            return query;
        }
        
        private static WorkLoadQuery ReadWorkLoadQueryH(
            WorkLoadQuery workLoadQuery, // the query we are storing the read information in
            int attrindex, // the collumn this read operation is about
            string[] splitQuery, // the word of the query
            int indexInSplitQuery // where we are looking within the word of the query
            )
        {
            // first we need to establish whether we are dealing with a single value or
            // an IN clause
            if (StrEq(splitQuery[indexInSplitQuery], "=")) // a single value
            { // after this = sign there is just one value to be parsed
                workLoadQuery.selectedAttributes[attrindex] = true; // note that we selected on this attribute
                
            }
            else if (StrEq(splitQuery[indexInSplitQuery], "IN")) // an IN clause
            { // what now comes is a single word, that is a csv list of values between brackets

            }
            else throw new Exception();
        }
        static void Init()
        {
            attributeIndices = new Dictionary<string, int>();
            attributeIndices.Add("id", 0);
            attributeIndices.Add("mpg", 1);
            attributeIndices.Add("cylinders", 2);
            attributeIndices.Add("displacement", 3);
            attributeIndices.Add("horsepower", 4);
            attributeIndices.Add("weight", 5);
            attributeIndices.Add("acceleration", 6);
            attributeIndices.Add("model_year", 7);
            attributeIndices.Add("origin", 8);
            attributeIndices.Add("brand", 9);
            attributeIndices.Add("model", 10);
            attributeIndices.Add("type", 11);
        }
        static void Main(string[] args)
        {
            throw new Exception();
            string[] spl = "\'dasas\'".Split('\'');
            for (int i = 0; i < spl.Length; i++)
                Console.WriteLine(spl[i] + "\n");
            return;
            /*
            StreamReader workloadReader2 = new StreamReader(workloadFilepath);

            workloadReader2.ReadLine(); // the first two lines are not relevant
            workloadReader2.ReadLine();
            WorkLoadQuery q = ReadWorkLoadQuery(workloadReader2.ReadLine());
            Console.WriteLine("times: " + q.times);
           
            return;*/

            // for testing, i first delete all the files affected by a previous run of the program, this keeps things clean
            //File.Delete(autompgDatabaseFilepath);

            // now we create and fill the files (perhaps again)
            //File.Create(autompgDatabaseFilepath);
            if (!skipDBsetup)
            {
                if (!File.Exists(metadbFilepath))
                    File.Create(metadbFilepath);
                if (!File.Exists(autompgDatabaseFilepath))
                    File.Create(autompgDatabaseFilepath);
                File.WriteAllText(autompgDatabaseFilepath, String.Empty);
                File.WriteAllText(metadbFilepath, String.Empty);
            }
            
            autompgDatabaseConnection = new SQLiteConnection("Data Source=" + autompgDatabaseFilepath);
            metadbConnection = new SQLiteConnection("Data Source=" + metadbFilepath);
            // open the connection(s)
            autompgDatabaseConnection.Open();
            metadbConnection.Open();
            
            // connect with the sources required for filling autompg
            StreamReader autompgSourceStream = new StreamReader(autompgSourceFilepath);
            string autompgSourceContents = autompgSourceStream.ReadToEnd();
            string[] rawsqlCommands = autompgSourceContents.Split(';', StringSplitOptions.None);
            List<string> filteredsqlCommands = new List<string>();
            for (int i = 0; i < rawsqlCommands.Length; i++)
                if (CheckProperAutompgInsertCommand(rawsqlCommands[i], out string output))
                    filteredsqlCommands.Add(output);
            string[] sqlCommands = filteredsqlCommands.ToArray();
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


            // this will be used to calculate idf's 
            Map<int, int> autompg_mpg_occurences = new Map<int, int>();
            Map<int, int> autompg_cylinders_occurences = new Map<int, int>();
            Map<int, int> autompg_displacement_occurences = new Map<int, int>();
            Map<int, int> autompg_horsepower_occurences = new Map<int, int>();
            Map<int, int> autompg_weight_occurences = new Map<int, int>();
            Map<int, int> autompg_acceleration_occurences = new Map<int, int>();
            Map<int, int> autompg_model_year_occurences = new Map<int, int>();
            Map<int, int> autompg_origin_occurences = new Map<int, int>();
            Map<string, int> autompg_brand_occurences = new Map<string, int>();
            Map<string, int> autompg_model_occurences = new Map<string, int>();
            Map<string, int> autompg_type_occurences = new Map<string, int>();
            //Console.WriteLine(AutompgInsertCommandValues(sqlCommands[2]));
            Console.WriteLine("last sql command:\n" + sqlCommands[sqlCommands.Length - 1]);
            for (int i = 0; i < sqlCommands.Length; i++)
            {
                    

                if (i > 0)
                {
                    var tup = AutompgInsertCommandValues(sqlCommands[i]);
                    //Console.WriteLine("first command: \n" + sqlCommands[i]);
                    int i_ = i - 1;
                    // to store the tuple internally for our preprocessing
                    autompg_id[i_] = tup.Item1;
                    autompg_mpg[i_] = tup.Item2;
                    autompg_cylinders[i_] = tup.Item3;
                    autompg_displacement[i_] = tup.Item4;
                    autompg_horsepower[i_] = tup.Item5;
                    autompg_weight[i_] = tup.Item6;
                    autompg_acceleration[i_] = tup.Item7;
                    autompg_model_year[i_] = tup.Item8;
                    autompg_origin[i_] = tup.Item9;
                    autompg_brand[i_] = tup.Item10;
                    autompg_model[i_] = tup.Item11;
                    autompg_type[i_] = tup.Item12;
                    MapIncrement<int> numerical_occurences_incrementer = new MapIncrement<int>();
                    MapIncrement<string> text_occurences_incrementer = new MapIncrement<string>();
                    Console.WriteLine("incrementing mpg value " + tup.Item2);
                    numerical_occurences_incrementer.Increment(autompg_mpg_occurences, Bin(tup.Item2, autompg_mpg_histowidth));
                    numerical_occurences_incrementer.Increment(autompg_cylinders_occurences, Bin(tup.Item3, autompg_cylinders_histowidth));
                    numerical_occurences_incrementer.Increment(autompg_displacement_occurences, Bin(tup.Item4, autompg_displacement_histowidth));
                    numerical_occurences_incrementer.Increment(autompg_horsepower_occurences, Bin(tup.Item5, autompg_horsepower_histowidth));
                    numerical_occurences_incrementer.Increment(autompg_weight_occurences, Bin(tup.Item6, autompg_weight_histowidth));
                    numerical_occurences_incrementer.Increment(autompg_acceleration_occurences, Bin(tup.Item7, autompg_acceleration_histowidth));
                    numerical_occurences_incrementer.Increment(autompg_model_year_occurences, tup.Item8);
                    numerical_occurences_incrementer.Increment(autompg_origin_occurences, tup.Item9);
                    text_occurences_incrementer.Increment(autompg_brand_occurences, tup.Item10);
                    text_occurences_incrementer.Increment(autompg_model_occurences, tup.Item11);
                    text_occurences_incrementer.Increment(autompg_type_occurences, tup.Item12);
                    //Console.WriteLine("first value of mpg in first tuple: " + autompg_mpg[i_].ToString());
                }
                if (!skipDBsetup)
                {
                    SQLiteCommand autompgCommand = new SQLiteCommand(sqlCommands[i], autompgDatabaseConnection);
                    autompgCommand.ExecuteNonQuery();
                }
                   
                Console.WriteLine("at query " + i);
            }


            // calculating IDFs, with the formula idfk(v) = log(N/Fk(v)), with k being the collumn of interest and v a value in that collumn, and N the number of rows
            IDFcalculator<int> numericalIdfCalculator = new IDFcalculator<int>();
            IDFcalculator<string> textIdfCalculator = new IDFcalculator<string>();
            Map<int, float> autompg_mpg_idfs = numericalIdfCalculator.CalcIdfMap(autompg_mpg_occurences, n);
            Map<int, float> autompg_cylinders_idfs = numericalIdfCalculator.CalcIdfMap(autompg_cylinders_occurences, n);
            Map<int, float> autompg_displacement_idfs = numericalIdfCalculator.CalcIdfMap(autompg_displacement_occurences, n);
            Map<int, float> autompg_horsepower_idfs = numericalIdfCalculator.CalcIdfMap(autompg_horsepower_occurences, n);
            Map<int, float> autompg_weight_idfs = numericalIdfCalculator.CalcIdfMap(autompg_weight_occurences, n);
            Map<int, float> autompg_acceleration_idfs = numericalIdfCalculator.CalcIdfMap(autompg_acceleration_occurences, n);
            Map<int, float> autompg_model_year_idfs = numericalIdfCalculator.CalcIdfMap(autompg_model_year_occurences, n);
            Map<int, float> autompg_origin_idfs = numericalIdfCalculator.CalcIdfMap(autompg_origin_occurences, n);
            Map<string, float> autompg_brand_idfs = textIdfCalculator.CalcIdfMap(autompg_brand_occurences, n);
            Map<string, float> autompg_model_idfs = textIdfCalculator.CalcIdfMap(autompg_model_occurences, n);
            Map<string, float> autompg_type_idfs = textIdfCalculator.CalcIdfMap(autompg_type_occurences, n);
            // idf will be calculated for all of these lists individually

            /*
            Console.WriteLine("done with idf work, idf samples:");
            Console.WriteLine("numerical samples on mpg");
            Random random = new Random();
            for (int i = 0; i < 15; i++)
            {
                int randomindex = random.Next(autompg_mpg_idfs.tuples.Count);
                (int, float) tuple = autompg_mpg_idfs.tuples[randomindex];
                Console.WriteLine("histobin " + tuple.Item1 + ", which runs from " + (tuple.Item1 * 6).ToString() + " to " + ((tuple.Item1 + 1) * 6).ToString() + ", has idf " + tuple.Item2);
            }

            Console.WriteLine("text samples on brand");
            // we need the following, a table of IDFs and and a table of QFs, for numerical values we will create a 
            for (int i = 0; i < 15; i++)
            {
                int randomindex = random.Next(autompg_brand_idfs.tuples.Count);
                (string, float) tuple = autompg_brand_idfs.tuples[randomindex];
                Console.WriteLine("brand " + tuple.Item1 + " has idf " + tuple.Item2);
            }
            */

            // now that the idf's are calculated, it is time to calculate the QF's
            Map<int, int> autompg_mpg_rqf = new Map<int, int>();
            Map<int, int> autompg_cylinders_rqf = new Map<int, int>();
            Map<int, int> autompg_displacement_rqf = new Map<int, int>();
            Map<int, int> autompg_horsepower_rqf = new Map<int, int>();
            Map<int, int> autompg_weight_rqf = new Map<int, int>();
            Map<int, int> autompg_acceleration_rqf = new Map<int, int>();
            Map<int, int> autompg_model_year_rqf = new Map<int, int>();
            Map<int, int> autompg_origin_rqf = new Map<int, int>();
            Map<string, int> autompg_brand_rqf = new Map<string, int>();
            Map<string, int> autompg_model_rqf = new Map<string, int>();
            Map<string, int> autompg_type_rqf = new Map<string, int>();

            StreamReader workloadReader = new StreamReader(workloadFilepath);

            workloadReader.ReadLine(); // the first two lines are not relevant
            workloadReader.ReadLine();










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





    public enum autompg
    {
        id,
        mpg,
        cylinders,
        displacement,
        horsepower,
        weight,
        acceleration,
        model_year,
        origin,
        brand,
        model,
        type
    }
    public class WorkLoadQuery
    {
        public int times;
        public bool[] selectedAttributes;

        public WorkLoadQuery()
        {
            selectedAttributes = new bool[12];
        }
        
        public int[] autompg_id;
        public float[] autompg_mpg;// histogram bin width:6
        public int[] autompg_cylinders; // histo width:3
        public float[] autompg_displacement; // histo width:70
        public float[] autompg_horsepower;// histo width: 60
        public float[] autompg_weight;// histo width: 600
        public float[] autompg_acceleration;// histo width: 4
        public int[] autompg_model_year;// histo width: 1 (no histogram)
        public int[] autompg_origin;// histo width: 1
        public string[] autompg_brand;
        public string[] autompg_model;
        public string[] autompg_type;
        
        public WorkLoadQuery ThrowValuesIn(
            int attrindex,
            string[] toParse)
        {
            selectedAttributes[attrindex] = true;
            switch (attrindex)
            {
                case 0: // id, int
                    workLoadQuery.autompg_id = HelperFunctions.ParseInts(toParse);
                    break;
                case 1: // mpg
                    workLoadQuery.autompg_mpg = HelperFunctions.ParseFloats(toParse);
                    break;
                case 2: // cylinders
                    workLoadQuery.autompg_cylinders = HelperFunctions.ParseInts(toParse);
                    break;
                case 3: // displacement
                    workLoadQuery.autompg_displacement = HelperFunctions.ParseFloats(toParse);
                    break;
                case 4: // horsepower
                    workLoadQuery.autompg_horsepower = HelperFunctions.ParseFloats(toParse);
                    break;
                case 5: // weight
                    workLoadQuery.autompg_weight = HelperFunctions.ParseFloats(toParse);
                    break;
                case 6: // acceleration
                    workLoadQuery.autompg_acceleration = HelperFunctions.ParseFloats(toParse);
                    break;
                case 7: // model_year
                    workLoadQuery.autompg_model_year = HelperFunctions.ParseInts(toParse);
                    break;
                case 8: // origin
                    workLoadQuery.autompg_origin = HelperFunctions.ParseInts(toParse);
                    break;
                case 9: // brand
                    workLoadQuery.autompg_brand = toParse;
                    break;
                case 10: // model
                    workLoadQuery.autompg_model = toParse;
                    break;
                case 11: // type
                    workLoadQuery.autompg_type = toParse;
                    break;
            }
        }

    }

    public class MapIncrement<T>
    {
        public void Increment(Map<T,int> map, T item)
        {
            if (map.indices.TryGetValue(item, out int index))
            {
                map.tuples[index] = (map.tuples[index].Item1, map.tuples[index].Item2 + 1);
            }
            else
            {
                map.Add(item, 1);
            }
        }
    }

    public class IDFcalculator<T>
    {


        public Map<T, float> CalcIdfMap(Map<T, int> occurencesMap, int n)
        {
            Map<T, float> result = new Map<T, float>();
            for (int i = 0; i < occurencesMap.tuples.Count; i++)
            {
                T value = occurencesMap.tuples[i].Item1;
                float idf = (float)Math.Log10(((double)n) / occurencesMap.tuples[i].Item2);
                result.Add(value, idf);
            }
            return result;
        }
    }

    // zelfgemaakte datastructuur

    public class Map<Key, Value>
    {
        public List<(Key, Value)> tuples = new List<(Key, Value)>();
        public Dictionary<Key, int> indices = new Dictionary<Key, int>();
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