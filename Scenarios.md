# Scenarios

This document describes the various scenarios that the fringeApi client library aims to support.

## Out of Scope

We do not consider user interface details here; the client library should not impose restrictions
on the user interface design.

This library is focused on search and view scenarios. 
The following are considered out of scope of this library, although they are required functionality 
for a full-fledged application:

- Purchasing tickets, including tracking purchased tickets and preferred payment methods.
- Managing user preferences, including maintaining favorite shows and genres.
- Sending notifications to users.

## Sizing Assumptions

We assume the following rough sizes:

- Number of shows: ~5,000
- Number of venues: ~500
- Number of performances per show: ~30

## Scenario 1: Browsing the Festival Listings

In this scenario, the user wishes to see a list of all the shows available at the Edinburgh Fringe Festival, 
along with basic information such as show titles, venues, and performance times. 
The user may want to apply filters such as genre, (partial) performer, or (partial) title.

### User Goals

- Quickly get an overview of all available shows.
- Identify shows of interest based on basic information.
- Navigate through the listings efficiently.

### System Requirements

- The system should be able to search through a large number of shows efficiently.
- The system should support filtering based on genre, (partial) performer, and (partial) title matches.

## Scenario 2: Viewing Show Details

In this scenario, the user wishes to view detailed information about a specific show.

### User Goals

- Obtain comprehensive information about a selected show.

### System Requirements

## Scenario 3: Searching for Performances in a Specific Date Range

In this scenario, the user wishes to find performances, grouped by show, that occur within a specific date/time range.
They may wish to filter by genre or by approximate location.

### User Goals

- Quickly identify performances that fit within their selected date/time range.

### System Requirements

- The system should allow users to specify a date/time range for searching performances.
- The system should support filtering performances by genre and approximate location.

## Scenario 4: "Nearby Now"

In this scenario, the user wishes to find performances that are happening close to their current location
and starting relatively soon.

### User Goals

- Discover performances that are close to their current location and starting soon.

### System Requirements

- The system should allow the user to specify a maximum distance and a time window for nearby performances.
- The system should allow the user to specify that the maximum distance should be increasing with the time
  until the performance.

## Scenario 5: Viewing Venue Details

In this scenario, the user wishes to view detailed information about a specific venue.

### User Goals

- Obtain comprehensive information about a selected venue.

### System Requirements

- The system should provide detailed information about a venue, including its location and upcoming performances.

## Scenario 6: Updating the Data Set in Background

In this scenario, the system updates the Fringe dataset in the background to ensure that the 
information remains current without interrupting the user's experience.
The Fringe API documentation requires that information be refreshed at least every 24 hours;
that is, that the information displayed to the user should not be more than 24 hours old.

### User Goals

- Ensure that the data they are viewing is up-to-date.
- Avoid manual refreshes or interruptions while browsing.

### System Requirements

- The system should support background updates to the dataset.
- The system should ensure that ongoing user interactions are not disrupted by background updates.
