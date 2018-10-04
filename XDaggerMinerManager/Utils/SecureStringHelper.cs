using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace XDaggerMinerManager.Utils
{
    public class SecureStringHelper
    {
        public static bool IsPasswordMatch(string inputPassword, string hashedPassword)
        {
            if (string.IsNullOrWhiteSpace(inputPassword) && string.IsNullOrWhiteSpace(hashedPassword))
            {
                return true;
            }

            if (string.IsNullOrWhiteSpace(inputPassword) || string.IsNullOrWhiteSpace(hashedPassword))
            {
                return false;
            }

            return (HashPassword(inputPassword) == hashedPassword);
        }

        public static string HashPassword(string password)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(password);
            SHA256Managed hashstring = new SHA256Managed();
            byte[] hash = hashstring.ComputeHash(bytes);
            string hashString = string.Empty;
            foreach (byte x in hash)
            {
                hashString += String.Format("{0:x2}", x);
            }

            return hashString;
        }
    }
}
