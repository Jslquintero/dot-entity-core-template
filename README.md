# Plantilla de API con ASP.NET Core

Base para una API con .NET 10 y PostgreSQL 16 que incluye Entity Framework Core, Identity,
autenticación JWT, autorización por roles, Swagger, registros estructurados y comprobaciones
del estado de la base de datos.

## Inicio local con Docker

Requisitos: Docker Engine con Compose y Python 3. Para desarrollar localmente con .NET también necesitas el SDK de .NET 10.

```bash
python3 scripts/setup-dev.py
docker compose up --build -d
```

Abre http://localhost:5249/swagger. Comprueba el estado de la base de datos en http://localhost:5249/healthz.
El correo del administrador inicial es `admin@example.test`; su **contraseña generada aleatoriamente**
está en `.env`, en la variable `ADMIN_PASSWORD`. No hay una contraseña compartida ni predeterminada.
El script de configuración no sobrescribe secretos existentes.

Compose utiliza una **configuración de desarrollo**: vincula los puertos a localhost, expone Swagger,
aplica las migraciones y crea el administrador inicial. Los datos de PostgreSQL persisten en el volumen
`postgres_data`. Detén los servicios con `docker compose down`; eliminar el volumen elimina la base de datos.

Si los puertos están ocupados, cambia `POSTGRES_PORT` y `API_PORT` en `.env`. Si ejecutas la API fuera
de Docker, actualiza también el puerto de la base de datos en `Template.Api/appsettings.Local.json`.

## Ejecutar la API con el SDK

Después de ejecutar `scripts/setup-dev.py`:

```bash
docker compose up -d postgres
dotnet tool restore
dotnet restore
dotnet run --project Template.Api --launch-profile http
```

`appsettings.Local.json` se carga únicamente en el entorno `Development`. Este archivo y `.env` quedan
excluidos de Git, de las imágenes Docker y de los proyectos generados con la plantilla. Las variables de
entorno tienen prioridad sobre la configuración local. Compose lee `.env`; `dotnet run` no lo carga automáticamente.

## Autenticación y usuarios

| Endpoint | Acceso | Comportamiento |
| --- | --- | --- |
| `POST /api/users/login` | Anónimo | Recibe correo y contraseña; devuelve un JWT y el perfil |
| `GET /api/users/me` | Token Bearer | Devuelve el perfil actual |
| `POST /api/users/logout` | Token Bearer | Revoca **todas** las sesiones actuales del usuario |
| `POST /api/users` | `SysAdmin` | Crea una cuenta estándar con el rol `User` |

JSON para iniciar sesión:

```json
{ "email": "admin@example.test", "password": "tu-contraseña-generada" }
```

La respuesta contiene `token`, `tokenExpirationDate` y `user`. Envía el token mediante
`Authorization: Bearer <token>` o utiliza el botón **Authorize** de Swagger. Las contraseñas nunca se devuelven.

JSON para crear un usuario:

```json
{
  "email": "persona@example.com",
  "password": "Una-contraseña-unica-y-segura-A1!",
  "name": "Nombre",
  "lastName": "Apellido"
}
```

Las contraseñas deben tener al menos 12 caracteres, una mayúscula, una minúscula, un dígito y un carácter
no alfanumérico. Cinco intentos fallidos de contraseña bloquean la cuenta durante 15 minutos.
El inicio de sesión está limitado a 10 solicitudes por minuto y dirección IP.

Los JWT duran 15 minutos de forma predeterminada. Se validan el emisor, la audiencia, la firma y la expiración.
Cada solicitud autenticada también comprueba en PostgreSQL que el usuario esté activo y que su sello de
seguridad (`SecurityStamp`) siga vigente. Así, cerrar sesión o cambiar la contraseña invalida los tokens
emitidos anteriormente. Las futuras funciones que cambien roles también deben actualizar el sello de seguridad.
Esta validación añade una consulta a la base de datos por solicitud autenticada.

La plantilla no incluye registro público, renovación mediante refresh tokens, envío de correos para recuperar
contraseñas ni un flujo de inicio de sesión con doble factor. Las cuentas con autenticación de doble factor
habilitada no pueden utilizar el endpoint que solo acepta contraseña.

Los roles iniciales son `SysAdmin`, `Admin` y `User`. La inicialización crea un administrador únicamente si
su correo no existe; nunca restablece contraseñas ni eleva los permisos de una cuenta existente.

## Migraciones de la base de datos

La migración inicial de Identity está incluida en el repositorio. El contexto utiliza
`IdentityDbContext<User, Role, string>`; las longitudes de las cadenas se asignan a columnas compatibles con PostgreSQL.

```bash
dotnet tool restore
dotnet ef migrations add TuCambio --project Template.Data --startup-project Template.Api
dotnet ef migrations has-pending-model-changes --project Template.Data --startup-project Template.Api
```

Para las herramientas que se conectan a la base de datos, define `ConnectionStrings__DefaultConnection`
en el entorno. La fábrica del contexto utilizada en tiempo de diseño no lee los secretos locales de la API:

```bash
dotnet ef database update --project Template.Data --startup-project Template.Api
dotnet ef migrations script --idempotent --project Template.Data --startup-project Template.Api --output migration.sql
```

En `Development`, las migraciones se aplican al arrancar. Fuera de ese entorno, la aplicación automática
de migraciones y la carga de datos iniciales están deshabilitadas de forma predeterminada. Aplica las
migraciones revisadas como parte del despliegue. Para crear el administrador en el primer despliegue,
habilita explícitamente `Seed__Enabled=true` y configura `Seed__AdminEmail` y `Seed__AdminPassword`;
después, deshabilita la inicialización.

## Pruebas y verificaciones

```bash
# Crea un contenedor temporal de PostgreSQL en un puerto aleatorio, ejecuta las pruebas y lo elimina.
python3 scripts/test.py

# Solo pruebas unitarias, sin Docker ni PostgreSQL:
dotnet test --filter 'Category!=Integration'

dotnet build
dotnet format --verify-no-changes
dotnet list package --vulnerable --include-transitive
```

Las pruebas de integración utilizan PostgreSQL real y cubren migraciones, datos iniciales, validación del
inicio de sesión, ausencia de contraseñas en las respuestas, creación de usuarios, autorización por roles,
revocación de sesiones, bloqueo de cuentas, límites de solicitudes, cuentas deshabilitadas y errores de
persistencia del repositorio.

Para ejecutarlas con tu propio servidor de pruebas, configura `TEST_POSTGRES_CONNECTION` con una conexión
de una cuenta que tenga permiso `CREATEDB` y ejecuta `dotnet test`. Cada ejecución crea y elimina una base
de datos con un nombre único.

## Configuración y despliegue

Variables de entorno que debes configurar fuera del desarrollo local:

- `ConnectionStrings__DefaultConnection`: cadena de conexión a PostgreSQL.
- `Jwt__Key`: clave de firma aleatoria de al menos 32 bytes UTF-8; guárdala en un gestor de secretos.
- `Jwt__Issuer` y `Jwt__Audience`: identificadores de la API y sus clientes.
- `Cors__AllowedOrigins__0` y los índices siguientes: orígenes del frontend permitidos explícitamente, si los necesitas.

En producción no se expone Swagger. Configura HTTPS en la infraestructura de alojamiento.
`HttpsRedirection:Enabled` está habilitado fuera de `Development`. Si un proxy inverso termina la conexión
TLS, configura el procesamiento de encabezados reenviados desde proxies de confianza o deshabilita la
redirección de la aplicación y exige HTTPS en el proxy. Configura los proxies de confianza antes de depender
de límites por IP del cliente; de lo contrario, todos los clientes compartirán el límite de la IP del proxy.

Utiliza una cuenta de base de datos con permisos adecuados, almacenamiento persistente, copias de seguridad
y un procedimiento de restauración probado. La cuenta utilizada por la aplicación no necesita permisos para
modificar el esquema si las migraciones se ejecutan por separado.

El Dockerfile utiliza .NET 10 y ejecuta la aplicación con un usuario sin privilegios de root.
Compose está preparado para desarrollo local; no constituye una configuración completa de producción.

Hangfire es opcional (`Hangfire__Enabled=true`) y solo se permite en `Development`. Utiliza almacenamiento
en memoria configurado explícitamente, por lo que pierde los trabajos al reiniciarse. Su panel mantiene la
restricción predeterminada de acceso local. Antes de añadir trabajos en segundo plano en producción,
configura almacenamiento persistente y autorización para el panel.

Se eliminaron las dependencias sin uso de AutoMapper, generación de PDF y paquetes relacionados con SQL Server.

## Crear un proyecto a partir de la plantilla

Desde este repositorio:

```bash
dotnet new install .
dotnet new repoapi -n MiEmpresaApi -o ../MiEmpresaApi
cd ../MiEmpresaApi
python3 scripts/setup-dev.py
docker compose up --build -d
```

El proyecto generado sustituye `Template` en los nombres de proyectos y espacios de nombres.
Los secretos locales y los archivos de compilación quedan excluidos. Ejecuta el script de configuración
en cada proyecto generado para crear credenciales independientes.

## Estructura

- `Template.Api`: endpoints, configuración de arranque, emisión de tokens e inicialización de la base de datos.
- `Template.Data`: contexto de EF para PostgreSQL, migraciones y repositorio genérico.
- `Template.Model`: entidades de Identity.
- `Template.Services`: puntos de extensión para los servicios de la aplicación.
- `Template.Common`: punto de extensión para código compartido.
- `Template.Api.Tests`: pruebas unitarias y de integración con PostgreSQL.
- `Template.Web`: proyecto web opcional de ejemplo; todavía no contiene un frontend implementado.

Las operaciones de escritura del repositorio guardan los cambios inmediatamente y propagan los errores de
la base de datos. Utiliza transacciones explícitas al combinar escrituras, métodos asíncronos para operaciones
de entrada y salida, y `TableNoTracking` con `Skip` y `Take` para consultas paginadas de lectura.
Administra contraseñas, roles y sellos de seguridad mediante los administradores de Identity,
en lugar de actualizaciones con el repositorio genérico.
