using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XDaggerMinerManager.Utils
{
    public class RandomUtils
    {
        /// <summary>
        /// The length should be less than 32
        /// </summary>
        /// <param name="length"></param>
        /// <returns></returns>
        public static string GenerateString(int length)
        {
            string ran = Guid.NewGuid().ToString().Replace("-", "");

            length = (length > ran.Length) ? ran.Length : length;

            return ran.Substring(0, length);
        }

        /// <summary>
        /// Generates the string until the condition matches
        /// </summary>
        /// <param name="length"></param>
        /// <param name="func"></param>
        /// <returns></returns>
        public static string GenerateString(int length, Func<string, bool> func)
        {
            string result = string.Empty;
            do
            {
                result = GenerateString(length);
            } while (!func(result));

            return result;
        }
    }
}
