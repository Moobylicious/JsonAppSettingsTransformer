using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PwdHelper
{
    class Program
    {
        static void Main(string[] args)
        {

            bool isEncrypting = ((args.Length > 0) && (args[0] == "e"));

            //this program itself is protected with a password, albeit a hard-coded one.
            var runPwd = args[1].EncryptOrDecrypt(true);

            //Compare given password to encrypted GKSERVER one
            if (runPwd != "v(}yu,\"w~")
            {
                return;
            }

            if ((args.Length > 2) && !string.IsNullOrEmpty(args[2]))
                Console.WriteLine(args[2].EncryptOrDecrypt(isEncrypting));
        }
    }

    public static class Helper
    {
        /// <summary>
        /// Performs basic encryption of strings.  kind of ascii-fied version of ROT13.
        /// Basically any character within the 'normal' range of characters (space, exclamation, numbers, upper/lowercase etc)
        /// Will be transformed by 13 characters or more (based partly on its position in the string)
        /// Not intended to fool hackers, just a quick and simple way of masking passwords etc. to put into config files.
        /// </summary>
        public static string EncryptOrDecrypt(
            this string value,
            bool isEncrypting,
            int firstValidChar = 32,
            int lastValidChar = 126,
            int shiftBase = 13,
            Func<int, int> recalcShift = null)
        {
            var array = value.ToCharArray();

            //any character from 32 to 126 is valid as a character in this routine...
            //for each char, we check it's in this range and adjust it by adding 13+i to it.
            //if it goes over 126 then wrap around...
            for (int i = 0; i < array.Length; i++)
            {
                int number = (int)array[i];

                int newChar = number;

                if (number >= firstValidChar && number <= lastValidChar)
                {
                    if (isEncrypting)
                    {
                        newChar += shiftBase + i;
                        while (newChar > lastValidChar)
                        {
                            newChar = (firstValidChar - 1) + (newChar - lastValidChar);
                        }
                    }
                    else
                    {
                        newChar -= (shiftBase + i);
                        while (newChar < firstValidChar)
                        {
                            newChar = (lastValidChar + 1) - (firstValidChar - newChar);
                        }
                    }
                }
                array[i] = (char)newChar;
            }
            return new string(array);
        }
    }

}
