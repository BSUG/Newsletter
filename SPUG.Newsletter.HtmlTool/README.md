Newsletter HTML Generator Tool
==============

This tool is used by Belarus SharePoint Community Enthusiasts  to build a weekly HTML newsletter digest:
http://belarussharepoint.wordpress.com

For creating a new episode all you need is create a JSON structure of your episode and run the following command:
```
Grunt --force
```
Make sure, you have Node.js and Grunt npm module installed:
```
npm install -g grunt-cli
```
File structure:
====
1. *digest.json* - is a pre-defined template for a standard SharePoint newsletter digest.
2. *digest-template.json* - is a sample JSON for one of the real SharePoint newsletter episode published in February, 2016.
3. *digestBitly.json* - used to create a JSON template based on the digest.json with shorted bit.ly links (currently, not working, sorry for that :))
4. *digest.jade* - is a template for final HTML.
5. *digest_tinyletter_narrow.jade* - If you're having issues with saving html in tinyletter (e.g. tinyletter eats all your pics! Or breaks styles!), try this alternative JADE file. Thanks to [w0lya](https://github.com/w0lya) for this fix!  
6. *app.js* - is a main file which does the whole job! Please, make sure you enter your real Bitly accessToken in the following line here:

```
Bitly.setAccessToken(''); //it will work, once we fix that! ;)
```

Enjoy!

