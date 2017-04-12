import * as moment from "moment";
import * as fs from "fs";

import { TwitterClient, SearchQuery, Tweet } from "./Twitter";
import AppConfig from "./config";
import {
    checkOriginalTweets,
    checkMinRetweetCount,
    checkUniqueLinks,
    checkJobOffers,
    saveDataFiles,
    saveHtmlFiles,
    getTweetsFromFiles,
    sortListByRetweetCountDesc
} from "./Utilities";

// list of functions that filter tweets to refine search results.
const filterFunctions: Array<(tweets: Array<Tweet>) => Array<Tweet>> = [
    sortListByRetweetCountDesc,
    checkOriginalTweets,
    checkMinRetweetCount,
    checkUniqueLinks,
    checkJobOffers];

// get params from the command line
const useCachedDataString: string = process.env.USE_CACHED || "";
const useCachedData: boolean = useCachedDataString.toLowerCase() === "true";

let key: string = process.env.KEY || "";
key = key.replace(/\'/g, "");

let secret: string = process.env.SECRET || "";
secret = secret.replace(/\'/g, "");

if (useCachedData || (key && secret)) {
    main();
} else {
    console.log(`KEY and SECRET are not specified. 
    Please make sure to specify them in either package.json for the \"start\" script or via a command line.`);
}


function main(): void {
    // ensure that the data folder exists.
    if (!fs.existsSync(AppConfig.dataPath)) {
        fs.mkdirSync(AppConfig.dataPath);
    }

    searchTweets(useCachedData, key, secret).then((tweets) => {
        console.log("Tweets before filtering: " + tweets.length);

        // refine search results.
        filterFunctions.forEach((filterFunction) => {
            tweets = filterFunction(tweets);
            console.log(`Tweets after the ${filterFunction.name} filter: ` + tweets.length);
        });

        // save json files with tweets by day.
        console.log("saving json files");
        saveDataFiles(tweets, AppConfig.dataPath);
        // generate html files for review.
        console.log("rendering html files.");
        saveHtmlFiles(tweets, AppConfig.reviewers, AppConfig.dataPath);

        console.log("done.");
    }).catch((error) => {
        console.log(error);
    });
}

/**
 * Run Twitter search request using TwitterClient.
 * Local files can be used as cache by specifying useCachedData flag.
 * 
 * @param {boolean} [useCachedData=false]
 * @returns {Promise<Array<Tweet>>}
 */
function searchTweets(useCachedData: boolean, consumerKey: string, consumerSecret: string): Promise<Array<Tweet>> {
    // if cache should be used, read tweets from local file.
    if (useCachedData) {
        return getTweetsFromFiles(AppConfig.dataPath);
    }

    return new Promise((resolve, reject) => {
        let twitterClient: TwitterClient = new TwitterClient();

        const query: SearchQuery = {
            searchTerms: AppConfig.searchTerms,
            lang: AppConfig.lang,
            resultType: AppConfig.resultType,
            count: AppConfig.count,
            until: moment().subtract(AppConfig.untilDays, "days").toDate(),
            since: moment().subtract(AppConfig.sinceDays, "days").toDate(),
            filter: AppConfig.filter
        };

        twitterClient.login(consumerKey, consumerSecret, true).then(() => {
            console.log("logged in");

            twitterClient.search(query)
                .then((tweets) => {
                    console.log("search done");
                    console.log("total tweets found: " + tweets.length);
                    resolve(tweets);
                })
                .catch((error) => {
                    console.log(error);
                    reject(error);
                });
        }).catch((error) => {
            console.log(error);
            reject(error);
        });
    });
}