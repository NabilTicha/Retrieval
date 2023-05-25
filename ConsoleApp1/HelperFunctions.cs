using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace ConsoleApp1
{
    public static class HelperFunctions
    {
        public static int[] ParseInts(string[] toParse)
        {
            int[] result = new int[toParse.Length];
            for (int i = 0; i < toParse.Length; i++)
                result[i] = int.Parse(toParse[i]);
            return result;
        }
        public static float[] ParseFloats(string[] toParse)
        {
            float[] result = new float[toParse.Length];
            for (int i = 0; i < toParse.Length; i++)
                result[i] = float.Parse(toParse[i], CultureInfo.InvariantCulture);
            return result;
        }
        public static void DisplayStringTuples((string,string)[] array)
        {
            Console.Write("[" + array[0]);
            for (int i = 1; i < array.Length; i++)
                Console.Write("," + array[i]);
            Console.Write("]");
        }
        public static void DisplayInts(int[] ints)
        {
            Console.Write("[" + ints[0]);
            for (int i = 1; i < ints.Length; i++)
                Console.Write(","+ints[i]);          
            Console.Write("]");
        }
        public static void DisplayFloats(float[] ints)
        {
            Console.Write("[" + ints[0]);
            for (int i = 1; i < ints.Length; i++)
                Console.Write("|" + ints[i], CultureInfo.InvariantCulture);
            Console.Write("]");
        }
        public static void DisplayStrings(string[] ints)
        {
            Console.Write("[" + ints[0]);
            for (int i = 1; i < ints.Length; i++)
                Console.Write("," + ints[i]);
            Console.Write("]");
        }
        public static string[] ExtractFromINclause(string compactINclause) // a compact in clause is
        { // one which has no spaces in it, just brackets and commas separating the values
            // first we remove the brackets on either end
            char[] trimmed = new char[compactINclause.Length - 2];
            for (int i = 0; i < trimmed.Length; i++)
                trimmed[i] = compactINclause[i + 1];
            // now the brackets are gone, we split all values on the commas
            string[] splitOnCommas = new string(trimmed).Split(',', StringSplitOptions.None);
            // lastly, we remove the \' characters at the edges
            string[] result = new string[splitOnCommas.Length];
            for (int i = 0; i < result.Length; i++)
                result[i] = splitOnCommas[i].Split('\'', StringSplitOptions.RemoveEmptyEntries)[0];
            return result;
        }

        public static int CalcMax(List<(int,int)> values)
        {
            int max = 0;
            for(int i = 0; i < values.Count; i++)
            {
                if (values[i].Item2 > max)
                    max = values[i].Item2;
            }

            return max;
        }

        public static int CalcMax(List<(string, int)> values)
        {
            int max = 0;
            for (int i = 0; i < values.Count; i++)
            {
                if (values[i].Item2 > max)
                    max = values[i].Item2;
            }

            return max;
        }

        public static Map<int, float> normalizeToOne (List<(int, int)> values)
        {
            Map<int,float> normalizedValues = new Map<int,float>();
            int max = CalcMax(values);

            if (max == 0)
            {
                return normalizedValues;
            }

            for (int i = 0; i < values.Count; i++)
            {
                normalizedValues.Add(values[i].Item1,((float)values[i].Item2) / ((float)max));
            }

            return normalizedValues;
        }

        public static Map<string, float> normalizeToOne(List<(string, int)> values)
        {
            Map<string, float> normalizedValues = new Map<string, float>();
            int max = CalcMax(values);

            if(max == 0)
            {
                return normalizedValues;
            }

            for (int i = 0; i < values.Count; i++)
            {
                normalizedValues.Add(values[i].Item1, ((float)values[i].Item2) / ((float)max));
            }

            return normalizedValues;
        }

        public static (string, string) orderTuple((string, string) tuple)
        {
            if (String.Compare(tuple.Item1, tuple.Item2) > 0)
            {
                tuple = (tuple.Item2, tuple.Item1);
            }

            return tuple;
        }

        public static (string, string)[] tupleCombinations(string[] strings)
        {
            List<(string, string)> preresult = new List<(string, string)>();
            for (int i = 0; i < strings.Length - 1; i++)
                for (int j = i + 1; j < strings.Length; j++)
                    preresult.Add(orderTuple((strings[i], strings[j])));         
            return preresult.ToArray();
        }

        public static float CalculateMean(float[] array)
        {
            float sum = 0;
            foreach (float num in array)
            {
                sum += num;
            }
            return sum / array.Length;
        }

        public static float CalculateSquaredDifferencesMean(float[] array, float mean)
        {
            float squaredDifferencesSum = 0;
            foreach (float num in array)
            {
                float difference = num - mean;
                squaredDifferencesSum += difference * difference;
            }
            return squaredDifferencesSum / array.Length;
        }

        public static float CalculateStandardDeviation(float[] array)
        {
            float mean = CalculateMean(array);
            float SquaredDifferencesMean = CalculateSquaredDifferencesMean(array, mean);
            return (float)Math.Sqrt(SquaredDifferencesMean);
        }

        public static float CalculateH(float[] array)
        {
            float standardDeviation = CalculateStandardDeviation(array);
            return 1.06f * standardDeviation * ((float)Math.Pow((double)array.Length, -0.2d));
        }

        public static float[] arrayToFloat(int[] array)
        {
            float[] result = new float[array.Length];

            for(int i = 0; i < array.Length; i++)
            {
                result[i] = (float)array[i];
            }

            return result;
        }
        public static string RemoveQuotations(string s)
        {
            List<char> result = new List<char>();
            for (int i = 0; i < s.Length; i++)
                if (s[i] != '\'')
                    result.Add(s[i]);
            return new string(result.ToArray());
        }
    }
}
