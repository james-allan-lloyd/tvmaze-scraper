# TVMaze Scraper
Scrape and serve data from TVMaze.

This project uses tye to run (https://github.com/dotnet/tye). It should be able
to be run manually, but you'll need to set the following environment variable
with the correct values for your mongo db instance.

```
CONNECTIONSTRINGS__MONGODB=mongodb://root:password@localhost:27016
```

## TODO
- [x] initial project setup with tye
- [x] get a page of shows from the https://api.tvmaze.com/shows
- [x] get cast list for the shows /shows/:id/cast
- [x] setup testing
- [x] create showCast repository
- [x] generate mongo documents
- [x] implement endpoint
- [x] time page processing and log it
- [x] add pagination to endpoint
- [x] add api tests
- [x] api page parameter should be 1 or greater
- [x] api size parameter should be 1 or greater
- [ ] fix _id to id
	- `.Project("{_id: 0, id: \"$_id\", name: \"$name\", cast: \"$cast\"}")`
	  works for show id but not person... :/
	- map on conversion from bson to json?
- [x] process all pages
- [ ] ~~cache person to cut down on number of queries?~~
	- should be cached by the edge server, they say...
	- are we asking for cached results?
	- we're not asking for person, we're asking for cast
- [ ] process all shows, then all people
- [ ] use fluent assertions for api, return more than 1 error
- [ ] ~~convert birthday to datetime~~
	- already in correct format
- [x] sort on datetime in backend?
- [ ] async enumerable returns?
- [ ] handle people with null birthdays (id 6 Nicholas Strong)?
- [ ] test backOff behavior
- [ ] make common app framework
- [ ] implement parallel processing?
- [ ] implement rate limiting?
- [ ] implement updating
- [ ] crash if we get too many unexpected status codes?
- [ ] test client status codes


# Decisions
## Storage
- Store documents as view optimized, denormalised data
	- Easy to update
	- Assume the tv maze data is consistent itself
	- Alternative: normalise the person data, join on serve



# Out of Scope
* Auth
* Swagger