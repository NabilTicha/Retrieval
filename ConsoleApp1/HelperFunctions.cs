using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    public class HelperFunctions
    {
        public int[] ParseInts(string[] toParse)
        {
            int[] result = new int[toParse.Length];
            for (int i = 0; i < toParse.Length; i++)
                result[i] = int.Parse(toParse[i]);
            return result;
        }
        public int[] ParseFloats(string[])
        {

        }
    }
}
