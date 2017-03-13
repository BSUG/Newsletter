/**
 * SearchMetadata returned with each search API response
 * Note that string type is used for both id and id_str fields (e.g. max_id and max_id_str).
 * This is done cause Twitter id is a 64bit digit that is not natively supported by Node.
 * 
 * @export
 * @class SearchMetadata
 */
export default class SearchMetadata {
    completed_in: number;
    max_id: string;
    max_id_str: string;
    next_results: string;
    query: string;
    refresh_url: string;
    count: number;
    since_id: string;
    since_id_str: string;
}