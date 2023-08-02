using EventService.Application.Features.Events.Commands.CreateEvent;
using EventService.Application.Features.Events.Queries.GetEventById;
using EventService.Application.Features.Events.Queries.GetEventList;
using EventService.Application.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Security.Claims;

namespace EventService.Api.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    [Authorize(Policy = "MustBeVerifiedUser")]
    public class EventController : ControllerBase
    {
        private readonly IMediator _mediator;

        public EventController(IMediator mediator)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        }

        [HttpGet("{userId}", Name = "GetEventsByUser")]
        [ProducesResponseType(typeof(IEnumerable<EventVm>), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<IEnumerable<EventVm>>> GetEventsByUserId()
        {
            // TODO: add user context manager
            var userId = User.Claims.FirstOrDefault(x=> x.Type == ClaimTypes.NameIdentifier)?.Value;

            var query = new GetEventListQuery(userId);
            var events = await _mediator.Send(query);

            return Ok(events);
        }

        // TODO: check user Id for security?
        [HttpGet("[action]/{eventId}", Name = "GetEventById")]
        [ProducesResponseType(typeof(EventVm), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<EventVm>> GetEventById(Guid eventId)
        {
            var query = new GetEventByIdQuery(eventId);
            var eventDetails = await _mediator.Send(query);
            // TODO: handle this in application layer
            if (eventDetails != null && eventDetails.CreatedBy != "abc123456") 
            {
                return Forbid("You don't have access to this event");
            }

            return Ok(eventDetails);
        }

        // DELETE method - remember to create and set isDeleted column in events

        // PUT method - update existing event with new values


        // TODO: validate incoming payload in application layer
        [HttpPost]
        [ProducesResponseType(typeof(Guid), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<Guid>> CreateEvent([FromBody] CreateEventCommand command)
        {
            // TODO: add user context manager
            var userId = User.Claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier)?.Value;
            command.CreatedBy = userId;
            var eventId = await _mediator.Send(command);

            return Ok(eventId);
        }
    }
}
