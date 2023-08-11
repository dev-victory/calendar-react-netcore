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
    /* 
    TODO: 
    - IMPORTANT UTC dates - event and notifications
    - better error handling not found exception error binding to UI
    - UI: refactor code, DRY, YAGNI
    - add user context manager
    - validate incoming payload in application layer
    - handle conditions in application layer
    - redis cache connection error
    - event bus connection error
    */

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

        // TODO: send start and end date to search, rather than funny bool
        [HttpGet("{IsFilterByWeek}", Name = "GetEventsByUser")]
        [ProducesResponseType(typeof(IEnumerable<EventVm>), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<IEnumerable<EventVm>>> GetEventsByUserId(bool? IsFilterByWeek)
        {
            var userId = User.Claims.FirstOrDefault(x=> x.Type == ClaimTypes.NameIdentifier)?.Value;

            var query = new GetEventListQuery(userId, IsFilterByWeek ?? true);
            var events = await _mediator.Send(query);

            return Ok(events);
        }

        [HttpGet("[action]/{eventId}", Name = "GetEventById")]
        [ProducesResponseType(typeof(EventVm), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<EventVm>> GetEventById(Guid eventId)
        {
            var userId = User.Claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier)?.Value;
            var query = new GetEventByIdQuery(eventId, userId);
            var eventDetails = await _mediator.Send(query);

            return Ok(eventDetails);
        }

        // TODO: check permissions if creator is deleting?
        [HttpDelete("[action]/{eventId}", Name = "Delete")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        public async Task<ActionResult> Delete(Guid eventId)
        {
            await _mediator.Send(new DeleteEventCommand { EventId = eventId });

            return Ok();
        }

        [HttpPut]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        public async Task<ActionResult> UpdateEvent([FromBody] UpdateEventCommand command)
        {
            var userId = User.Claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier)?.Value;
            command.ModifiedBy = userId;
            await _mediator.Send(command);

            return Ok();
        }

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
