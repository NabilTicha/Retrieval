using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
                Console.Write("," + ints[i]);
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
    }
}
