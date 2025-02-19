# URL Shortener
This small web service transforms a given url into a shortened alias with a consistent length.
When navigating to said shortened url, the client is redirected to the original.

# To Run
Assuming .NET 8 is installed:
1. `git clone https://`
2. `cd UrlShortener`
3. `dotnet run --project .\UrlShortener\UrlShortener.csproj`

And there ya go! Check the logs which localhost port it will run from.

## Features

### Other Things I Would Add

- To prevent DOS-attacks, I want to enforce a request limit for a given client unless they create an account.
Registered accounts would get unlimited access while other clients can only hit the API X number of times in a given window.

## Time Log

