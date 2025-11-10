using System;

public static class AuthEvents
{
    // Se llama cada vez que cambia la sesión (login o logout)
    public static Action OnSessionChanged;

    public static void RaiseSessionChanged()
    {
        OnSessionChanged?.Invoke();
    }
}
