# TVMaze Scraper
Scrape and serve data from TVMaze.

## TODO
[x] initial project setup with tye
[x] get a page of shows from the https://api.tvmaze.com/shows
[x] get cast list for the shows /shows/:id/cast
[x] setup testing
[x] create showCast repository
[x] generate mongo documents
[] implement endpoint
[] add pagination to endpoint
[] handle people with null birthdays (id 6 Nocholas Strong)
[] convert birthday to datetime
[] sort on datetime in backend?
[] time page processing and log it
[] test backOff behavior
[] make common app framework
[] process all pages
[] implement parallel processing?
[] implement rate limiting?
[] implement updating
[] crash if we get too many unexpected status codes?


## Out of Scope
* Auth
* Swagger