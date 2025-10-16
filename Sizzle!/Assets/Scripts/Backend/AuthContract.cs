using System;

namespace Sizzle.Auth
{
    public interface IAuthProvider
    {
        void ValidateCredentials(string emailOrUser, string password, Action<bool, string> onResult);
        void Register(string username, string email, string password, Action<bool, string> onResult);
    }

    public static class ServerConfig
    {
        public const string BaseUrl = "https://edaphic-coralie-preapply.ngrok-free.dev";
    }
}
