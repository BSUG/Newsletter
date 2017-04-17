import * as fs from "fs";
import * as path from "path";
import * as moment from "moment";
import * as Immutable from "immutable";

import { Tweet } from "./Twitter";
import AppConfig from "./config";

const DateFormat: string = "dddd, MMM D";

const greetings: Array<string> = [
    "You are about to review <strong>{{TWEETS}}</strong> tweets for <strong>{{DAYS}}</strong> days.",
    "You've got <strong>{{TWEETS}}</strong> tweets for <strong>{{DAYS}}</strong> days.",
    "Here's some <strong>{{TWEETS}}</strong> tweets for <strong>{{DAYS}}</strong> days."
];

const highlightColors: Array<string> = [
    "#ff00ff",
    "#daa520",
    "#8a2be2",
    "#7fff00",
    "#ff8c00",
    "#ff69b4"
];

const farewells: Array<string> = [
    "The end!",
    "That's it for today!",
    "See you next week!",
    "All done!"
];

const emoticons: Array<string> = [
    `<div>
        <div class="emoticon">
            (='X'=)
        </div>
        <div class="footer-text">
            This cat is happy with your progress.
        </div>
    </div>`,
    `<div style="">
        <div class="emoticon">
            ^(;,;)^
        </div>
        <div class="footer-text">
            Cthulhu is pleased!
        </div>
        </div>`,
    `<div style="">
        <div class="emoticon">
            (^_^)b
        </div>
        <div class="footer-text">
            Well done!
        </div>
    </div>`,
    `<div style="">
        <div class="emoticon">
            ¯_(ツ)_/¯
        </div>
        <div class="footer-text">
            Wow, that was quick.
        </div>
    </div>`,
    `<div style="">
        <div class="emoticon">
            (;-;)
        </div>
        <div class="footer-text">
            Sorry, no more tweets left for you.
        </div>
    </div>`
];

export function sortListByRetweetCountDesc(tweets: Array<Tweet>): Array<Tweet> {
    return tweets.sort(sortByRetweetCountDesc);
}

/**
 * Filters out tweets with duplicating urls.
 * If several tweets contains duplicating urls, the tweet with most retweets will be returned, and others will be omitted.
 * 
 * @export
 * @param {Array<Tweet>} tweets
 * @returns {Array<Tweet>}
 */
export function checkUniqueLinks(tweets: Array<Tweet>): Array<Tweet> {
    let links = new Array<string>();
    let newTweets = new Array<Tweet>();

    tweets.forEach((tweet) => {
        if (tweet.entities && tweet.entities.urls && tweet.entities.urls.length !== 0) {
            if (links.indexOf(tweet.entities.urls[0].expanded_url.toLowerCase()) === -1) {
                links.push(tweet.entities.urls[0].expanded_url.toLocaleLowerCase());
                newTweets.push(tweet);
            }
        }
    });

    return newTweets;
}

/**
 * Returns original tweets with urls.
 * Tweets that are retweets, replies, contain quotes, or do not contain any urls will be omitted.
 * 
 * @export
 * @param {Array<Tweet>} tweets
 * @returns {Array<Tweet>}
 */
export function checkOriginalTweets(tweets: Array<Tweet>): Array<Tweet> {
    return tweets.filter((tweet) => {
        return !tweet.in_reply_to_status_id
            && !tweet.is_quote_status
            && !tweet.in_reply_to_screen_name
            && !tweet.retweeted_status
            && (tweet.entities.urls.length !== 0);
    });
}

/**
 * Returns tweets with retweets count higher or equal than a configured value.
 * 
 * @export
 * @param {Array<Tweet>} tweets 
 * @returns {Array<Tweet>} 
 */
export function checkMinRetweetCount(tweets: Array<Tweet>): Array<Tweet> {
    return tweets = tweets.filter((tweet) => {
        return tweet.retweet_count >= AppConfig.minRetweetCount;
    });
}

/**
 * Renders html file using tweets provided.
 * 
 * @param {Array<Tweet>} tweets
 * @returns {string}
 */
function render(tweets: Array<Tweet>): string {
    // get map of tweets grouped and sorted by date.
    let tweetsByDay = groupByDates(tweets).sortBy((value, key) => key);

    let tweetsItems = "";
    let index: number = 0;

    tweetsByDay.keySeq().forEach((key) => {
        tweetsItems += `<h2>${moment(key).format(DateFormat)}</h2>`;

        tweetsItems += `<ol class="list">`;

        let dayTweets = tweetsByDay.get(key).toArray();

        dayTweets.forEach((tweet) => {
            let text = tweet.text;

            tweet.entities.urls.forEach((url) => {
                text = text.replace(url.url, `<a href="${url.expanded_url}">${url.display_url}</a>`);
            });

            tweetsItems += `
                <li class="list-item">
                    ${text}
                </li>
            `;
        });

        const mostRetweetedTweet = getPopularByField(tweets, "retweet_count", 0, 1);
        const leastPopularTweets = getPopularByField(tweets, "retweet_count", 1, 4);

        tweetsItems += `</ol>`;

        if (index === 2) {
            tweetsItems += `
                <div class="fact">
                    <h3>  
                        Most <strong>retweeted</strong> of the week:
                    </h3>
                    ${getEmbededTweetsHtml(mostRetweetedTweet, true)}
                </div>
            `;
        }

        if (index === 5) {
            tweetsItems += `
                <div class="fact">
                    <h3>  
                        <strong>2<sup>nd</sup></strong>, <strong>3<sup>rd</sup></strong> and <strong>4<sup>th</sup></strong> places are:
                    </h3>
                    ${getEmbededTweetsHtml(leastPopularTweets, false)}
                </div>
            `;
        }

        index++;
    });

    tweetsItems += `<h2 class="end-text">{{FAREWELL_TEXT}}</h2>`;


    let tweetsList: string = `
        <h1>
            {{GREETING_TEXT}}
        </h1>
            ${tweetsItems}
    `;

    let html: string = `
        <!DOCTYPE html>
        <html lang="en">
            <head>
                <meta charset="utf-8">
                <meta http-equiv="X-UA-Compatible" content="IE=edge">
                <meta name="viewport" content="width=device-width, initial-scale=1">

                <style>
                    .list {
                        line-height: 1.4;
                        padding-left: 35px;
                    }

                    .list-item {
                        font-weight: normal;
                        font-family: "Roboto", sans-serif;
                        font-size: 16px;
                        padding-bottom: 10px;                        
                    }

                    .list-item a {
                        color: #2b7bb9;
                        text-decoration: none;
                    }

                    h1 {
                        color: #555;
                        font-size: 3em;
                        font-family: "Roboto Slab", Georgia, serif;
                        padding-left: 5px;
                    }

                    h2 {
                        font-size: 2em;
                        font-family: "Roboto Slab", Georgia, serif;
                        padding-left: 5px;
                        color: #666;
                    }

                    h3 {
                        margin-bottom: 0;
                        font-family: "Roboto", sans-serif;
                        font-size: 30px;
                    }

                    strong {
                        color: {{HIGHLIGHT_COLOR}};
                    }

                    .footer {
                        width: 100%;
                        text-align: center;
                    }

                    .emoticon {
                        font-size: 150px;
                        font-family: monospace;
                        color: #666;
                        letter-spacing: -15px;
                    }

                    .footer-text {
                        font-size: 40px;
                        font-family: "Roboto Slab", Georgia, serif;
                        color: #666;
                    }

                    .fact {
                        width: 50%;
                        margin: auto;
                        padding: 50px;
                    }

                    .end-text {
                        padding-bottom: 80px;
                        color: {{HIGHLIGHT_COLOR}};
                    }
                </style>

                <link href="https://fonts.googleapis.com/css?family=Roboto|Roboto+Slab" rel="stylesheet">
                <script async src="https://platform.twitter.com/widgets.js" charset="utf-8"></script>
            </head>
            <body>
                <div>
                    ${tweetsList}
                    <footer class="footer">
                        {{EMOTICON}}
                    </footer>
                </div>
            </body>
        </html>
    `;

    html = html
        .replace(/{{HIGHLIGHT_COLOR}}/g, getRandomValue(highlightColors))
        .replace(/{{GREETING_TEXT}}/g, getRandomValue(greetings))
        .replace(/{{FAREWELL_TEXT}}/g, getRandomValue(farewells))
        .replace(/{{EMOTICON}}/g, getRandomValue(emoticons))
        .replace(/{{TWEETS}}/g, tweets.length.toString())
        .replace(/{{DAYS}}/g, tweetsByDay.count().toString());

    return html;
}

/**
 * Groups tweets by day and saves as json files.
 * 
 * @export
 * @param {Array<Tweet>} tweets
 * @param {string} dataPath
 */
export function saveDataFiles(tweets: Array<Tweet>, dataPath: string) {
    let tweetsPerDay = Immutable.OrderedMap<Date, Immutable.List<Tweet>>();

    tweets.forEach((tweet) => {
        const createdDate = moment(new Date(tweet.created_at.replace("+0000 ", "") + " UTC")).utc().startOf("day").toDate();
        tweetsPerDay = tweetsPerDay.update(createdDate, Immutable.List<Tweet>(), list => list.push(tweet));
    });

    tweetsPerDay.keySeq().forEach((key) => {
        console.log(key.toDateString() + " : " + tweetsPerDay.get(key).count());

        const filePath = path.join(dataPath, `tweets for ${moment(key).format("YYYY-MM-DD")}.json`);
        fs.writeFile(filePath, JSON.stringify(tweetsPerDay.get(key), null, "\t"));
    });
}

/**
 * Groups tweets by reviewer, renders html files and saves them.
 * 
 * @export
 * @param {Array<Tweet>} tweets 
 * @param {Array<string>} reviewers 
 * @param {string} dataPath 
 */
export function saveHtmlFiles(tweets: Array<Tweet>, reviewers: Array<string>, dataPath: string): void {
    let tweetsPerReviewer = Immutable.OrderedMap<string, Immutable.List<Tweet>>();

    let index = 0;
    const reviewersListSize = reviewers.length;

    tweets.forEach((tweet) => {
        let reviewer = reviewers[index];

        tweetsPerReviewer = tweetsPerReviewer.update(reviewer, Immutable.List<Tweet>(), list => list.push(tweet));
        index = getNextIndex(index, reviewersListSize);
    });

    tweetsPerReviewer.keySeq().forEach((key) => {
        const filePath = path.join(dataPath, `tweets for ${key}.html`);

        console.log(key + " : " + tweetsPerReviewer.get(key).count());

        fs.writeFile(filePath, render(tweetsPerReviewer.get(key).toArray().sort(sortByRetweetCountDesc)));
    });
}

/**
 * Sort function for Tweet by retweets count.
 * 
 * @param {Tweet} a 
 * @param {Tweet} b 
 * @returns 
 */
function sortByRetweetCountDesc(a: Tweet, b: Tweet) {
    const aRetweets = a.retweet_count;
    const bRetweets = b.retweet_count;

    if (aRetweets > bRetweets) {
        return -1;
    } else if (aRetweets < bRetweets) {
        return 1;
    } else {
        return 0;
    }
}

/**
 * Returns tweets list groupped by day.
 * Created date is used.
 * 
 * @param {Array<Tweet>} tweets
 * @returns {Immutable.OrderedMap<Date, Immutable.List<Tweet>>}
 */
function groupByDates(tweets: Array<Tweet>): Immutable.OrderedMap<Date, Immutable.List<Tweet>> {
    let tweetsPerDay = Immutable.OrderedMap<Date, Immutable.List<Tweet>>();

    tweets.forEach((tweet) => {
        const createdDate = moment(new Date(tweet.created_at.replace("+0000 ", "") + " UTC")).utc().startOf("day").toDate();
        createdDate.setHours(0, 0, 0, 0);

        tweetsPerDay = tweetsPerDay.update(createdDate, Immutable.List<Tweet>(), list => list.push(tweet));
    });

    return tweetsPerDay;
}

/**
 * Returns a part of a tweets list sorted by a specified field.
 * 
 * @param {Array<Tweet>} tweets 
 * @param {string} field 
 * @param {number} from 
 * @param {number} to 
 * @returns {Array<Tweet>} 
 */
function getPopularByField(tweets: Array<Tweet>, field: string, from: number, to: number): Array<Tweet> {
    // create copy of an array, sort by retweets and return N top entries.
    return tweets.slice().sort(sortByRetweetCountDesc).slice(from, to);
}

/**
 * Returns the html for a list of tweets. Html template for embeded tweet is used.
 * 
 * @param {Array<Tweet>} tweets 
 * @param {boolean} showImages 
 * @returns {string} 
 */
function getEmbededTweetsHtml(tweets: Array<Tweet>, showImages: boolean): string {
    const userNameTemplate = "{USER_NAME}";
    const userScreenNameTemplate = "{USER_SCREEN_NAME}";
    const tweetIdTemplate = "{ID}";
    const tweetTextTemplate = "{TEXT}";
    const tweetCreatedAtTemplate = "{CREATED_AT}";

    const htmlTemplate: string = `
        <blockquote class="twitter-tweet" data-cards="${showImages ? '' : 'hidden'}" data-lang="en">
            <p lang="en" dir="ltr">
                ${tweetTextTemplate}
            </p>
            &mdash; ${userNameTemplate} (@${userScreenNameTemplate})
            <a href="https://twitter.com/${userScreenNameTemplate}/status/${tweetIdTemplate}}">
                ${tweetCreatedAtTemplate}
            </a>
        </blockquote>
    `;

    let tweetsHtml = "";

    tweets.forEach((tweet) => {
        tweetsHtml += htmlTemplate
            .replace(new RegExp(userNameTemplate, "g"), tweet.user.name)
            .replace(new RegExp(userScreenNameTemplate, "g"), tweet.user.screen_name)
            .replace(new RegExp(tweetIdTemplate, "g"), tweet.id_str)
            .replace(new RegExp(tweetTextTemplate, "g"), tweet.text)
            .replace(
            new RegExp(tweetCreatedAtTemplate, "g"),
            moment(new Date(tweet.created_at.replace("+0000 ", "") + " UTC")).format(DateFormat)
            );
    });

    return tweetsHtml;
}

/**
 * Returns a list of tweets from all json files from a specified folder.
 * 
 * @export
 * @param {string} dataFolder 
 * @returns {Promise<Array<Tweet>>} 
 */
export function getTweetsFromFiles(dataFolder: string): Promise<Array<Tweet>> {
    console.log(dataFolder);

    return new Promise<Array<Tweet>>((resolve, reject) => {
        const dataFiles = fs.readdirSync(dataFolder);
        let tweets = new Array<Tweet>();

        dataFiles.forEach((fileName) => {
            if (fileName.endsWith(".json")) {
                const filePath = path.join(dataFolder, fileName);
                console.log("F: " + filePath);

                const dayTweets = JSON.parse(fs.readFileSync(filePath, "utf8"));

                tweets = tweets.concat(dayTweets);
            }
        });

        resolve(tweets);
    });
}

/**
 * Returns a random value from the specified list.
 * 
 * @param {Array<string>} list
 * @returns {string}
 */
function getRandomValue(list: Array<string>): string {
    return list[Math.floor(Math.random() * list.length)];
}

/**
 * Return next index in the array. If the last index was the max value, returns 0.
 * 
 * @param {number} currentIndex 
 * @param {number} listSize 
 * @returns 
 */
function getNextIndex(currentIndex: number, listSize: number) {
    if (currentIndex >= listSize - 1) {
        return 0;
    } else {
        return currentIndex + 1;
    }
}