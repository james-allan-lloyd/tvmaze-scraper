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
- [x] process all pages
- [x] ~~cache person to cut down on number of queries?~~
	- should be cached by the edge server, they say...
	- are we asking for cached results?
	- we're not asking for person, we're asking for cast
- [x] ~~process all shows, then all people~~
	- people don't know what shows they were in, still need to query cast
	- there's about 341543 people in the db, as opposed to 58937. More efficient
	  to iterate show cast lits
- [x] ~~convert birthday to datetime~~
	- already in correct format
- [x] sort on datetime in backend?
- [x] handle people with null birthdays (id 6 Nicholas Strong)?
	- left as null
- [x] test backOff behavior
	- backing off for 10 seconds to optimize throughput to 40 requests per 11
	  seconds or so (around 3.5 requests per second, which is more than what
	  they advertise) for non-edge cached requests.
- [x] implement updating
	- only need to do show updates
- [x] run updates regularly
- [x] only run updates that haven't been applied (store last timestamp)
- [ ] fix _id to id
	- `.Project("{_id: 0, id: \"$_id\", name: \"$name\", cast: \"$cast\"}")`
	  works for show id but not person... :/
	- map on conversion from bson to json?
- [ ] remove extra call in update (do it all with the ?embed=cast endpoint)
- [ ] make common app framework
- [ ] implement parallel processing?
- [ ] async enumerable returns?
- [ ] implement rate limiting?
- [ ] crash if we get too many unexpected status codes?
- [ ] test client status codes
- [ ] use fluent assertions for api, return more than 1 error


# Decisions
## Assumptions
- Mostly going to read data in the way described. If we have to provide
  different views, consider adding more tables/joins/etc.
## Storage
- Store documents as view optimized, denormalised data
	- Easy to update
	- Assume the tv maze data is consistent itself
	- Alternative: normalise the person data, join on serve



# Out of Scope
* Auth
* Swagger