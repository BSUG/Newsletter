using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;

using HtmlAgilityPack;

using BSUG.Newsletter.Utility.Entities;
using BSUG.Newsletter.Utility.Logic.Helpers;

namespace BSUG.Newsletter.Utility.Logic
{
    /// <summary>
    /// The class allows to look for duplicates in the current episode's json file and previous episodes published in a blog.
    /// </summary>
    public class DuplicateFinder
    {
        #region Private variables

        private readonly string _episodeJsonFilePath;
        private readonly string _episodesCacheFolder;
        private readonly string _episodeFileNameFormat;
        private readonly string _blogPostUrlFormat;
        private readonly int _firstEpisodeNumber;
        private readonly string[] _filterLinks;
        private readonly string[] _stopWords;

        #endregion Private variables

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="DuplicateFinder" /> class.
        /// </summary>
        /// <param name="episodeJsonFilePath">The episode json file path.</param>
        /// <param name="episodesCacheFolder">The episodes cache folder.</param>
        /// <param name="episodeFileNameFormat">The episode file name format.</param>
        /// <param name="blogPostUrlFormat">The blog post URL format.</param>
        /// <param name="firstEpisodeNumber">The first episode number.</param>
        /// <param name="filterLinks">The filter links.</param>
        /// <param name="stopWords">The stop words.</param>
        public DuplicateFinder(string episodeJsonFilePath, string episodesCacheFolder, string episodeFileNameFormat, string blogPostUrlFormat, int firstEpisodeNumber, string[] filterLinks, string[] stopWords)
        {
            _episodeFileNameFormat = episodeFileNameFormat;
            _episodesCacheFolder = episodesCacheFolder;
            _episodeJsonFilePath = episodeJsonFilePath;
            _blogPostUrlFormat = blogPostUrlFormat;

            _firstEpisodeNumber = firstEpisodeNumber;

            _filterLinks = filterLinks;
            _stopWords = stopWords;
        }

        /// <summary>
        /// Prevents a default instance of the <see cref="DuplicateFinder"/> class from being created.
        /// </summary>
        private DuplicateFinder()
        {
        }

        #endregion Constructors

        /// <summary>
        /// Finds the duplicates.
        /// </summary>
        public void FindDuplicates()
        {
            ConsoleHelper.Info("Getting previous episodes starting from {0}.", _firstEpisodeNumber);
            List<Episode> allEpisodes = GetAllEpisodes(_episodesCacheFolder, _blogPostUrlFormat, _episodeFileNameFormat, _firstEpisodeNumber);
            ConsoleHelper.Info("{0} episodes loaded.", allEpisodes.Count);

            ConsoleHelper.Info("Saving loaded episodes to cache.");
            // Save loaded episodes to improve processing speed.
            EpisodeHelper.SaveEpisodes(_episodesCacheFolder, _episodeFileNameFormat, allEpisodes, false);

            ConsoleHelper.Info("Loading current episode json file.");
            Episode lastEpisode = EpisodeHelper.GetEpisodeFromFile(_episodeJsonFilePath);

            if (lastEpisode != null)
            {
                ConsoleHelper.Info("Looking for duplicates in the last episode.");
                DisplayDuplicates(lastEpisode);

                ConsoleHelper.Info("Looking for stop words in the last episode.");
                DisplayStopWords(lastEpisode, _stopWords);

                if (allEpisodes != null)
                {
                    ConsoleHelper.Info("Looking for duplicates between the last and previous episodes.");
                    DisplayDuplicates(lastEpisode, allEpisodes);
                }
            }
        }

        /// <summary>
        /// Gets all episodes either from cache in the folder or from the blog.
        /// </summary>
        /// <param name="episodesCacheFolder">The episodes cache folder.</param>
        /// <param name="blogPostUrlFormat">The blog post URL format.</param>
        /// <param name="episodeFileNameFormat">The episode file name format.</param>
        /// <param name="firstEpisodeNumber">The first episode number.</param>
        /// <returns></returns>
        private List<Episode> GetAllEpisodes(string episodesCacheFolder, string blogPostUrlFormat, string episodeFileNameFormat, int firstEpisodeNumber)
        {
            bool episodeNotFound = false;
            int episodeNumber = firstEpisodeNumber;
            List<Episode> allEpisodes = new List<Episode>();

            // Try to load episodes until the next one is not found
            while (!episodeNotFound)
            {
                string episodeFileName = string.Format(episodeFileNameFormat, episodeNumber);
                string episodeFilePath = Path.Combine(episodesCacheFolder, episodeFileName);

                // If available, load episode from file
                if (File.Exists(episodeFilePath))
                {
                    Episode episode = EpisodeHelper.GetEpisodeFromFile(episodeFilePath);
                    episode.Number = episodeNumber;
                    allEpisodes.Add(episode);

                    ConsoleHelper.Info("Loaded episode {0} from cache.", episode.Number);
                }
                // Otherwise load from blog
                else
                {
                    string episodeBlogUrl = string.Format(blogPostUrlFormat, episodeNumber);
                    Episode episode;

                    TryGetEpisodeFromBlog(episodeBlogUrl, out episode);

                    if (episode != null)
                    {
                        episode.Number = episodeNumber;
                        allEpisodes.Add(episode);

                        ConsoleHelper.Info("Loaded episode {0} from blog.", episode.Number);
                    }
                    else
                    {
                        // If episode hasn't been loaded, stop loading episodes
                        episodeNotFound = true;
                    }
                }

                episodeNumber++;
            }

            return allEpisodes;
        }

        /// <summary>
        /// Displays the duplicates.
        /// </summary>
        /// <param name="episode">The episode.</param>
        /// <param name="episodesList">The episodes list.</param>
        /// <returns></returns>
        private void DisplayDuplicates(Episode episode, List<Episode> episodesList)
        {
            bool duplicatesFound = false;

            foreach (Episode pastEpisode in episodesList)
            {
                List<Item> pastEpisodeItems = EpisodeHelper.GetEpisodeContentItems(pastEpisode);
                List<Item> episodeItems = EpisodeHelper.GetEpisodeContentItems(episode);

                foreach (ContentItem pastEpisodeItem in pastEpisodeItems)
                {
                    string pastEpisodeUrl = GetCleanUrl(pastEpisodeItem.Url.TrimEnd('/'));

                    foreach (ContentItem episodeItem in episodeItems)
                    {
                        if (!string.IsNullOrEmpty(episodeItem.Url))
                        {
                            string episodeUrl = GetCleanUrl(episodeItem.Url.TrimEnd('/'));

                            if (pastEpisodeUrl.Equals(episodeUrl, StringComparison.OrdinalIgnoreCase))
                            {
                                ConsoleHelper.Warning("Ep. {0}, Url {1}. Current episode title: {2}".PadRight(10),
                                    pastEpisode.Number, pastEpisodeItem.Url, episodeItem.Title);
                                duplicatesFound = true;
                            }
                        }
                    }
                }
            }

            if (!duplicatesFound)
            {
                ConsoleHelper.Success("Yay! No duplicates found.");
            }
        }

        /// <summary>
        /// Displays the duplicates between links in the episode, if any found.
        /// </summary>
        /// <param name="episode">The episode.</param>
        private void DisplayDuplicates(Episode episode)
        {
            bool duplicatesFound = false;
            List<Item> episodeItems = EpisodeHelper.GetEpisodeContentItems(episode);

            foreach (ContentItem contentItem in episodeItems)
            {
                if (!string.IsNullOrEmpty(contentItem.Url))
                {
                    string url = GetCleanUrl(contentItem.Url.TrimEnd('/'));

                    foreach (ContentItem ci in episodeItems)
                    {
                        if ((contentItem != ci) && !string.IsNullOrEmpty(contentItem.Url) && !string.IsNullOrEmpty(ci.Url))
                        {
                            string url2 = GetCleanUrl(ci.Url.TrimEnd('/'));

                            if (url.Equals(url2, StringComparison.OrdinalIgnoreCase))
                            {
                                ConsoleHelper.Warning("Current episode titles: \"{0}\" and \"{1}\"", contentItem.Title, ci.Title);
                                duplicatesFound = true;
                            }
                        }
                    }
                }
            }

            if (!duplicatesFound)
            {
                ConsoleHelper.Success("Yay! No duplicates found.");
            }
        }

        /// <summary>
        /// Displays possible stop words in the episode.
        /// </summary>
        /// <param name="episode">The last episode.</param>
        /// <param name="stopWords">The stop words.</param>
        private void DisplayStopWords(Episode episode, string[] stopWords)
        {
            List<Item> episodeItems = EpisodeHelper.GetEpisodeContentItems(episode);
            string regexExpression = string.Empty;

            for (int i = 0; i < stopWords.Length; i++)
            {
                regexExpression += stopWords[i];
                if (i != stopWords.Length - 1)
                {
                    regexExpression += "|";
                }
            }

            regexExpression = $@"\b({regexExpression})\b";

            foreach (ContentItem contentItem in episodeItems)
            {
                if (contentItem.Title != null)
                {
                    FindStopWords("Title", contentItem.Title, regexExpression);
                }
                
                if (contentItem.Text != null)
                {
                    FindStopWords("Text", contentItem.Text, regexExpression);
                }
            }
        }

        private static void FindStopWords(string sectionName, string source, string regexExpression)
        {
            Match match = Regex.Match(source, regexExpression, RegexOptions.Singleline | RegexOptions.IgnoreCase);
            List<string> stopWordsFound = new List<string>();

            while (match.Success)
            {
                stopWordsFound.Add(match.Value);
                match = match.NextMatch();
            }

            if (stopWordsFound.Count != 0)
            {
                ConsoleHelper.Warning($"Please check for stop words \"{string.Join(", ", stopWordsFound)}\" in {sectionName}: {source}.");
            }
        }

        /// <summary>
        /// Downloads the page.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <returns>The html code of the page.</returns>
        /// <exception cref="ArgumentException">Blog post not found by the url: {url}</exception>
        private string DownloadPage(string url)
        {
            using (WebClient client = new WebClient())
            {
                try
                {
                    string html = client.DownloadString(url);
                    return html;
                }
                catch (WebException exception)
                {
                    throw new ArgumentException("Blog post not found by the url: {url}", exception);
                }
            }
        }

        /// <summary>
        /// Tries the get episode from blog.
        // Simple parse, puts all links in the Blog category.
        // TODO: Parse html into an Episode using proper categories
        /// </summary>
        /// <param name="episodePostUrl">The episode post URL.</param>
        /// <param name="episode">The episode.</param>
        private void TryGetEpisodeFromBlog(string episodePostUrl, out Episode episode)
        {
            try
            {
                string pageContent = DownloadPage(episodePostUrl);
                List<string> linksList = new List<string>();

                HtmlDocument doc = new HtmlDocument();
                doc.LoadHtml(pageContent);

                foreach (HtmlNode link in doc.DocumentNode.SelectNodes("//a[@href]"))
                {
                    string blogLink = link.GetAttributeValue("href", string.Empty);

                    blogLink = GetCleanUrl(blogLink);

                    if (!linksList.Contains(blogLink))
                        linksList.Add(blogLink);
                }

                episode = new Episode();

                foreach (string link in linksList)
                {
                    if (IsContentLink(link))
                    {
                        episode.Blogs.Articles.Add(new ContentItem { Url = link });
                    }
                }

                episode.DigestTitle = episodePostUrl;
            }
            catch (ArgumentException)
            {
                ConsoleHelper.Warning("Episode with the Url: {0} not found", episodePostUrl);
                episode = null;
            }
        }

        /// <summary>
        /// Gets the URL without additional parameters and ads parameters.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <returns></returns>
        private string GetCleanUrl(string url)
        {
            return RemoveAdditionalParams(RemoveAdsUrlParams(url));
        }

        /// <summary>
        /// Removes the ads URL parameters.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <returns></returns>
        private string RemoveAdsUrlParams(string url)
        {
            string adsParam = "?utm_";

            return GetSubstring(url, adsParam);
        }

        /// <summary>
        /// Removes the additional parameters from the URL that go after the # symbol.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <returns></returns>
        private string RemoveAdditionalParams(string url)
        {
            string additionalParam = "#";

            return GetSubstring(url, additionalParam);
        }

        /// <summary>
        /// Returns the substring of the specified string that ends with the specified string to find.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="stringToFind">The string to find.</param>
        /// <returns></returns>
        private string GetSubstring(string source, string stringToFind)
        {
            int index = source.IndexOf(stringToFind, StringComparison.OrdinalIgnoreCase);

            if (index != -1)
                source = source.Substring(0, index);

            return source;
        }

        /// <summary>
        /// Determines whether the specified link is a link to a blog post, video, presentation, etc. Filters out all the service links (blog internal links, ads, javascript code).
        /// </summary>
        /// <param name="link">The link.</param>
        /// <returns></returns>
        private bool IsContentLink(string link)
        {
            Uri absoluteUrl;
            Uri.TryCreate(link, UriKind.Absolute, out absoluteUrl);

            // Only absolute Urls are supported
            if ((absoluteUrl == null) || (absoluteUrl.Scheme == "javascript"))
                return false;

            return _filterLinks.All(filterLink => link.IndexOf(filterLink, StringComparison.OrdinalIgnoreCase) == -1);
        }
    }
}