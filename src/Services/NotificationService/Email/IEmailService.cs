namespace NotificationService.Email
{
    public interface IEmailService
    {
        Task<bool> SendEmail(EmailMessage email);
    }
}
