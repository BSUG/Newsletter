import * as RequestPromise from "request-promise";
import * as Request from "request";
import * as BigInteger from "big-integer";
import * as moment from "moment";

import Tweet from "./Tweet";
import SearchResult from "./SearchResult";
import SearchQuery from "./SearchQuery";

const BaseApiUrl: string = "https://api.twitter.com/";
const SearchApiEndpoint: string = "1.1/search/tweets.json";
const AuthApiEndpoint: string = "oauth2/token";

// date format required by the Twitter API
const DateFormat: string = "YYYY-MM-DD";

/**
 * Answer from the Twitter authentication API.
 * 
 * @export
 * @interface BearerTokenResponse
 */
export interface BearerTokenResponse {
    token_type: string;
    access_token: string;
}

/**
 * Implements access to the Twitter Search REST API.
 * 
 * @export
 * @class TwitterClient
 */
export default class TwitterClient {
    /**
     * Defines whether the login has been performed.
     * 
     * @private
     * @type {boolean}
     * @memberOf TwitterClient
     */
    private isLoggedIn: boolean;

    /**
     * Stores the authentication information required for REST API calls.
     * 
     * @private
     * @type {Request.OAuthOptions}
     * @memberOf TwitterClient
     */
    private oAuthOptions: Request.OAuthOptions;

    constructor() {
        this.isLoggedIn = false;
    }

    /**
     * Authenticates the app using the application-only auth flow. See https://dev.twitter.com/oauth/application-only for details.
     * 
     * @param {string} consumerKey 
     * @param {string} consumerSecret 
     * @param {boolean} isAppAuth 
     * @returns {Promise<BearerTokenResponse>} 
     * 
     * @memberOf TwitterClient
     */
    public login(consumerKey: string, consumerSecret: string, isAppAuth: boolean): Promise<BearerTokenResponse> {
        return new Promise((resolve, reject) => {
            if (!isAppAuth) {
                reject("Only the Application-only authentication is supported.");
            }

            const credentials: string = new Buffer(consumerKey + ":" + consumerSecret).toString("base64");
            const options: Request.CoreOptions = {
                headers: {
                    "Authorization": "Basic " + credentials,
                    "Content-Type": "application/x-www-form-urlencoded;charset=UTF-8"
                },
                body: "grant_type=client_credentials",
                json: true,
            };

            RequestPromise.post(BaseApiUrl + AuthApiEndpoint, options).then((result: BearerTokenResponse) => {
                const accessToken: string = result.access_token;

                this.oAuthOptions = {
                    consumer_key: consumerKey,
                    consumer_secret: consumerSecret,
                    verifier: accessToken
                };

                this.isLoggedIn = true;
                resolve(result);
            }).catch((error) => {
                reject(error);
            });
        });
    }

    /**
     * Runs the Twitter search query using the Twitter REST API.
     * Supports working with timelines (see https://dev.twitter.com/rest/public/timelines for details).
     * I.e. performs multiple search queries to get all the requested results.
     * 
     * @param {SearchQuery} query
     * @returns {Promise<Array<Tweet>>}
     * 
     * @memberOf TwitterClient
     */
    public search(query: SearchQuery): Promise<Array<Tweet>> {
        return new Promise<Array<Tweet>>((resolve, reject) => {
            if (!this.isLoggedIn) {
                reject("Please authenticate first using the login function.");
            }

            const searchRequestOptions = this.buildSearchQueryRequestOptions(query);
            // add authentication info to the request.
            searchRequestOptions.oauth = this.oAuthOptions;

            this.doSearch(searchRequestOptions, "").then((tweets) => {
                resolve(tweets);
            }).catch((error) => {
                reject(error);
            });
        });
    }

    /**
     * Performs search query untill max_id exists.
     * 
     * @param {Request.CoreOptions} searchRequestOptions
     * @param {string} maxId
     * @param {Immutable.List<Tweet>} tweets
     * @returns {Promise<Immutable.List<Tweet>>}
     * 
     * @memberOf TwitterClient
     */
    private doSearch(searchRequestOptions: Request.CoreOptions, maxId: string): Promise<Array<Tweet>> {
        return new Promise<Array<Tweet>>((resolve, reject) => {
            // if max_id param is specified, add it to the search request options
            if (maxId) {
                searchRequestOptions.qs.max_id = maxId;
            }

            // perform search request
            RequestPromise.get(BaseApiUrl + SearchApiEndpoint, searchRequestOptions).then((searchResult: SearchResult) => {
                console.log(`searching with max_id ${maxId}... Got tweets: ${searchResult.statuses.length}`);

                const bigId = BigInteger("1e100");
                const previousMaxId: BigInteger = maxId ? BigInteger(maxId) : bigId;
                let currentMaxId: BigInteger = this.getMinId(searchResult.statuses, previousMaxId);

                if (previousMaxId.equals(currentMaxId)) {
                    // no more results to retrieve
                    resolve(searchResult.statuses);
                    return;
                } else {
                    // get max_id for next request
                    currentMaxId = currentMaxId.minus(1);
                    let nextMaxId: string = currentMaxId.toString();

                    this.doSearch(searchRequestOptions, nextMaxId).then((subTweets) => {
                        // concatenate results from recursive requests and the current request, and resolve promise.
                        const tweets = subTweets.concat(searchResult.statuses);
                        resolve(tweets);
                    }).catch((error) => {
                        reject(error);
                    });
                }
            }).catch((error) => {
                reject(error);
            });
        });
    }

    /**
     * Gets the minimum id from the list of tweets.
     * 
     * @private
     * @param {Array<Tweet>} tweets 
     * @param {BigInteger} initialMin 
     * @returns {BigInteger} 
     * 
     * @memberOf TwitterClient
     */
    private getMinId(tweets: Array<Tweet>, initialMin: BigInteger): BigInteger {
        let minId: BigInteger = initialMin;

        tweets.forEach((tweet) => {
            const id = BigInteger(tweet.id_str);

            if (id.compare(minId) < 0) {
                minId = id;
            }
        });

        return minId;
    }

    /**
     * Builds the search query request based on the specified options.
     * 
     * @private
     * @param {SearchQuery} searchQuery 
     * @returns {Request.CoreOptions} 
     * 
     * @memberOf TwitterClient
     */
    private buildSearchQueryRequestOptions(searchQuery: SearchQuery): Request.CoreOptions {
        // combine search terms into a query string.
        const queryText: string = searchQuery.searchTerms.reduce((query, term) => {
            if (term.split(" ").length > 1) {
                return `${query} OR \"${term}\"`;
            } else {
                return `${query} OR ${term}`;
            }
        });

        const sinceDateString = moment(searchQuery.since).format(DateFormat);
        const untilDate = moment(searchQuery.until);
        // if until date is today, do not specify it in the query and use empty string instead.
        const untilDateString = moment().isSame(untilDate, "day") ? "" : untilDate.format(DateFormat);

        const options: Request.CoreOptions = {
            qs: {
                q: queryText,
                lang: searchQuery.lang,
                result_type: searchQuery.resultType,
                count: searchQuery.count,
                until: untilDateString,
                since: sinceDateString,
                filter: searchQuery.filter
            },
            json: true
        };

        return options;
    }
}