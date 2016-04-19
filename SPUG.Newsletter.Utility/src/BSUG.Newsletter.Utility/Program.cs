using System;
using System.Configuration;
using System.IO;

using BSUG.Newsletter.Utility.Logic;
using BSUG.Newsletter.Utility.Logic.Helpers;

namespace BSUG.Newsletter.Utility
{
    class Program
    {
        static void Main(string[] args)
        {
            // TODO: Add args support to perform various operations. For now, only find duplicates is supported.
            string operation = "duplicates";

            switch (operation)
            {
                case "duplicates":
                    FindDuplicates();
                    break;
                default:
                    ConsoleHelper.Info("Operation {0} is not supported.", operation);
                    break;
            }

            ConsoleHelper.Info("All done. Press any key to exit.");
            Console.ReadKey();
        }

        /// <summary>
        /// Finds the duplicates in the current episode and previous published episodes.
        /// </summary>
        private static void FindDuplicates()
        {
            ConsoleHelper.Info("Newsletter Utility: Finding duplicates.");

            bool parametersValid = true;

            string episodeJsonFilePath = LoadConfigParam("episodeJsonFilePath");
            string episodesCacheFolder = LoadConfigParam("episodesCacheFolder");
            string episodeFileNameFormat = LoadConfigParam("episodeFileNameFormat");
            string blogPostUrlFormat = LoadConfigParam("blogPostUrlFormat");

            string firstEpisodeNumberString = LoadConfigParam("firstEpisodeNumber");
            string filterLinksString = LoadConfigParam("filterLinks");
            string stopWordsString = LoadConfigParam("stopWords");

            // Validate configuration parameters
            if (episodeJsonFilePath == null || episodesCacheFolder == null || episodeFileNameFormat == null ||
                blogPostUrlFormat == null || firstEpisodeNumberString == null || filterLinksString == null || stopWordsString == null)
            {
                parametersValid = false;
            }

            if (!string.IsNullOrEmpty(episodesCacheFolder) && !Directory.Exists(episodesCacheFolder))
            {
                ConsoleHelper.Info("Cache folder does not exist. It will be created by the following path: {0}.", episodesCacheFolder);
                Directory.CreateDirectory(episodesCacheFolder);
            }

            if (!string.IsNullOrEmpty(episodeJsonFilePath) && !File.Exists(episodeJsonFilePath))
            {
                ConsoleHelper.Error("Json file with the current episode has not been found. Please check the specified path: {0}.", episodeJsonFilePath);
                parametersValid = false;
            }

            int firstEpisodeNumber;
            if (!int.TryParse(firstEpisodeNumberString, out firstEpisodeNumber))
            {
                ConsoleHelper.Error("Cannot parse first episode number {0}. Please specify a valid number.", firstEpisodeNumberString);
                parametersValid = false;
            }

            if (parametersValid)
            {
                // Parse links string into an array
                string[] filterLinks = filterLinksString.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < filterLinks.Length; i++)
                    filterLinks[i] = filterLinks[i].Trim();

                // Parse stop words string into an array
                string[] stopWords = stopWordsString.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < stopWords.Length; i++)
                    stopWords[i] = stopWords[i].Trim();

                var duplicateFinder = new DuplicateFinder(episodeJsonFilePath, episodesCacheFolder, episodeFileNameFormat, blogPostUrlFormat, firstEpisodeNumber, filterLinks, stopWords);
                duplicateFinder.FindDuplicates();
            }
            else
            {
                ConsoleHelper.Warning("Please fix configuarion parameters and run again.");
            }
        }

        /// <summary>
        /// Loads the configuration parameter from AppSettings.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns></returns>
        private static string LoadConfigParam(string key)
        {
            string value = ConfigurationManager.AppSettings[key];

            if (value == null)
                ConsoleHelper.Error("{0} parameter is not specified in the application config.", key);

            return value;
        }
    }
}