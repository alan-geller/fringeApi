# Design

## System Requirements

- The system should not impose restrictions on the user interface design.
- The system should not require keeping a socket open at all times.
- The system should support multithreaded access. In particular, it should allow multiple readers
  to access the system concurrently without blocking each other, and should allow a single writer
  to access the system exclusively without unduly delaying readers.

We assume robust garbage collection and so don't worry about limiting pointers between objects, 
nor about avoiding pointer cycles.

## Object Model

The primary objects in the Fringe dataset are Show, Performance, and Venue.
In addition, there is a Festival object that acts as a container for the Shows, Performances, and Venues.

- A Show represents a theatrical production, including its title, genre, and performer.
- A Performance represents a specific instance of a Show, including its date, time, and venue.
- A Venue represents a location where Performances take place, including its name and address.
- A Festival represents the overall event, containing all Shows, Performances, and Venues.

## Show

A Show represents a theatrical production, including its title, genre, and performer.

### Attributes
- `Title`: The title of the show.
- `Genre`: The genre of the show.
- `Performer`: The performer(s) presenting the show.
- `Description`: A brief description of the show.
- `Venue`: The venue where the show is performed. This will be null if the show is performed at multiple venues.
- `Extra`: Any additional information about the show from the Fringe dataset, as a string dictionary (JSON object).

### Relationships
- `Performances`: A list of Performance objects associated with this show.

## Performance

A Performance represents a specific instance of a Show, including its date, time, and venue.

### Attributes
- `Date`: The date of the performance.
- `Time`: The time of the performance.
- `Venue`: The venue where the performance takes place.
- `Extra`: Any additional information about the performance from the Fringe dataset, as a string dictionary (JSON object).

### Relationships
- `Show`: The Show object associated with this performance.

## Venue

A Venue represents a location where Performances take place, including its name and address.

### Attributes
- `Name`: The name of the venue.
- `Address`: The address of the venue.
- `Code`: A unique code identifying the venue.
- `Extra`: Any additional information about the venue from the Fringe dataset, as a string dictionary (JSON object).

### Relationships
- `Performances`: A list of Performance objects taking place at this venue.

## Festival

A Festival represents the overall event, containing all Shows, Performances, and Venues.

### Attributes
- `Name`: The name of the festival.
- `Location`: The location of the festival.
- `Extra`: Any additional information about the festival from the Fringe dataset, as a string dictionary (JSON object).

### Relationships
- `Shows`: A list of all Show objects in the festival.
- `Performances`: A list of all Performance objects in the festival.
- `Venues`: A list of all Venue objects in the festival.

