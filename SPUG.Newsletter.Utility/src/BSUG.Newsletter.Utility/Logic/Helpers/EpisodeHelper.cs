using System.Collections.Generic;
using System.IO;
using System.Net.NetworkInformation;
using Newtonsoft.Json;

using BSUG.Newsletter.Utility.Entities;

namespace BSUG.Newsletter.Utility.Logic.Helpers
{
    /// <summary>
    /// Helper class that provides methods to work with episodes.
    /// </summary>
    public class EpisodeHelper
    {
        /// <summary>
        /// Gets the episode content items.
        /// </summary>
        /// <param name="episode">The episode.</param>
        /// <returns>The list of all content items of the episode.</returns>
        public static List<Item> GetEpisodeContentItems(Episode episode)
        {
            List<Item> items = new List<Item>();

            items.AddRange(episode.News.Articles);
            items.AddRange(episode.Tools.Articles);
            items.AddRange(episode.Novices.Articles);
            items.AddRange(episode.Videos.Articles);
            items.AddRange(episode.Blogs.Articles);
            items.AddRange(episode.Events.Articles);

            return items;
        }

        /// <summary>
        /// Saves the episodes to the specified folder.
        /// </summary>
        /// <param name="episodesFolderPath">The episodes folder path.</param>
        /// <param name="episodeFileNameFormat">The episode file name format.</param>
        /// <param name="episodes">The list of episodes.</param>
        /// <param name="overwrite">if set to <c>true</c> overwrites the existing file.</param>
        public static void SaveEpisodes(string episodesFolderPath, string episodeFileNameFormat, List<Episode> episodes, bool overwrite)
        {
            foreach (Episode episode in episodes)
            {
                string episodeFileName = string.Format(episodeFileNameFormat, episode.Number);
                string episodeFilePath = Path.Combine(episodesFolderPath, episodeFileName);

                SaveEpisode(episodeFilePath, episode, overwrite);
            }
        }

        /// <summary>
        /// Saves the episode by the specified file path.
        /// </summary>
        /// <param name="episodeFilePath">The episode file path.</param>
        /// <param name="episode">The episode.</param>
        /// <param name="overwrite">if set to <c>true</c> overwrites the existing file.</param>
        public static void SaveEpisode(string episodeFilePath, Episode episode, bool overwrite)
        {
            bool fileExists = File.Exists(episodeFilePath);

            if (!fileExists || overwrite)
            {
                string json = JsonConvert.SerializeObject(episode, Formatting.Indented);
                File.WriteAllText(episodeFilePath, json);
            }
        }

        /// <summary>
        /// Gets the episode object from file.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        /// <returns>The Episode object.</returns>
        public static Episode GetEpisodeFromFile(string filePath)
        {
            string fileContent = File.ReadAllText(filePath);
            try
            {
                Episode episode = JsonConvert.DeserializeObject<Episode>(fileContent);
                return episode;
            }
            catch (JsonReaderException exception)
            {
                ConsoleHelper.Error("There were problems while parsing episode's json file. Please check that the json is valid. Additional information: {0}", exception.Message);
            }

            return null;
        }
    }
}