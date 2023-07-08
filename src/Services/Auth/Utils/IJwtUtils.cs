using CalendarInvitation.Auth.Entities;

namespace CalendarInvitation.Auth.Utils
{
    public interface IJwtUtils
    {
        public string GenerateJwtToken(User user);
        public int? ValidateJwtToken(string? token);
    }

}
