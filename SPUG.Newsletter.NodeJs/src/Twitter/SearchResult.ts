import Tweet from "./Tweet";
import SearchMetadata from "./SearchMetadata";

/**
 * SearchResult returned by the Twitter search API.
 * 
 * @export
 * @class SearchResult
 */
export default class SearchResult {
    statuses: Array<Tweet>;
    search_metadata: SearchMetadata;
}