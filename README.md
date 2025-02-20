# URL Shortener
This small web service transforms a given url into a shortened alias with a consistent length.
When navigating to said shortened url, the client is redirected to the original.

# To Run
Assuming .NET 8 is installed on your machine:
1. `git clone https://github.com/MiahDrao97/UrlShortener.git`
2. `cd UrlShortener`
3. `dotnet run --project .\UrlShortener\UrlShortener.csproj`

To run tests, simply execute `dotnet test` in the root directory.

And there ya go! The `launchSettings.json` file configures which localhost port the site will run on (default is 5136).

## Features
The index page allows the user to create a new shortened url.
Simply put a url in the input and hit 'Enter' or the 'Submit' button.
The Urls page displays all saved urls and their shortened versions as well as some metadata.
When navigating to the shortened url, the user shall be redirected to the full url.
If a shortened url is not on record, the user will be directed to a not-found page.

## Design Decisions
The Urls page makes a server call on every search, sort, next page, and previous page.
In some ways, that is useful so that the user can see the top X of a given filter or ordering with the *entire* data collection in view.
However, this is not scalable if this service were to hold thousands of urls.
At this point, the filtration is fine, but if it were to scale into the realm of thousands, then other filtration should take place, such as only displaying urls belonging to the current user.

Hits on urls are recorded in a background service.
Related to scaling, I didn't want to block the client for one last database call to record analytics, but I also didn't want to simply spawn a new thread either.
If we had a few hundred concurrent users, thread exhaustion is definitely possible.
So instead, the analytics are emitted to a channel with a background service constantly listening to said channel and applying analytics as they come in.

On code level, you may observe that I prefer returning errors as values.
This may not be "ergonomic" from a developer point of view (meaning you have to write more code and holy-nested-generics-batman), but I believe writing less code in the name of "developer experience" is largely a farce.
Errors as values forces you to handle errors in the location they're returned (locality of behavior).
In error-prone systems (like HTTP endpoints), returning a result over branching by exception results in better performance since validation is a regular control flow.

The unit/integration-testing harness is one that I developed for own personal projects.
Using an in-memory database and live service scope will test the server in its entirety rather than against mocks.
I do believe unit-testing is especially helpful for those edge-case bug crevices, but it doesn't test the larger system at work.

### Other Things I Would Add
- To prevent DOS-attacks, a request limit should be enforced for any given client unless they create an account.
Registered accounts would get unlimited access while other clients can only hit the API X number of times within a certain window.
I would use Oauth 2.0 and authorize against a few supported identity providers (like Google, for instance).
- On the Urls page, I'd like to be able to copy a shortened url just by clicking it, displaying a quick message saying it was copied.
- SQLite persistence layer rather than in-memory.
- When redirecting to the full url, a page should briefly display that the user is being redirected.

## Time Log
### First Commit
Adding boilerplace and starting backend services - 2.66 hours
### Some ergonomic things
Specifically around the result types - 40 min
### Wrapped up services
30 min
### Beginning frontend work
30 min
### Getting the hasing algo super sorted out before enter the maze of razor pages
Dev-testing via Postman to ensure correctness - 1 hour
### Creating unit tests
First few unit tests - 30 min
### Forgot to add these
Literally forgot `git add --all` on changes from the night before - 1 min
### I think the create urls page is in a stable place
Focused work on the index page that allows the user to creat urls - 1 hour
### Got the FE as good as I think I can without spending wayyy more time
Urls page and other aesthetic things - 2 hours
### Got telemetry background service running
Also added the analytics columns to the Urls page - 30 min
### Fix existing unit tests
I broke something - 10 min
### Re-added url transformer for testing
Re-introducing an old class and interface that I had deleted because it helps increase the test coverage - 1 hour
### Done with testing
Completing unit tests - 2 hours
