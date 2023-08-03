using EventService.Application.Features.Events.Commands.CreateEvent;
using EventService.Application.Features.Events.Commands.DeleteEvent;
using EventService.Application.Features.Events.Commands.UpdateEvent;
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

        // TODO: add user context manager
        [HttpGet("{userId}", Name = "GetEventsByUser")]
        [ProducesResponseType(typeof(IEnumerable<EventVm>), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<IEnumerable<EventVm>>> GetEventsByUserId()
        {
            var userId = User.Claims.FirstOrDefault(x=> x.Type == ClaimTypes.NameIdentifier)?.Value;

            var query = new GetEventListQuery(userId);
            var events = await _mediator.Send(query);

            return Ok(events);
        }

        // TODO: 
        // 1. add user context manager
        // 2. handle conditions in application layer
        [HttpGet("[action]/{eventId}", Name = "GetEventById")]
        [ProducesResponseType(typeof(EventVm), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<EventVm>> GetEventById(Guid eventId)
        {
            
            var userId = User.Claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier)?.Value;
            var query = new GetEventByIdQuery(eventId);
            var eventDetails = await _mediator.Send(query);

            if (eventDetails != null && eventDetails.CreatedBy != userId) 
            {
                return Forbid("You don't have access to this event");
            }

            return Ok(eventDetails);
        }

        // DELETE method - remember to create and set isDeleted column in events
        [HttpDelete]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        public async Task<ActionResult> DeleteEvent([FromBody] DeleteEventCommand command)
        {
            var userId = User.Claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier)?.Value;
            
            // TODO: check permissions if creator is deleting?
            await _mediator.Send(command);

            return Ok();
        }


        // PUT method - update existing event with new values
        [HttpPut]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        public async Task<ActionResult> UpdateEvent([FromBody] UpdateEventCommand command)
        {
            var userId = User.Claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier)?.Value;
            command.ModifiedBy = userId;
            await _mediator.Send(command);

            return Ok();
        }

        // TODO: 
        // 1. validate incoming payload in application layer
        // 2. why isn't createdby being set for notifications and invitations table?
        // 3. add user context manager
        [HttpPost]
        [ProducesResponseType(typeof(Guid), (int)HttpStatusCode.Created)]
        public async Task<ActionResult<Guid>> CreateEvent([FromBody] CreateEventCommand command)
        {
            var userId = User.Claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier)?.Value;
            command.CreatedBy = userId;
            var eventId = await _mediator.Send(command);

            return Created("/Event/GetEventById/", eventId);
        }
    }
}
