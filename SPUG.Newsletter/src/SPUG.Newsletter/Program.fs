open System
open System.Net
open System.Text.RegularExpressions
open System.Globalization
open TweetSharp

// Search configuration
open System.Configuration

#if INTERACTIVE // Values used
let key       = "wk8ICcCgJY2iXa6WS0PDZiUYC"
let secret    = "AwA9G4fnwfPciCANx6QuWBaj009dwvAHFx3UW1XQD6yadH88py"
let queries   = [|"#Office365"|]
let minRetweetCount = 2
#else
let key       = ConfigurationManager.AppSettings.["consumerKey"]
let secret    = ConfigurationManager.AppSettings.["consumerSecret"]
let queries   = ConfigurationManager.AppSettings.["Queries"].Split([|';'|])
let minRetweetCount = Int32.Parse(ConfigurationManager.AppSettings.["MinRetweetCount"])
let participants   = ConfigurationManager.AppSettings.["Participants"].Split([|';'|])
#endif



// ------------ Twitter authentication ---------------------

// Pass your app credentials to the service
let service = new TwitterService(key, secret)
// Step 1 - Retrieve an OAuth Request Token
let requestToken = service.GetRequestToken()
// Step 2 - Redirect to the OAuth Authorization URL
let uri = service.GetAuthorizationUri(requestToken)
System.Diagnostics.Process.Start(uri.ToString()) |> ignore
// Step 3 - Exchange the Request Token for an Access Token
printf "Please, enter PIN (from browser window):"
let verifier = Console.ReadLine()
let access = service.GetAccessToken(requestToken, verifier)
// Step 4 - User authenticates using the Access Token
service.AuthenticateWith(access.Token, access.TokenSecret)


// Export tweets by query
let getTweets query =
    let rec collect (maxId:Nullable<int64>) attempt =
        let options = SearchOptions(Q = query, Count = Nullable(200),
                                    Resulttype = Nullable(TwitterSearchResultType.Recent))
        if (maxId.HasValue) then options.MaxId <- maxId
        printfn "Loading %s under id %A" query maxId
        let results =
            match service.Search(options) with
            | null -> []
            | res ->  res.Statuses |> Seq.toList
        printfn "\t Loaded %d tweets" results.Length
        if (results.Length <= 1)
        then
            if attempt > 0
            then
                printfn "Magic sleep for 20sec to be sure that server does not have results..."
                Threading.Thread.Sleep(TimeSpan.FromSeconds(20.0))
                collect maxId (attempt-1)
            else
                []
        else
            let lastTweet = results |> Seq.minBy (fun x -> x.Id)
            if (not maxId.HasValue || lastTweet.Id < maxId.Value)
                then results |> List.append (collect (Nullable(lastTweet.Id)) 1)
                else results
    collect (Nullable()) 1 |> List.rev

let urlRegexp = Regex("((mailto\:|(news|(ht|f)tp(s?))\://){1}\S+)", RegexOptions.IgnoreCase)
let filterUniqLinks (tweets: TwitterStatus list) =
    let hash = new System.Collections.Generic.HashSet<string>();
    tweets |> List.fold
        (fun acc t ->
            let mathces = urlRegexp.Matches(t.Text)
            if (mathces.Count = 0) then acc
            else let urls =
                    [0 .. (mathces.Count-1)]
                    |> List.map (fun i -> mathces.[i].Value)
                    |> List.filter (hash.Contains >> not)
                 if (List.isEmpty urls) then acc
                 else urls |> List.iter(hash.Add >> ignore)
                      t :: acc)
        [] |> List.rev

let startDate = DateTime.Now - TimeSpan.FromDays(7.0)
let allTweets =
    List.ofArray queries
    |> List.map getTweets
    |> List.concat
    |> List.filter(fun x-> x.CreatedDate > startDate)

let filteredTweets =
    allTweets
    |> List.filter (fun t->  t.RetweetCount >= minRetweetCount)
    |> List.sortBy (fun t -> t.CreatedDate)
    |> filterUniqLinks

printfn "All tweets count : %d" allTweets.Length
printfn "Filtered tweets count : %d" filteredTweets.Length

let printTweetsInHtml filename (tweets: TwitterStatus list) =
    let formatTweet (text:string) =
        let matches = urlRegexp.Matches(text)
        seq {0 .. (matches.Count-1)}
            |> Seq.fold (
                fun (t:string) i ->
                    let url = matches.[i].Value
                    t.Replace(url, (sprintf "<a href=\"%s\" target=\"_blank\">%s</a>" url url)))
                text
    let rows =
        tweets
        |> List.mapi (fun i t ->
            let id = (tweets.Length - i)
            let text = formatTweet(t.Text)
            let tweetUrl = sprintf "https://twitter.com/%s/status/%d" t.Author.ScreenName t.Id
            sprintf "<table id=\"%d\"><tr><td rowspan=\"2\" width=\"30\">%d</td><td rowspan=\"2\" width=\"80\"><a href=\"javascript:remove('%d')\">Remove</a><td rowspan=\"2\"><a href=\"https://twitter.com/%s\" target=\"_blank\"><img src=\"%s\"/></a></td><td><b>%s</b></td></tr><tr><td>Created : <a href=\"%s\" target=\"_blank\">%s</a> <br></td></tr></table>"
                    id id id t.Author.ScreenName t.Author.ProfileImageUrl text tweetUrl (t.CreatedDate.ToString()))
        |> List.fold (fun s r -> s+"&nbsp;"+r) ""
    let html = sprintf "<html><head><script>function remove(id){return (elem=document.getElementById(id)).parentNode.removeChild(elem);}</script></head><body>%s</body></html>" rows
    System.IO.File.WriteAllText(filename, html)

let rec split length (xs: list<'T>) =
  [ yield List.truncate length xs
    if xs.Length > length then
        yield! split length (List.skip length xs) ]

filteredTweets
|> split ((filteredTweets.Length + participants.Length - 1) / participants.Length) // Chunk size
|> List.zip (List.ofArray participants)
|> List.filter (fun (_,tweets) -> tweets.Length > 0)
|> List.iter (fun (name, tweetsForUser: TwitterStatus list) ->
    let fileName = sprintf "Tweets_for_%s__StartingFrom_%s.html" name (startDate.ToString("yyyy_MM_dd"))
    printTweetsInHtml fileName tweetsForUser
)

[<EntryPoint>]
let main argv =
    printfn "Press any key to exit"
    Console.ReadKey() |> ignore
    0
