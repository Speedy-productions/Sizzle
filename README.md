# 🧩 sizzle – Setup rápido (Server + Unity)

Guía para levantar el **backend local con MariaDB** y conectar el **cliente de Unity** por HTTPS usando **ngrok**.  
Pensado para funcionar “desde cero” en una máquina limpia.

---

## 🧱 1) Requisitos a instalar

### Base de datos
- **[XAMPP](https://www.apachefriends.org/)** → Para MySQL/MariaDB local  
- **[HeidiSQL](https://www.heidisql.com/)** → Cliente gráfico para gestionar la base de datos  

### Runtime y herramientas
- **[Node.js LTS](https://nodejs.org/)**  
- **[Git](https://git-scm.com/)**  
- **[ngrok](https://ngrok.com/)** → Túnel HTTPS

### Unity
- Proyecto del juego (ya configurado con los scripts de login/registro).

💡 *Tip:* Después de instalar XAMPP, abrir el panel de control y arrancar **MySQL** antes de usar la base de datos.

---

## 📦 2) Clonar el servidor

```bash
git clone https://github.com/Speedy-productions/Server
cd Server
code .
```

Esto abre la carpeta del servidor en **VS Code**.

---

## ⚙️ 3) Instalar dependencias del backend

En la terminal de VS Code (ubicada en la carpeta `Server`):

```bash
npm install express mariadb cors bcrypt dotenv
```

---

## 🔐 4) Configurar variables de entorno

Crear un archivo `.env` en la raíz de `Server`:

```env
# .env
DB_HOST=127.0.0.1
DB_PORT=3306
DB_USER=root
DB_PASSWORD=
DB_NAME=sizzle

# Puerto local del backend
APP_PORT=3000
```

> 🔸 **Nota:** En XAMPP el campo `DB_PASSWORD` suele estar vacío.  
> El backend escuchará en `http://localhost:3000` (luego ngrok le pondrá HTTPS público).

---

## 🗄️ 5) Crear la base de datos con HeidiSQL

1. Abrir **HeidiSQL**  
2. Crear una conexión “Local” a `127.0.0.1`, usuario `root`, sin contraseña (o la que corresponda).  
3. Conectar y usar el menú **Archivo → Cargar archivo SQL…**  
4. Seleccionar el archivo SQL del repositorio (`sizzle.sql`).  
5. Ejecutar.  

Debe quedar la BD **sizzle** con la tabla **usuario**.

> ⚠️ Las contraseñas se almacenan con **hash bcrypt**, nunca en texto plano.

---

## 🚀 6) Arrancar el backend en local

Desde la terminal (en la carpeta `Server`):

```bash
node index.js
```

Deberías ver algo como:

```
API escuchando en http://localhost:3000
```

Probar el endpoint de salud:

```bash
curl http://localhost:3000/health
```

Respuesta esperada:

```json
{"ok": true, "message": "Server up"}
```

---

## 🌐 7) Exponer HTTPS con ngrok

En otra terminal:

```bash
ngrok http 3000
```

ngrok mostrará una URL pública tipo:

```
https://<subdominio>.ngrok-free.dev
```

Copia esa URL exacta (la de `https://...`) para usarla en el cliente Unity.

> 💡 El subdominio puede cambiar entre ejecuciones.  
> Si cambia, hay que actualizar la URL en Unity.

---

## 🎮 8) Configurar la URL en Unity

Abrir el proyecto de Unity y editar:

`Assets/Scripts/Backend/IAuth.cs`

```csharp
public static class ServerConfig
{
    // Pegar aquí la URL pública HTTPS de ngrok
    public const string BaseUrl = "https://<tu-subdominio>.ngrok-free.dev";
}
```

Guardar y compilar.  
La UI de login/registro usará esa URL para llamar a:

- `POST /auth/login` → `{ "emailOrUser": "...", "password": "..." }`
- `POST /auth/register` → `{ "username": "...", "email": "...", "password": "..." }`

---

## 🧩 9) Flujo de la UI (Unity)

### Login
- Escena o pantalla con `LoginUI.cs`
- Completar email/usuario y contraseña, presionar **Iniciar sesión**
- Si es correcto → navega a `MenuPrincipal`

### Registro
- Botón “Registrarse” desde el login → abre `RegisterUI.cs`
- Completar **username**, **email**, **contraseña** y confirmación
- Al registrarse, vuelve al login con los campos limpios

---

## ⚡ 10) Comandos rápidos para levantar todo

Cada vez que se vaya a trabajar:

**1. Arrancar MySQL**
- Abrir **XAMPP → botón Start** en `MySQL`

**2. Backend**
```bash
cd Server
node index.js
```

**3. ngrok**
```bash
ngrok http 3000
```

Pegar la URL `https://` de ngrok en `ServerConfig.BaseUrl` (si es diferente).

---

## 🔗 11) Endpoints útiles (curl/Postman)

### Salud
```bash
curl https://<subdominio>.ngrok-free.dev/health
```

### Registro
```bash
curl -X POST https://<subdominio>.ngrok-free.dev/auth/register   -H "Content-Type: application/json"   -d "{"username":"Lalo","email":"lalo@example.com","password":"1234"}"
```

### Login
```bash
curl -X POST https://<subdominio>.ngrok-free.dev/auth/login   -H "Content-Type: application/json"   -d "{"emailOrUser":"lalo@example.com","password":"1234"}"
```

---

## 🧠 12) Notas de seguridad

- **Transporte:** HTTPS (TLS) provisto por ngrok cifra todo el tráfico.  
- **Contraseñas:** Hasheadas con **bcrypt**, nunca en texto plano.  
- **CORS:** Permitido para GET/POST en desarrollo (endurecer en producción).  
- **Claves RSA:** No se usan aquí (TLS de ngrok cubre cifrado).

---

## 🧰 13) Solución de problemas

| Problema | Posible causa / solución |
|-----------|--------------------------|
| **ngrok responde pero Unity no conecta** | Verificar que `ServerConfig.BaseUrl` coincida exactamente con la URL de ngrok (incluido `https://`). |
| **500 al registrar** | Revisar que la tabla `usuario` existe y que se importó el SQL correcto. |
| **401 en login** | Usuario o contraseña incorrectos, o hash no coincide. Crear un usuario nuevo. |
| **Puerto ocupado** | Cambiar `APP_PORT` en `.env` y ejecutar `ngrok http <puerto>` con el nuevo valor. Actualizar Unity. |

---

## 📂 14) Estructura mínima del backend

```
Server/
  ├── index.js           # Servidor Express (login + register)
  ├── .env               # Credenciales locales
  ├── package.json
  └── sizzle.sql  # Script SQL de la BD (tabla usuario)
```

---

## 🧩 15) Qué hace cada parte

- **index.js:** API REST con rutas `/auth/register` y `/auth/login`, conexión a MariaDB, validaciones y hash bcrypt.  
- **.env:** Configuración local (host, puerto, db, usuario).  
- **Unity:** Scripts `WebAuthProvider`, `AuthService`, `LoginUI`, `RegisterUI` construyen JSON y se comunican con la API HTTPS según la URL en `ServerConfig.BaseUrl`.

---

✅ **Listo:** con esto puede ejecutar el backend, tunelarlo con HTTPS y conectar el cliente Unity de forma segura.


# 🔐 Criptografía y Seguridad en Sizzle

Explicación de cómo se cifran los datos y se protegen las credenciales tanto **en tránsito** como **en reposo**, sin necesidad de implementar cifrados manuales en el cliente Unity.

---

## 🎯 16) Objetivo

Proteger:
- **Credenciales y datos en tránsito** (entre cliente y servidor)
- **Contraseñas en reposo** (en la base de datos)

El cliente **no maneja cifrados a mano**: todo el cifrado se delega a HTTPS y bcrypt en el backend.

---

## 🌐 16.1 Transporte (Cliente ↔ Servidor)

El juego se comunica con el backend mediante **HTTPS**, que es **HTTP sobre TLS** (normalmente **TLS 1.3** en ngrok).

Durante el *handshake* TLS se negocian algoritmos de cifrado:

- **RSA** o, más común hoy, **ECDHE** para acordar una clave de sesión.
- Una vez establecida, los datos se cifran con **AES-GCM**, por ejemplo:  
  `TLS_AES_128_GCM_SHA256`

**Resultado:**  
Todo lo que sale de Unity y llega al servidor viaja **cifrado y autenticado** por TLS.  
No se exponen usuarios ni contraseñas en claro en la red.

> ⚙️ No se implementa RSA/AES manualmente en Unity; lo maneja TLS.  
> Por eso no hay directamente archivos `public.pem` o `private.pem` en el proyecto.

---

## 💾 16.2 Credenciales en Reposo (Base de Datos)

El servidor **no guarda contraseñas reales**.  
Utiliza **bcrypt** para almacenar un *hash* con *salt*.

**Flujo del login:**

1. El cliente envía `{ emailOrUser, password }` por HTTPS.  
2. El servidor obtiene el *hash* guardado.  
3. Llama `bcrypt.compare(password, hash)`  
4. Si coincide → acceso permitido. Si no → error 401.

**Ventajas:**
- Si se filtra la tabla, las contraseñas reales no se pueden recuperar.  
- bcrypt es *intencionadamente lento* para dificultar ataques de fuerza bruta.

---

## 🧠 16.3 Por qué no se usa AES-GCM “a mano” en Unity

El protocolo **TLS** ya utiliza **AES-GCM** internamente para cifrar el canal.  
Agregar una capa adicional de cifrado con AES manual solo complica el flujo y **no aporta seguridad extra**.

La capa de aplicación debe enfocarse en:

- Validaciones lógicas  
- Reglas de negocio  
- (Para futuro) Firmas HMAC o JWT para eventos de sesiones de usuarios o gameplay

---

## 📊 16.4 Comparación rápida de piezas

| Pieza | Qué hace | Dónde se usa aquí |
|--------|-----------|-------------------|
| **TLS (RSA/ECDHE + AES-GCM)** | Cifra y autentica la conexión | Unity ↔ API (vía ngrok) |
| **bcrypt** | Guarda contraseñas como *hash* con *salt* | Tabla `usuario.Contrasenia` |
| **JWT (futuro)** | Token de sesión firmado | No implementado aún |

---

## 🗄️ 17) SQL del esquema actual

```sql
-- sizzle.sql — esquema mínimo para login/registro con bcrypt

CREATE DATABASE IF NOT EXISTS `sizzle`
  DEFAULT CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci;
USE `sizzle`;

DROP TABLE IF EXISTS `usuario`;
CREATE TABLE `usuario` (
  `Id` INT(11) UNSIGNED NOT NULL AUTO_INCREMENT,
  `NombreUsuario` VARCHAR(50) NOT NULL,
  `Email` VARCHAR(100) NOT NULL,
  `Contrasenia` VARCHAR(100) NOT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `uniq_email` (`Email`),
  UNIQUE KEY `uniq_user` (`NombreUsuario`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- usar /auth/register para crear usuarios con hash bcrypt.
```

---

## 📝 Notas

- La columna `Contrasenia` es **VARCHAR(100)** → suficiente para los hashes bcrypt (`$2b$...`).  
- Índices únicos en `Email` y `NombreUsuario` → evitan duplicados.  
- Las contraseñas solo deben crearse mediante `/auth/register`, que aplica el hash bcrypt automáticamente.

---

✅ Con esto, los datos viajan cifrados, las contraseñas se guardan seguras, y el sistema evita manejo manual de claves o cifrados innecesarios.
