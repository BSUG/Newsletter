using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;

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
        private readonly string _episodeJsonFilePath;
        private readonly string _episodesCacheFolder;
        private readonly string _episodeFileNameFormat;
        private readonly string _blogPostUrlFormat;
        private readonly int _firstEpisodeNumber;
        private readonly string[] _filterLinks;

        /// <summary>
        /// Initializes a new instance of the <see cref="DuplicateFinder" /> class.
        /// </summary>
        /// <param name="episodeJsonFilePath">The episode json file path.</param>
        /// <param name="episodesCacheFolder">The episodes cache folder.</param>
        /// <param name="episodeFileNameFormat">The episode file name format.</param>
        /// <param name="blogPostUrlFormat">The blog post URL format.</param>
        /// <param name="firstEpisodeNumber">The first episode number.</param>
        /// <param name="filterLinks">The filter links.</param>
        public DuplicateFinder(string episodeJsonFilePath, string episodesCacheFolder, string episodeFileNameFormat, string blogPostUrlFormat, int firstEpisodeNumber, string[] filterLinks)
        {
            _episodeFileNameFormat = episodeFileNameFormat;
            _episodesCacheFolder = episodesCacheFolder;
            _episodeJsonFilePath = episodeJsonFilePath;
            _blogPostUrlFormat = blogPostUrlFormat;

            _firstEpisodeNumber = firstEpisodeNumber;

            _filterLinks = filterLinks;
        }

        /// <summary>
        /// Prevents a default instance of the <see cref="DuplicateFinder"/> class from being created.
        /// </summary>
        private DuplicateFinder()
        {
        }

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

            ConsoleHelper.Info("Looking for duplicates.");
            DisplayDuplicates(lastEpisode, allEpisodes);
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
                    string pastEpisodeUrl = pastEpisodeItem.Url.TrimEnd(new[] { '/' });

                    foreach (ContentItem episodeItem in episodeItems)
                    {
                        string episodeUrl = episodeItem.Url.TrimEnd(new[] {'/'});

                        if (pastEpisodeUrl.Equals(episodeUrl, StringComparison.OrdinalIgnoreCase))
                        {
                            ConsoleHelper.Warning("Ep. {0}, Url {1}. Current episode title: {2}".PadRight(10), pastEpisode.Number, pastEpisodeItem.Url, episodeItem.Title);
                            duplicatesFound = true;
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

                    blogLink = RemoveAdsUrlParams(blogLink);

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
        /// Removes the ads URL parameters.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <returns></returns>
        private string RemoveAdsUrlParams(string url)
        {
            string adsParam = "?utm_";

            if (url.IndexOf(adsParam, StringComparison.OrdinalIgnoreCase) != -1)
                url = url.Substring(0, url.IndexOf(adsParam, StringComparison.OrdinalIgnoreCase));

            return url;
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