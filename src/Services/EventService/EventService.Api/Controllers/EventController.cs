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

        // TODO: send start and end date to search, rather than funny bool
        [HttpGet("{IsFilterByWeek}", Name = "GetEventsByUser")]
        [ProducesResponseType(typeof(IEnumerable<EventVm>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
        public async Task<ActionResult<IEnumerable<EventVm>>> GetEventsByUserId(bool? IsFilterByWeek)
        {
            var query = new GetEventListQuery(User.Identity.Name, IsFilterByWeek ?? true);
            var events = await _mediator.Send(query);

            return Ok(events);
        }

        [HttpGet("[action]/{eventId}", Name = "GetEventById")]
        [ProducesResponseType(typeof(EventVm), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
        public async Task<ActionResult<EventVm>> GetEventById(Guid eventId)
        {
            var query = new GetEventByIdQuery(eventId, User.Identity.Name);
            var eventDetails = await _mediator.Send(query);

            return Ok(eventDetails);
        }

        [HttpDelete("[action]/{eventId}", Name = "Delete")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
        public async Task<ActionResult> Delete(Guid eventId)
        {
            await _mediator.Send(new DeleteEventCommand { EventId = eventId, UserId = User.Identity.Name });

            return Ok();
        }

        [HttpPut]
        [ProducesResponseType((int)HttpStatusCode.Accepted)]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
        public async Task<ActionResult> UpdateEvent([FromBody] UpdateEventCommand command)
        {
            command.ModifiedBy = User.Identity.Name;
            await _mediator.Send(command);

            return Accepted();
        }

        [HttpPost]
        [ProducesResponseType(typeof(Guid), (int)HttpStatusCode.Created)]
        [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
        public async Task<ActionResult<Guid>> CreateEvent([FromBody] CreateEventCommand command)
        {
            command.CreatedBy = User.Identity.Name;
            var eventId = await _mediator.Send(command);

            return Created("/Event/GetEventById/", eventId);
        }
    }
}
