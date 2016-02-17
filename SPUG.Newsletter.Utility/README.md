Newsletter Utility
==============

This tool allows to find duplicates in the current episode's json file and previously published episodes. It downloads episodes prevously published in the blog and looks for duplicate links.

By default the tool expects the digest.json file to be placed in the same directory with the tool. This and other settings can be changed with the application configuration file.


Configuration parameters:
====
The following parameters can be specified in the application configuration file:
1. *episodeJsonFilePath* - a path to the current episode's json file.
2. *episodesCacheFolder* - a path to the folder where cached episodes are stored. The folder should exist prior to utility execution.
3. *episodeFileNameFormat* - format for files naming in the cache folder.
4. *blogPostUrlFormat* - a format of the url for published episodes in a blog.
5. *firstEpisodeNumber* - a first episode to start looking for duplicates.
6. *filterLinks* - a list of links to ignore when looking for duplicates. Links should be separated by semicolon.