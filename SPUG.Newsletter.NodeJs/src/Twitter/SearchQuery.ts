/**
 * Describes the Twitter Search Query
 * 
 * @export
 * @interface SearchQuery
 */
interface SearchQuery {
    /**
     * List of terms to search. Result will contain one or more search terms, i.e. terms combined using OR operator.
     * 
     * @type {Array<string>}
     * @memberOf TwitterSearchQuery
     */
    searchTerms: Array<string>;
    /**
     * Restricts tweets to the given language, given by an ISO 639-1 code. Language detection is best-effort.
     * 
     * @type {string}
     * @memberOf TwitterSearchQuery
     */
    lang: string;
    /**
     * Specifies what type of search results you would prefer to receive. Valid values include:
     * - mixed : Include both popular and real time results in the response.
     * - recent : return only the most recent results in the response.
     * - popular : return only the most popular results in the response.
     * 
     * @type {("recent" | "popular" | "mixed")}
     * @memberOf TwitterSearchQuery
     */
    resultType: "recent" | "popular" | "mixed";
    /**
     * The number of tweets to return per page, up to a maximum of 100.
     * 
     * @type {number}
     * @memberOf TwitterSearchQuery
     */
    count: number;
    /**
     * Returns tweets created before the given date.
     * Keep in mind that the search index has a 7-day limit. In other words, no tweets will be found for a date older than one week.
     * 
     * @type {Date}
     * @memberOf TwitterSearchQuery
     */
    until: Date;
    /**
     * Returns tweets created after the given date.
     * Keep in mind that the search index has a 7-day limit. In other words, no tweets will be found for a date older than one week.
     * 
     * @type {Date}
     * @memberOf TwitterSearchQuery
     */
    since: Date;
    /**
     * Specifies additional filter for returned tweets. Valid values include:
     * - safe : Tweets marked as potentially sensitive removed.
     * - media: Tweet contains an image or video.
     * - native_video : Tweet contains an uploaded video, Amplify video, Periscope, or Vine.
     * - periscope : Tweet contains a Periscope video URL.
     * - vine : Tweet contains a Vine.
     * - images : Tweet contains links identified as photos, including third parties such as Instagram.
     * - twimg : Tweet contains a pic.twitter.com link representing one or more photos.
     * - links : Tweet contains linking to URL.
     * 
     * @type {("safe" | "media" | "native_video" | "periscope" | "vine" | "images" | "twimg" | "links")}
     * @memberOf TwitterSearchQuery
     */
    filter?: "safe" | "media" | "native_video" | "periscope" | "vine" | "images" | "twimg" | "links";
    /**
     * Returns results with an ID less than (that is, older than) or equal to the specified ID.
     * 
     * @type {string}
     * @memberOf TwitterSearchQuery
     */
    maxId?: string;
}

export default SearchQuery;