'use strict';

var fs = require('fs');
var BitlyAPI = require("node-bitlyapi");
var Bitly = new BitlyAPI({
    client_id: "some",
    client_secret: "some"  
});

//Bitly.setAccessToken('Enter your accessToken here.It can be generated on https://bitly.com/a/oauth_apps');
Bitly.setAccessToken('');

var regEpxForURLs = /https?:\/\/(www\.)?[-a-zA-Z0-9@:%._\+~#=]{2,256}\.[a-z]{2,6}\b([-a-zA-Z0-9@:%_\+.~#?&//=]*)/g,
    jsonAsString = fs.readFileSync('digest.json').toString(),
    jsonAsObj = JSON.parse(fs.readFileSync('digest.json')),
    longURLs = jsonAsString.match(regEpxForURLs),
    shortURLs = [],
    curIndex = 0;


Bitly.createBundle({private: true, title: jsonAsObj.digestTitle}, function(err, result) {
    var res = JSON.parse(result);
    var bundLink = res.data.bundle.bundle_link;

    function genNewJson () {
        Bitly.shorten({longUrl: longURLs[curIndex]}, function(err, results) {
            var res = JSON.parse(results);
            var shortURL = res.data.url;
            shortURLs.push(shortURL);

            Bitly.addLinkToBundle({bundle_link: bundLink, link: shortURL.toString()}, function(err, result) {});

            if (shortURLs.length === longURLs.length) {
                var count = 0;
                var newString = jsonAsString.replace(regEpxForURLs, function () {
                    return shortURLs[count++];
                });
                fs.writeFile('digestBitly.json', newString);
            };
        });
        curIndex++;
        if (curIndex === longURLs.length) {
            clearInterval(intervalId);
        };
    };
    var intervalId = setInterval(genNewJson, 10000);
});

console.log('Some magic work right now ...')
