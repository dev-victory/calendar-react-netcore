# Calendar Invitation React App

## Summary 
This PoC application is a basic demonstration of a microservices architecture backend with Event and Notification services powering a React application. The calendar events are managed by performing basic CRUD operations on the Event service.

![HLD](http://drive.google.com/uc?id=1h_ZI1sSF_WT3kdIXKoo81EkP_AHM4vlI)
*High-level System Design*

### Auth
The authentication and authorization of the application is managed via [Auth0](https://auth0.com) a third party user management service. There are two primary components that manage two different fronts of the Calendar Invitation app.
The **authentication** of the front-end is managed via *Single Page Application* component in Auth0 by using Client credentials to obtain an access token.
The **authorization** for accessing specific endpoints to back-end is managed using a *Machine to Machine* API component in Auth0. The back channel communication occurs behind the scenes to authorize the calls made to Event micro-service behind the API gateway by checking the roles configured in Auth0.

### Event service
Event service is the backbone of the application having access to all the events data. Event service communicates with the database services and publishes events to the message queue for the [Notification service](#notification-service) to process later.
There are five endpoints available for performing CRUD operations in relation to Events, namely:
1. `GET /api/v1/Event/{isFilterByWeek}` for getting events by the current week. The latest events are updated to a [Redis cache](https://redis.io/) for quick access to the current week's events.
2. `GET /api/v1/Event/GetEventById/{eventId}` for getting event details for an event by its unique ID.
3. `DELETE /api/v1/Event/Delete/{eventId}` for deleting an event by its unique ID.
4. `PUT /api/v1/Event` for updating a specific event.
5. `POST /api/v1/Event` for adding a new event.

DELETE, UPDATE AND POST endpoints update the cache to have real-time access to the latest events.

The front end application doesn't have access to the API directly. The application follows a **Back end for Front end design pattern (BFF)**. All API calls are made to [Ocelot](https://ocelot.readthedocs.io/en/latest/introduction/gettingstarted.html) powered API gateway. The access to this channel is also secured using Auth0 for preventing any unauthorized access.

Events service acts as a *Producer* of new events hence all new events added to the events catalog are further published on to a [Kafka](https://docs.confluent.io/kafka-clients/dotnet/current/overview.html) topic as a message with required Event details. These messages are consumed by the Notification service for notifying relative users of new events they have been invited to.

### Notification service
Notification service is solely responsible for sending notifications to the users invited to a specific event. Basically notification service acts as a *Consumer* for the new messages being published to the subscribed topic in Kafka for new events.

**[Note]** As of now, notification service only publishes notifications for a new event to the users. 

## Instructions to run the app
1. Open the solution file named `CalendarInvitation.sln` under the root folder of the project.

2. Select the following projects as startup projects:
- `ApiGateway` - this is the BFF project that acts as the gatekeeper for all incoming requests from the Web project
- `EventService.Api` - this is the backbone API for serving all event requests for CRUD tasks
- `NotificationService` - This will pass on the Event Notifications to SendGrid for email communications

3. Open a command-prompt/Powershell terminal in the `src\Web\calendarinvitation.web` folder of the solution project and run the following command: 
```npm run start```
This will start the Calender Invitation react app on `localhost:3000`.

## Pre-requisites
1. Kindly make sure that you have a redis cache running on a docker container (or a locally installed instance) because the EventService.Api requires Redis cache for storing the events 
[See `appsettings.json` file of EventService.Api for configuration settings of `Redis` section]

2. Run the `kafka-docker.compose.yml` using `docker-compose up` command (from the folder containing the `.yml` file) which will spin up a local kafka instance with all the necessary components for smooth running of Kafka event topic queue. The control center to view the event messages are run on port `9021` by default. See the `new-event-meta` topic in control center for viewing the latest messages, based on the offset.

## Project Technical Details
- **calendarinvitation.web**: is a vanilla react app secured using Auth0 library for user management. This app uses a few third-party `npm` calendar UI component libraries. The app has a basic routing structure setup for navigation.
- **EventService.Api**: is a .NET 7 web app talking to class library services that are designed using Clean Architecture principles as descirbed out by Jason Taylor in the [Clean Architecture repo](https://github.com/jasontaylordev/CleanArchitecture). 
The application messages are exchanged within the application as commands and queries as per CQRS design pattern. The incoming command and query messages are validated using `Fluent Validation` library.
The infrastructure layer uses Entity Framework Core v7, the backend layer talks to a SQL server instance.
- **ApiGateway**: is a .NET 7 web app that uses Ocelot API Gateway library for exposing the backend services. The API gateway is secured using an auth server powered by Auth0. The gateway routes are setup in the `ocelot.local.json` file.
- **NotificationService**: is a .NET 7 web app with a `minimal API` endpoint that gets data from a consumer service in sync with a Kafka event queue.
- **EventService.Application.Tests**: a suite of `xUnit` tests is added for code coverage of the primary application functional units for a sanity check.


#### Future updates:
1. Scheduled reminders/notifications for notifying the intended users.
2. Securing notification service using [SASL]( https://medium.com/tribalscale/kafka-security-configuring-sasl-authentication-on-net-core-apps-da5d0b0fcc5).
3. Filters for getting events by a date range in Event service event list endpoint.
4. Add event search ability using [Elastic search](https://www.elastic.co/enterprise-search/search-applications).
5. Add Generative AI integration to add, modify and view events.
