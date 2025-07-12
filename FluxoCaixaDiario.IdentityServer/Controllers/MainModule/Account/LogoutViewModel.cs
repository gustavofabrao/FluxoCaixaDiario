namespace FluxoCaixaDiario.IdentityServer.Controllers.MainModule.Account
{
    public class LogoutViewModel : LogoutInputModel
    {
        public bool ShowLogoutPrompt { get; set; } = true;
    }
}
