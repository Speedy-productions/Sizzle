// -----------------------------------------------
// AuthService.cs
// Resumen:
// - Fachada estática para la UI. Permite cambiar el backend sin tocar la UI.
// - Hoy usa WebAuthProvider (HTTP(S) + JSON).
// -----------------------------------------------
using System;
using UnityEngine;
using Sizzle.Auth;

public static class AuthService
{
    private static IAuthProvider _provider;

    public static void Init(MonoBehaviour runner)
    {
        _provider = new WebAuthProvider(ServerConfig.BaseUrl, runner);
    }

    public static void ValidateCredentials(string emailOrUser, string password, Action<bool, string> onResult)
    {
        if (_provider == null) { onResult?.Invoke(false, "AuthService no inicializado"); return; }
        _provider.ValidateCredentials(emailOrUser, password, onResult);
    }

    public static void Register(string username, string email, string password, Action<bool, string> onResult)
    {
        if (_provider == null) { onResult?.Invoke(false, "AuthService no inicializado"); return; }
        _provider.Register(username, email, password, onResult);
    }
}
