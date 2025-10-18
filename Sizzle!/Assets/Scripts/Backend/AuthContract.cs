// -----------------------------------------------
// IAuth.cs
// Resumen:
// - Define una interfaz mínima para autenticación.
// - La implementación actual (WebAuthProvider) envía credenciales
//   por HTTPS a la API (TLS) y el server valida con bcrypt.
// -----------------------------------------------
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
        // endpoint HTTPS (ngrok durante desarrollo)
        public const string BaseUrl = "https://serversizzle.onrender.com";
    }
}
