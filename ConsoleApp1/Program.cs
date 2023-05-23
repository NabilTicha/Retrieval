using System.Data.SQLite;
using System.Globalization;
using System.Web;

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

        const float autompg_mpg_histowidth = 6;// histogram bin width:6
        const int autompg_cylinders_histowidth = 3;// histo width:3
        const float autompg_displacement_histowidth = 70;// histo width:70
        const float autompg_horsepower_histowidth = 60;// histo width: 60
        const float autompg_weight_histowidth = 600;// histo width: 600
        const float autompg_acceleration_histowidth = 4;
        static MapIncrement<float> floatIncrementer = new MapIncrement<float>();
        static MapIncrement<int> intIncrementer = new MapIncrement<int>();
        static MapIncrement<string> stringIncrementer = new MapIncrement<string>();
        static MapIncrement<(string,string)> stringTupleIncrementer = new MapIncrement<(string,string)>();


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
            if (input.Length <= 8)
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
                float.Parse(values[1], CultureInfo.InvariantCulture),
                int.Parse(values[2]),
                float.Parse(values[3], CultureInfo.InvariantCulture),
                float.Parse(values[4], CultureInfo.InvariantCulture),
                float.Parse(values[5], CultureInfo.InvariantCulture),
                float.Parse(values[6], CultureInfo.InvariantCulture),
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

        private static string[] ReattachStrings(string[] splitQuery)
        {
            List<String> result = new List<String>();

            for(int i = 0; i < splitQuery.Length; i++)
            {
                if(splitQuery[i][0] != '\'')
                {
                    result.Add(splitQuery[i]);
                }

                else
                {
                    string temp = "";
                    while (splitQuery[i][splitQuery[i].Length - 1] != '\'')
                    {
                        temp += (splitQuery[i] + " ");
                        i++;
                    }

                    temp += splitQuery[i];
                    result.Add(temp);
                }
            }

            return result.ToArray();
        }
        private static WorkLoadQuery ReadWorkLoadQuery(string input)
        {
            WorkLoadQuery query = new WorkLoadQuery();
            string[] splitQuery0 = input.Split(' ', StringSplitOptions.TrimEntries);
            string[] splitQuery = ReattachStrings(splitQuery0);
            query.times = int.Parse(splitQuery[0]);
            int i;
            for (i = 0; !StrEq(splitQuery[i], "WHERE") ; i++) ;
            i++;
            // at this i, the queries start, each parameter is either an IN clause or an = clause
            // ReadWorkLoadQueryH reads a single part of a query, each part is separated by an AND
            ReadWorkLoadQueryH(query, attributeIndices[splitQuery[i]], splitQuery, i);
            // here we process all of the potential ANDs
            for (; i < splitQuery.Length; i++)
                if (StrEq(splitQuery[i], "AND"))
                    ReadWorkLoadQueryH(query, attributeIndices[splitQuery[i+1]], splitQuery, i+1);
            return query;
        }
        
        private static void ReadWorkLoadQueryH(
            WorkLoadQuery workLoadQuery, // the query we are storing the read information in
            int attrindex, // the collumn this read operation is about
            string[] splitQuery, // the word of the query
            int indexInSplitQuery // where we are looking within the word of the query
            )
        {
            // first we need to establish whether we are dealing with a single value or
            // an IN clause
            if (StrEq(splitQuery[indexInSplitQuery + 1], "=")) // a single value
            { // after this = sign there is just one value to be parsed
                string[] value = splitQuery[indexInSplitQuery +2].Split('\'',StringSplitOptions.RemoveEmptyEntries);
                workLoadQuery.ThrowValuesIn(attrindex, value);
            }
            else if (StrEq(splitQuery[indexInSplitQuery + 1], "IN")) // an IN clause
            { // what now comes is a single word, that is a csv list of values between brackets
                string inClause = "";
                int i = indexInSplitQuery + 2;
                inClause += splitQuery[i];
                while (splitQuery[i][splitQuery[i].Length - 1] != ')') // while we dont encounter the word with the closing bracket
                {
                    inClause += " " + splitQuery[i + 1]; // add the strings to the in clause statement, with the lost space back
                    i++;
                }
                string[] inputs = HelperFunctions.ExtractFromINclause(inClause);
                workLoadQuery.ThrowValuesIn(attrindex, inputs);
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
            string[] indexAttributes_ = new string[12]
            {
                "id",
                "mpg",
                "cylinders",
                "displacement",
                "horsepower",
                "weight",
                "acceleration",
                "model_year",
                "origin",
                "brand",
                "model",
                "type"
            };
            indexAttributes = indexAttributes_;
        }
        static string[] indexAttributes;
        private static void DisplayQuery(WorkLoadQuery w)
        {
            Console.Write("times: " + w.times);
            for (int i = 0; i < w.selectedAttributes.Length; i++)
                if (w.selectedAttributes[i])
                {
                    Console.Write( "," + indexAttributes[i] + ":");
                    w.DisplayStringValue(i);
                }
            Console.WriteLine("\n");
        }
        static void Main(string[] args)
        {
            Init();
            /*
            string test = "(\'hello\',\'im\',\'bob\')";
            Console.WriteLine(test);
            string[] spl = HelperFunctions.ExtractFromINclause(test);
            for (int i = 0; i < spl.Length; i++)
                Console.WriteLine(spl[i]);
            string[] value = "\'hdos\'".Split('\'', StringSplitOptions.RemoveEmptyEntries);
            Console.WriteLine("values: "+ value.Length);
            for(int i = 0; i < value.Length; i++)
                Console.WriteLine(value[i]);*/

            //HelperFunctions.DisplayStrings(HelperFunctions.ExtractFromINclause("(\'ddf fd\',\'gtgd\')"));


            
            // nu nemen we een willekeurige query en lezen we hem in 
            //Console.WriteLine("Query ++++++++++++++++++++++\n" + "7 times: SELECT * FROM autompg WHERE displacement = \'75\' AND model_year = \'74\' AND type IN (\'station wagon\',\'pickup\')");
            //Console.WriteLine("What was read -------------------------");
            //WorkLoadQuery ww = ReadWorkLoadQuery("7 times: SELECT * FROM autompg WHERE displacement = \'75\' AND model_year = \'74\' AND type IN (\'station wagon\',\'pickup\')");
            //DisplayQuery(ww);
            //string testQuery = "68 times: SELECT * FROM autompg WHERE mpg = \'15\' AND displacement = \'107\' AND horsepower = \'120\' AND brand = \'bmw\' AND type = \'station wagon\'";
            //Console.WriteLine("Query ++++++++++++++++++++++\n" + testQuery);
            //Console.WriteLine("What was read -------------------------");
            //WorkLoadQuery www = ReadWorkLoadQuery(testQuery);
            //DisplayQuery(www);
            //Random random = new Random();

            //while (true)
            //{
            //    int rindex = random.Next(0, queries.Count);
            //    Console.WriteLine("Query ++++++++++++++++++++++\n" + queries[rindex]);
            //    Console.WriteLine("What was read -------------------------");
            //    WorkLoadQuery w = ReadWorkLoadQuery(queries[rindex]);
            //    DisplayQuery(w);
            //    Thread.Sleep(4000);
            //}


            //string WLQuery = "124 times: SELECT * FROM autompg WHERE model_year = '82' AND type = 'sedan'";


            //SQLiteConnection connection = new SQLiteConnection();
            //SQLiteCommand WLCommand = new SQLiteCommand(WLQuery, connection);



            //using (SQLiteDataReader dataReader = WLCommand.ExecuteReader())
            //{
            //    while(dataReader.Read())
            //    {
            //        int id = dataReader.GetInt32(dataReader.GetOrdinal("id"));
            //        Console.WriteLine(id);
            //    }
            //}
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
            float[] autompg_mpg = new float[n];// histogram bin width:6
            int[] autompg_cylinders = new int[n];// histo width:3
            float[] autompg_displacement = new float[n];// histo width:70
            float[] autompg_horsepower = new float[n]; // histo width: 60
            float[] autompg_weight = new float[n]; // histo width: 600
            float[] autompg_acceleration = new float[n]; // histo width: 4
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
            Map<int, int> autompg_mpg_rqf = new Map<int, int>();//
            Map<int, int> autompg_cylinders_rqf = new Map<int, int>();//
            Map<int, int> autompg_displacement_rqf = new Map<int, int>();//
            Map<int, int> autompg_horsepower_rqf = new Map<int, int>();//
            Map<int, int> autompg_weight_rqf = new Map<int, int>();//
            Map<int, int> autompg_acceleration_rqf = new Map<int, int>();//
            Map<int, int> autompg_model_year_rqf = new Map<int, int>();
            Map<int, int> autompg_origin_rqf = new Map<int, int>();
            Map<string, int> autompg_brand_rqf = new Map<string, int>();
            Map<string, int> autompg_model_rqf = new Map<string, int>();
            Map<string, int> autompg_type_rqf = new Map<string, int>();


            StreamReader sr = new StreamReader(workloadFilepath);
            sr.ReadLine();
            sr.ReadLine();
            List<WorkLoadQuery> queries = new List<WorkLoadQuery>();
            string query;
            while ((query = sr.ReadLine()).Length > 3)
            {
                queries.Add(ReadWorkLoadQuery(query));
            }

            Map<string, int> autompg_brand_in_clause_occurences = new Map<string, int>();
            Map<string, int> autompg_model_in_clause_occurences = new Map<string, int>();
            Map<string, int> autompg_type_in_clause_occurences = new Map<string, int>();

            Map<(string, string), int> autompg_brand_in_clause_intersections = new Map<(string, string), int>();
            Map<(string, string), int> autompg_model_in_clause_intersections = new Map<(string, string), int>();
            Map<(string, string), int> autompg_type_in_clause_intersections = new Map<(string, string), int>();

            for (int i = 0; i < queries.Count; i++) //Calculate the RQFs of attribute values and put them in the correct maps
            {
                for (int j = 0; j < queries[i].selectedAttributes.Length; j++)
                {
                    if (queries[i].selectedAttributes[j])
                    {
                        switch (j)
                        {
                            case 0: // id, int

                                break;
                            case 1: // mpg
                                for (int k = 0; k < queries[i].autompg_mpg.Length; k++)
                                    intIncrementer.Increment(autompg_mpg_rqf, Bin(queries[i].autompg_mpg[k], autompg_mpg_histowidth), queries[i].times);
                                break;
                            case 2: // cylinders
                                for (int k = 0; k < queries[i].autompg_cylinders.Length; k++)
                                    intIncrementer.Increment(autompg_cylinders_rqf, Bin(queries[i].autompg_cylinders[k], autompg_cylinders_histowidth), queries[i].times);
                                break;
                            case 3: // displacement
                                for (int k = 0; k < queries[i].autompg_displacement.Length; k++)
                                    intIncrementer.Increment(autompg_displacement_rqf, Bin(queries[i].autompg_displacement[k], autompg_displacement_histowidth), queries[i].times);
                                break;
                            case 4: // horsepower
                                for (int k = 0; k < queries[i].autompg_horsepower.Length; k++)
                                    intIncrementer.Increment(autompg_horsepower_rqf, Bin(queries[i].autompg_horsepower[k], autompg_horsepower_histowidth), queries[i].times);
                                break;
                            case 5: // weight
                                for (int k = 0; k < queries[i].autompg_weight.Length; k++)
                                    intIncrementer.Increment(autompg_weight_rqf, Bin(queries[i].autompg_weight[k], autompg_weight_histowidth), queries[i].times);
                                break;
                            case 6: // acceleration
                                for (int k = 0; k < queries[i].autompg_acceleration.Length; k++)
                                    intIncrementer.Increment(autompg_acceleration_rqf, Bin(queries[i].autompg_acceleration[k], autompg_acceleration_histowidth), queries[i].times);
                                break;
                            case 7: // model_year
                                for (int k = 0; k < queries[i].autompg_model_year.Length; k++)
                                    intIncrementer.Increment(autompg_model_year_rqf, queries[i].autompg_model_year[k], queries[i].times);
                                break;
                            case 8: // origin
                                for (int k = 0; k < queries[i].autompg_origin.Length; k++)
                                    intIncrementer.Increment(autompg_origin_rqf, queries[i].autompg_origin[k], queries[i].times);
                                break;
                            case 9: // brand
                                if (queries[i].autompg_brand.Length > 1)
                                {
                                    (string, string)[] combinations = HelperFunctions.tupleCombinations(queries[i].autompg_brand);
                                    for (int l = 0; l < combinations.Length; l++)
                                        stringTupleIncrementer.Increment(autompg_brand_in_clause_intersections, combinations[l], queries[i].times);
                                    for (int m = 0; m < queries[i].autompg_brand.Length; m++)
                                        stringIncrementer.Increment(autompg_brand_in_clause_occurences, queries[i].autompg_brand[m], queries[i].times); ;
                                } 
                                for (int k = 0; k < queries[i].autompg_brand.Length; k++)
                                    stringIncrementer.Increment(autompg_brand_rqf, queries[i].autompg_brand[k], queries[i].times);
                                break;
                            case 10: // model
                                if (queries[i].autompg_model.Length > 1)
                                {
                                    (string, string)[] combinations = HelperFunctions.tupleCombinations(queries[i].autompg_model);
                                    for (int l = 0; l < combinations.Length; l++)
                                        stringTupleIncrementer.Increment(autompg_model_in_clause_intersections, combinations[l], queries[i].times);
                                    for (int m = 0; m < queries[i].autompg_model.Length; m++)
                                        stringIncrementer.Increment(autompg_model_in_clause_occurences, queries[i].autompg_model[m], queries[i].times);
                                }
                                for (int k = 0; k < queries[i].autompg_model.Length; k++)
                                    stringIncrementer.Increment(autompg_model_rqf, queries[i].autompg_model[k], queries[i].times);
                                
                                break;
                            case 11: // type
                                if (queries[i].autompg_type.Length > 1)
                                {
                                    (string, string)[] combinations = HelperFunctions.tupleCombinations(queries[i].autompg_type);
                                    for (int l = 0; l < combinations.Length; l++)
                                        stringTupleIncrementer.Increment(autompg_type_in_clause_intersections, combinations[l], queries[i].times);
                                    for (int m = 0; m < queries[i].autompg_type.Length; m++)
                                        stringIncrementer.Increment(autompg_type_in_clause_occurences, queries[i].autompg_type[m], queries[i].times);
                                }
                                for (int k = 0; k < queries[i].autompg_type.Length; k++)
                                    stringIncrementer.Increment(autompg_type_rqf, queries[i].autompg_type[k], queries[i].times);
                                break;
                        }
                    }
                }
                
            }

            //Console.WriteLine("test begint!");
            //for (int i = 0; i < autompg_brand_rqf.tuples.Count; i++)
            //{
            //    Console.WriteLine(autompg_brand_rqf.tuples[i]);
            //}

            //Console.WriteLine(autompg_brand_rqf.tuples);

            Map<int, float> autompg_mpg_qf = HelperFunctions.normalizeToOne(autompg_mpg_rqf.tuples);
            Map<int, float> autompg_cylinders_qf = HelperFunctions.normalizeToOne(autompg_cylinders_rqf.tuples);
            Map<int, float> autompg_displacement_qf = HelperFunctions.normalizeToOne(autompg_displacement_rqf.tuples);
            Map<int, float> autompg_horsepower_qf = HelperFunctions.normalizeToOne(autompg_horsepower_rqf.tuples);
            Map<int, float> autompg_weight_qf = HelperFunctions.normalizeToOne(autompg_weight_rqf.tuples);
            Map<int, float> autompg_acceleration_qf = HelperFunctions.normalizeToOne(autompg_acceleration_rqf.tuples);
            Map<int, float> autompg_model_year_qf = HelperFunctions.normalizeToOne(autompg_model_year_rqf.tuples);
            Map<int, float> autompg_origin_qf = HelperFunctions.normalizeToOne(autompg_origin_rqf.tuples);
            Map<string, float> autompg_brand_qf = HelperFunctions.normalizeToOne(autompg_brand_rqf.tuples);
            Map<string, float> autompg_model_qf = HelperFunctions.normalizeToOne(autompg_model_rqf.tuples);
            Map<string, float> autompg_type_qf = HelperFunctions.normalizeToOne(autompg_type_rqf.tuples);
            
            if(autompg_brand_qf.TryGetValue("volkswagen", out float value))
                Console.WriteLine("volkswagen qf: " + value);
            //Console.WriteLine("acceleration bucket 2: " + autompg_acceleration_qf[2]);
            //Console.WriteLine("model year 70: " + autompg_model_year_qf[70]);


            Map<(string,string), float> autompg_brand_jacquard = new Map<(string,string), float>();
            Map<(string,string), float> autompg_model_jacquard = new Map<(string,string), float>();
            Map<(string,string), float> autompg_type_jacquard = new Map<(string,string), float>();


            for (int i = 0; i < autompg_brand_in_clause_intersections.tuples.Count; i++)
            {
                ((string, string), int) tuple = autompg_brand_in_clause_intersections.tuples[i];
                int numerator = tuple.Item2;
                int denominator = -numerator;
                if (autompg_brand_in_clause_occurences.TryGetValue(tuple.Item1.Item1, out int occurences))
                    denominator += occurences;
                if (autompg_brand_in_clause_occurences.TryGetValue(tuple.Item1.Item2, out int occurences2))
                    denominator += occurences2;
                autompg_brand_jacquard.Add(tuple.Item1, ((float)numerator) / ((float)denominator));
            }
            for (int i = 0; i < autompg_model_in_clause_intersections.tuples.Count; i++)
            {
                ((string, string), int) tuple = autompg_model_in_clause_intersections.tuples[i];
                int numerator = tuple.Item2;
                int denominator = -numerator;
                if (autompg_model_in_clause_occurences.TryGetValue(tuple.Item1.Item1, out int occurences))
                    denominator += occurences;
                if (autompg_model_in_clause_occurences.TryGetValue(tuple.Item1.Item2, out int occurences2))
                    denominator += occurences2;
                autompg_model_jacquard.Add(tuple.Item1, ((float)numerator) / ((float)denominator));
            }
            for (int i = 0; i < autompg_type_in_clause_intersections.tuples.Count; i++)
            {
                ((string, string), int) tuple = autompg_type_in_clause_intersections.tuples[i];
                int numerator = tuple.Item2;
                int denominator = -numerator;
                if (autompg_type_in_clause_occurences.TryGetValue(tuple.Item1.Item1, out int occurences))
                    denominator += occurences;
                if (autompg_type_in_clause_occurences.TryGetValue(tuple.Item1.Item2, out int occurences2))
                    denominator += occurences2;
                autompg_type_jacquard.Add(tuple.Item1, ((float)numerator) / ((float)denominator));
            }

            float[] autompg_h = new float[9];

            autompg_h[0] = 0;
            autompg_h[1] = HelperFunctions.CalculateH(autompg_mpg);
            autompg_h[2] = HelperFunctions.CalculateH(HelperFunctions.arrayToFloat(autompg_cylinders));
            autompg_h[3] = HelperFunctions.CalculateH(autompg_displacement);
            autompg_h[4] = HelperFunctions.CalculateH(autompg_horsepower);
            autompg_h[5] = HelperFunctions.CalculateH(autompg_weight);
            autompg_h[6] = HelperFunctions.CalculateH(autompg_acceleration);
            autompg_h[7] = HelperFunctions.CalculateH(HelperFunctions.arrayToFloat(autompg_model_year));
            autompg_h[8] = HelperFunctions.CalculateH(HelperFunctions.arrayToFloat(autompg_origin));

            HelperFunctions.DisplayFloats(autompg_h);

            /*

            SQLiteCommand command;
            SQLiteDataReader reader;
            /*
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

                
            }
            */



            autompgDatabaseConnection.Close();
        }
        static string FirstN(char[] chars, int n)
        {
            char[] result = new char[n];
            for (int i = 0; i < n; i++)
                result[i] = chars[i];
            return new string(result);
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
        
        public void DisplayStringValue(int attrindex)
        {
            switch (attrindex)
            {
                case 0: // id, int
                    HelperFunctions.DisplayInts(autompg_id);
                    break;
                case 1: // mpg
                    HelperFunctions.DisplayFloats(autompg_mpg);
                    break;
                case 2: // cylinders
                    HelperFunctions.DisplayInts(autompg_cylinders);
                    break;
                case 3: // displacement
                    HelperFunctions.DisplayFloats(autompg_displacement);
                    break;
                case 4: // horsepower
                    HelperFunctions.DisplayFloats(autompg_horsepower);
                    break;
                case 5: // weight
                    HelperFunctions.DisplayFloats(autompg_weight);
                    break;
                case 6: // acceleration
                    HelperFunctions.DisplayFloats(autompg_acceleration);
                    break;
                case 7: // model_year
                    HelperFunctions.DisplayInts(autompg_model_year);
                    break;
                case 8: // origin
                    HelperFunctions.DisplayInts(autompg_origin);
                    break;
                case 9: // brand
                    HelperFunctions.DisplayStrings(autompg_brand);
                    break;
                case 10: // model
                    HelperFunctions.DisplayStrings(autompg_model);
                    break;
                case 11: // type
                    HelperFunctions.DisplayStrings(autompg_type);
                    break;
            }
        }
        public void ThrowValuesIn(
            int attrindex,
            string[] toParse)
        {
            selectedAttributes[attrindex] = true;
            switch (attrindex)
            {
                case 0: // id, int
                    autompg_id = HelperFunctions.ParseInts(toParse);
                    break;
                case 1: // mpg
                    autompg_mpg = HelperFunctions.ParseFloats(toParse);
                    break;
                case 2: // cylinders
                    autompg_cylinders = HelperFunctions.ParseInts(toParse);
                    break;
                case 3: // displacement
                    autompg_displacement = HelperFunctions.ParseFloats(toParse);
                    break;
                case 4: // horsepower
                    autompg_horsepower = HelperFunctions.ParseFloats(toParse);
                    break;
                case 5: // weight
                    autompg_weight = HelperFunctions.ParseFloats(toParse);
                    break;
                case 6: // acceleration
                    autompg_acceleration = HelperFunctions.ParseFloats(toParse);
                    break;
                case 7: // model_year
                    autompg_model_year = HelperFunctions.ParseInts(toParse);
                    break;
                case 8: // origin
                    autompg_origin = HelperFunctions.ParseInts(toParse);
                    break;
                case 9: // brand
                    autompg_brand = toParse;
                    break;
                case 10: // model
                    autompg_model = toParse;
                    break;
                case 11: // type
                    autompg_type = toParse;
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

        public void Increment(Map<T, int> map, T item, int times)
        {
            if (map.indices.TryGetValue(item, out int index))
            {
                map.tuples[index] = (map.tuples[index].Item1, map.tuples[index].Item2 + times);
            }
            else
            {
                map.Add(item, times);
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