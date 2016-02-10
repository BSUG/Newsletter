using System;

namespace BSUG.Newsletter.Utility.Logic.Helpers
{
    /// <summary>
    /// Provides helper methods to display information into the console.
    /// </summary>
    public class ConsoleHelper
    {
        /// <summary>
        /// Displays the warning message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="parameters">The parameters.</param>
        public static void Warning(string message, params object[] parameters)
        {
            Console.BackgroundColor = ConsoleColor.Yellow;
            Console.ForegroundColor = ConsoleColor.DarkYellow;

            Console.WriteLine(message, parameters);

            Console.ResetColor();
        }

        /// <summary>
        /// Displays the information message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="parameters">The parameters.</param>
        public static void Info(string message, params object[] parameters)
        {
            Console.WriteLine(message, parameters);
        }

        /// <summary>
        /// Displays the error message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="parameters">The parameters.</param>
        public static void Error(string message, params object[] parameters)
        {
            Console.BackgroundColor = ConsoleColor.Red;
            Console.ForegroundColor = ConsoleColor.DarkRed;

            Console.WriteLine(message, parameters);

            Console.ResetColor();
        }
    }
}