/**
 * Configuration for the app.
 * 
 * @export
 * @class AppConfig
 */
export default class AppConfig {
    /**
     * Path where to store the retrieved tweets.
     * 
     * @static
     * @type {string}
     * @memberOf AppConfig
     */
    public static dataPath: string = "_tweets";

    /**
     * List of terms to search
     * 
     * @static
     * @type {Array<string>}
     * @memberOf AppConfig
     */
    public static searchTerms: Array<string> = ["SharePoint", "Office365", "SPFX", "Office 365"];

    /**
     * List of reviewers.
     * For each reviewer a html file will be generated for a review.
     * 
     * @static
     * @type {Array<string>}
     * @memberOf AppConfig
     */
    public static reviewers: Array<string> = ["Dmitry", "Alex", "Olya", "Natally", "Andrew"];

    /**
     * Filter for search results by a minimum retweets count.
     * 
     * @static
     * 
     * @memberOf AppConfig
     */
    public static minRetweetCount = 3;

    /**
     * Language filter
     * 
     * @static
     * @type {string}
     * @memberOf AppConfig
     */
    public static lang: string = "en";
    /**
     * Type of results to return.
     * 
     * @static
     * @type {("recent" | "popular" | "mixed")}
     * @memberOf AppConfig
     */
    public static resultType: "recent" | "popular" | "mixed" = "recent";
    /**
     * Search page size
     * Maximum value is 100.
     * @static
     * @type {number}
     * @memberOf AppConfig
     */
    public static count: number = 100;
    /**
     * Only results older than untilDays will be returned. Until is counting from today.
     * I.e. 0 - including today, 1 - not earlier than yesterday, etc.
     * 
     * @static
     * @type {number}
     * @memberOf AppConfig
     */
    public static untilDays: number = 0;
    /**
     * Only results newer that sinceDays will be returned. Since is counting from today.
     * Please note, that Twitter index stores about last 6-9 days of results max.
     * 
     * @static
     * @type {number}
     * @memberOf AppConfig
     */
    public static sinceDays: number = 6;

    /**
     * Additional filter to apply.
     * 
     * @static
     * @type {("safe" | "media" | "native_video" | "periscope" | "vine" | "images" | "twimg" | "links")}
     * @memberOf AppConfig
     */
    public static filter: "safe" | "media" | "native_video" | "periscope" | "vine" | "images" | "twimg" | "links" = "links";
}