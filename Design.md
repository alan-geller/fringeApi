# Design

## System Requirements

- The system should not impose restrictions on the user interface design.
- The system should not require keeping a socket open at all times.
- The system should support multithreaded access. In particular, it should allow a single writer
  to access the system exclusively without unduly delaying readers.
- The system should use UTC timestamps for all date and time information because that is the
  Edinburgh local time.
- The system should allow all festival information to be saved to or loaded from local storage.

We assume robust garbage collection and so don't worry about limiting pointers between objects, 
nor about avoiding pointer cycles.

## Styles and Conventions

- Use PascalCase for all class names and properties.
- Use camelCase for local variables and method parameters.
- Use UTC timestamps for all date and time information.
- Use JSON objects for storing additional information in the `Extra` property of each class.
- In general, prefer automatic properties over instance variables for class design.

## Object Model

The primary classes in the festival dataset are `Show`, `Performance`, and `Venue`.
In addition, there is a `Festival` class that acts as a container for the `Shows`, `Performances`, 
and `Venues`, and an `ApiClient` class that manages the actual Web service interactions.

- A `Show` represents a theatrical production, including its title, genre, and performer.
- A `Performance` represents a specific instance of a Show, including its date, time, and venue.
- A `Venue` represents a location where Performances take place, including its name and address.
- A `Festival` represents the overall event, containing all Shows, Performances, and Venues.
- An `ApiClient` represents the interface to the [Edinburgh Festival Web service](https://api.edinburghfestivalcity.com/).

## Concurrency and Background Processing

The typical usage pattern for festival data is:

- Data gets loaded from local storage or the Web service.
- Data is read/searched repeatedly in response to user requests. There is no need for multiple
  reads to occur in parallel.
- Data is periodically updated from the Web service. These updates should interfere as little as
  possible with both existing and new reads.

We rely on the CLR thread pool and make extensive use of the `Task` facility, rather than
managing our own threading explicitly.
In particular, the background update process is implemented using `Task` with explicit completions
and using a cancellation token to allow halting the process.

Because there's only ever a single reader, there's no benefit to the added complexity of read/write
locks.
Instead, we lock specific collection fields within the `Festival` object when reading or writing.
In general, we lock the fields for the duration of an entire search when reading and for the
duration of a single object find and update while writing.

## Exception Handling

Exceptions while reading should be essentially non-existent; if one occurs, it will probably be
fatal (e.g., out of memory).

Exceptions while initializing or while updating come in two varieties:

- Recoverable errors, such as a connection failure trying to access the Web service; and
- Unrecoverable errors, such as a Web service authentication failure.

Recoverable errors must be caught and handled.
Generally the appropriate behavior is to retry the operation, possibly after a delay.

Unrecoverable errors should "bubble up" to the main thread.

## Show

A Show represents a theatrical production, including its title, genre, and performer.

### Properties

- `Title`: The title of the show.
- `Genre`: The genre of the show.
- `Performer`: The performer(s) presenting the show.
- `Description`: A brief description of the show.
- `Venue`: The venue where the show is performed.
- `LastUpdated`: The UTC timestamp of the last update to the show's information.
- `Extra`: Any additional information about the show from the Fringe dataset, as a string dictionary (JSON object).

### Relationships

- `Performances`: A list of Performance objects associated with this show.

### Key Methods

- `GetPerformancesByDate`: Returns the list of Performance objects associated with this show within a date range.

## Performance

A Performance represents a specific instance of a Show, including its date, time, and venue.

### Properties

- `Start`: The start date and time of the performance.
- `End`: The date and time when the performance ends.
- `LastUpdated`: The UTC timestamp of the last update to the performance's information.
- `Extra`: Any additional information about the performance from the Fringe dataset, as a string dictionary (JSON object).

### Relationships

- `Show`: The Show object associated with this performance.

## Venue

A Venue represents a location where Performances take place, including its name and address.

### Properties

- `Name`: The name of the venue.
- `Address`: The address of the venue.
- `Code`: A unique code identifying the venue.
- `Id`: The unique identifier of the venue. This is guaranteed to be constant across updates.
- `LastUpdated`: The timestamp of the last update to the venue's information.
- `Extra`: Any additional information about the venue from the Fringe dataset, as a string dictionary (JSON object).

### Relationships

- `Performances`: A list of Performance objects taking place at this venue.

## Festival

A Festival represents the overall event, containing all Shows, Performances, and Venues.

### Properties

- `Name`: The name of the festival.
- `Location`: The location of the festival.
- `LastUpdated`: The timestamp of the last update to the festival's information.
- `Extra`: Any additional information about the festival from the Fringe dataset, as a string dictionary (JSON object).

### Relationships

- `Shows`: A list of all Show objects in the festival.
- `Performances`: A list of all Performance objects in the festival.
- `Venues`: A list of all Venue objects in the festival.

### Key Methods
- `GetShowsByDate`: Returns the list of Shows that have Performances within a date range, optionally filtered
  by genre and venue.
- `GetNearbyPerformances`: Returns the list of Performances taking place near a specified location and starting 
  within a specified time range.
- `UpdateFromFringeDataset`: Updates the festival's information from a Fringe JSON dataset.
