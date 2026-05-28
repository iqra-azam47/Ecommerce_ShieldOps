namespace ShieldOps.Services
{
    public interface IEmailMockService
    {
        void SendVerificationToken(string email, string token);
    }
}