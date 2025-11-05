

## ⚙️ 1) Arquitectura actual

El sistema ahora funciona así:

```
Unity (Cliente)
   ↓ HTTPS (TLS 1.3)
Render (Servidor Node.js)
   ↓
Supabase (Base de datos + autenticación segura)
```

- El backend (Node.js + Express + bcrypt + OAuth2) está desplegado en **Render**:  
  👉 [https://github.com/Speedy-productions/Server](https://github.com/Speedy-productions/Server)

  https://serversizzle.onrender.com

- Render se encarga de mantener el servidor activo con HTTPS y TLS.  
- Supabase gestiona el almacenamiento, los usuarios y la seguridad.  
- Unity solo hace peticiones HTTPS al servidor remoto (no hay base local ni túneles).

---

## 🌍 2) Configurar Unity

Abrir el archivo:

```
Assets/Scripts/Backend/IAuth.cs
```

Y verificar que apunte al servidor hosteado en Render:

```csharp
public static class ServerConfig
{
    // Endpoint del backend público en Render
    public const string BaseUrl = "https://serversizzle.onrender.com";
}
```

El cliente Unity usará este endpoint para:

- `POST /auth/register` → Crear nuevos usuarios  
- `POST /auth/login` → Iniciar sesión  
- `GET /auth/google/start` → Autenticación OAuth con Google  

> 💡 Todo esto viaja cifrado mediante HTTPS automáticamente.  
> Unity no maneja contraseñas ni cifrados manuales.

---

## 🎮 3) Flujo de la UI (Unity)

- **LoginUI.cs:** Llama a `/auth/login` con email/usuario + contraseña.  
- **RegisterUI.cs:** Llama a `/auth/register` para crear usuarios con hash bcrypt.  
- **Google Login:** Redirige a `/auth/google/start`, Render maneja el OAuth completo.  

Si las credenciales son correctas, Render responde con `{ ok:true, user:{...} }` y Unity carga el menú principal.

---

# 🔐 Criptografía y Seguridad en Sizzle

Explicación de cómo se cifran los datos y se protegen las credenciales tanto **en tránsito** como **en reposo**, ahora bajo la infraestructura segura de **Render + Supabase**.

---

## 🎯 4) Objetivo

Proteger:
- **Credenciales y datos en tránsito** (entre Unity y Render)
- **Contraseñas en reposo** (en la base de datos Supabase)

El cliente **no cifra manualmente**: toda la seguridad se delega a **HTTPS** (Render) y **bcrypt** (backend).

---

## 🌐 4.1 Transporte (Cliente ↔ Render)

El juego se comunica con el servidor desplegado en **Render** usando **HTTPS (TLS 1.3)**.  
Durante el handshake TLS se negocian algoritmos como:

- **ECDHE** para el intercambio de claves.
- **AES-GCM** (por ejemplo, `TLS_AES_128_GCM_SHA256`) para cifrar la sesión.

**Resultado:**  
Todo lo que sale del cliente Unity y llega al backend viaja **cifrado y autenticado**.  
Nadie puede ver usuarios, contraseñas ni datos sensibles en la red.

> 🔐 Render proporciona el certificado TLS automáticamente.  
> Unity no necesita claves RSA locales ni archivos `.pem`.

---

## 💾 4.2 Credenciales en Reposo (Supabase)

Las contraseñas **no se guardan en texto plano**.  
El backend (en Render) aplica **bcrypt** antes de insertarlas en Supabase.

**Flujo simplificado:**
1. Unity envía `{ email, password }` al backend (por HTTPS).  
2. El backend genera un *hash bcrypt* con *salt*.  
3. Guarda el hash en Supabase.  
4. Durante login, compara el hash con `bcrypt.compare()`.

**Ventajas:**
- Si la base se filtra, las contraseñas no son reversibles.  
- bcrypt es lento a propósito → evita ataques de fuerza bruta.

---

## 🧠 4.3 Por qué no se cifra “a mano” en Unity

TLS ya cifra los datos con **AES-GCM** en cada conexión.  
Agregar una capa manual con AES o RSA solo añade complejidad sin beneficios reales.

La seguridad de Sizzle se basa en:
- HTTPS → canal cifrado (Render)  
- bcrypt → hash seguro de contraseñas (Servidor)  
- Supabase → base de datos gestionada con autenticación segura  

---

## 📊 4.4 Comparación de tecnologías

| Capa | Tecnología | Propósito |
|------|-------------|-----------|
| Transporte | **TLS (AES-GCM)** | Cifra Unity ↔ Render |
| Backend | **bcrypt** | Hash de contraseñas |
| Almacenamiento | **Supabase** | Base de datos segura y administrada |
| Hosting | **Render** | Servidor HTTPS público y persistente |

---

## ✅ 5) Resultado

- No se necesita MariaDB, ngrok ni servidor local.  
- Todo el backend está en Render, con HTTPS automático.  
- Supabase mantiene los datos y los usuarios.  
- Unity se comunica solo con la API pública.  
- Las contraseñas se almacenan de forma segura con bcrypt.
