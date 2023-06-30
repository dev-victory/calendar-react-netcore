using CalendarInvitation.Auth.Entities;
using CalendarInvitation.Auth.Models;

namespace CalendarInvitation.Auth.Services
{
    public interface IUserService
    {
        AuthenticateResponse? Authenticate(AuthenticateRequest model);
        IEnumerable<User> GetAll();
        User? GetById(int id);
    }
}
