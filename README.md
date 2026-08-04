# TPI Backend - Sistema de Turnos Médicos

Trabajo Práctico Integrador de **Desarrollo de Software 2026 - UTN FRT**.

## Descripción

API REST desarrollada con ASP.NET Core para administrar especialidades, médicos, disponibilidades horarias y citas médicas. El sistema permite registrar e iniciar sesión como administrador, iniciar sesión o registrar automáticamente pacientes y proteger las operaciones mediante autenticación JWT y autorización por roles.

Las disponibilidades mensuales generan slots de 30 minutos, excluyen los feriados configurados para Argentina y permiten reservar y cancelar citas sin eliminar información histórica de la base de datos.

## Integrantes

| Apellido y nombre | Legajo | Comisión |
|---|---:|:---:|
| Díaz Montivero, Luisina | 60317 | 3K3 |
| Espinosa, Florencia Noelia | 60521 | 3K3 |
| Ortega Fernández, Victoria | 60847 | 3K3 |
| Torres Gauffin, Tadeo | 60232 | 3K3 |

## Tecnologías utilizadas

- C# y .NET 10
- ASP.NET Core Web API
- Entity Framework Core
- SQL Server LocalDB
- ASP.NET Core Identity
- JWT (JSON Web Token)
- Swagger / OpenAPI
- Serilog
- ASP.NET Core Rate Limiting

## Arquitectura

La solución está organizada en capas:

- `Dsw2026Tpi.Api`: controladores, middleware y configuración de la API.
- `Dsw2026Tpi.Application`: DTOs, interfaces y servicios de aplicación.
- `Dsw2026Tpi.Domain`: entidades, enumeraciones, interfaces y reglas del dominio.
- `Dsw2026Tpi.Data`: persistencia con Entity Framework Core, Identity, migraciones y proveedores de datos.
- `Dsw2026Tpi.CrossCutting`: excepciones, validaciones, recursos, roles y componentes transversales.

## Requisitos previos

Para configurar y ejecutar el proyecto localmente se necesita:

- Visual Studio con la carga de trabajo **ASP.NET y desarrollo web**, o un IDE compatible con .NET.
- .NET 10 SDK.
- SQL Server LocalDB o una instancia compatible de SQL Server.
- Git, si se desea clonar o actualizar el repositorio.

Visual Studio normalmente instala SQL Server LocalDB junto con las herramientas de desarrollo de .NET.

## Configuración y ejecución local

### 1. Obtener y abrir el proyecto

Clonar o descargar el repositorio y abrir el archivo de solución:

```text
Dsw2026Tpi.slnx
```

En Visual Studio, verificar que `Dsw2026Tpi.Api` sea el proyecto de inicio.

### 2. Restaurar las dependencias

Visual Studio restaura los paquetes NuGet automáticamente. También se puede hacer desde una terminal ubicada en la raíz del repositorio:

```powershell
dotnet restore Dsw2026Tpi.slnx
```

### 3. Revisar la conexión a la base de datos

La cadena de conexión del entorno de desarrollo se encuentra en:

```text
Dsw2026Tpi.Api/appsettings.Development.json
```

La configuración incluida utiliza SQL Server LocalDB y una base llamada `Dsw2026Tpi`. Si se utiliza otra instancia de SQL Server, se debe modificar `ConnectionStrings:DefaultConnection`.

### 4. Aplicar las migraciones

La aplicación utiliza dos contextos de Entity Framework Core:

- `AuthenticationDbContext`: usuarios, roles y autenticación con Identity.
- `Dsw2026TpiDbContext`: especialidades, médicos, pacientes, disponibilidades, slots y citas.

#### Desde Visual Studio

Abrir:

```text
Herramientas > Administrador de paquetes NuGet > Consola del Administrador de paquetes
```

Seleccionar `Dsw2026Tpi.Data` como proyecto predeterminado y ejecutar:

```powershell
Update-Database -Context AuthenticationDbContext
Update-Database -Context Dsw2026TpiDbContext
```

### 5. Ejecutar la API

Desde Visual Studio, seleccionar el perfil `https` y ejecutar `Dsw2026Tpi.Api`.

También se puede ejecutar desde la raíz del repositorio:

```powershell
dotnet run --project Dsw2026Tpi.Api
```

En el entorno de desarrollo, Swagger se abre automáticamente. Las direcciones configuradas son:

```text
https://localhost:7075/swagger/index.html
http://localhost:5278/swagger/index.html
```

## Endpoints implementados

### Autenticación

| Método | Endpoint | Autorización | Descripción |
|---|---|---|---|
| POST | `/api/auth/admin/register` | Pública | Registra un usuario administrador. |
| POST | `/api/auth/admin/login` | Pública | Valida email y contraseña y devuelve un JWT de administrador. |
| POST | `/api/auth/patient/login` | Pública | Inicia sesión como paciente o lo registra automáticamente si no existe. |

## Primer uso

### 1. Registrar un administrador

Si todavía no existe un administrador, ejecutar:

```http
POST /api/auth/admin/register
```

Body de ejemplo:

```json
{
  "email": "admin@admin.com",
  "password": "Admin123!"
}
```

La contraseña debe tener al menos 8 caracteres e incluir una letra mayúscula, una letra minúscula, un número y un carácter no alfanumérico.

### 2. Iniciar sesión como administrador

Ejecutar:

```http
POST /api/auth/admin/login
```

```json
{
  "email": "admin@admin.com",
  "password": "Admin123!"
}
```

La respuesta contiene un token JWT con el rol `Administrador`.

### 3. Autorizar las solicitudes en Swagger

Para utilizar los endpoints protegidos:

1. Copiar el token devuelto por el login.
2. Presionar el botón `Authorize` de Swagger.
3. Ingresar `Bearer`, un espacio y el token.
4. Confirmar la autorización.

```text
Bearer eyJhbGciOiJIUzI1NiIs...
```

### 4. Iniciar sesión como paciente

Ejecutar:

```http
POST /api/auth/patient/login
```

```json
{
  "email": "paciente@gmail.com",
  "dni": 40123456
}
```

Si el email y el DNI todavía no existen, el sistema crea automáticamente el usuario y el paciente, les asigna el rol `Paciente` y devuelve un token JWT que incluye el DNI.

### Especialidades

| Método | Endpoint | Autorización | Descripción |
|---|---|---|---|
| GET | `/api/specialties?pageSize=10&pageIndex=1&name=` | Administrador o Paciente | Lista especialidades con paginación y filtro opcional por nombre. |
| POST | `/api/specialties` | Administrador | Crea una especialidad y devuelve la entidad creada. |
| PUT | `/api/specialties/{id}` | Administrador | Actualiza una especialidad y devuelve la entidad actualizada. |
| DELETE | `/api/specialties/{id}` | Administrador | Realiza la eliminación lógica de una especialidad. |

Body para `POST` y `PUT`:

```json
{
  "name": "Cardiología",
  "description": "Atención cardiológica general"
}
```
Validaciones:`name` es obligatorio (de 3 a 100 caracteres), `description` es obligatorio (de 10 a 100 caracteres)

Respuesta del `GET`
```json
{
  "pageSize": 10,
  "pageIndex": 1,
  "data": [
   {
     "id": "Guid",
     "name": "string",
     "descripcion": "string"
   },
  ],
  "total": number
}
```
El filtro `name` es opcional. Si no se envía, se listan todas las especialidades no eliminadas. Los registros eliminados lógicamente no aparecen en las consultas normales.

### Médicos

| Método | Endpoint | Autorización | Descripción |
|---|---|---|---|
| GET | `/api/doctors?pageSize=10&pageIndex=1&name=` | Administrador o Paciente | Lista médicos activos con paginación y filtro opcional por nombre. |
| GET | `/api/doctors/{id}/availabilities` | Administrador | Obtiene las reglas de disponibilidad del mes actual para un médico. |
| POST | `/api/doctors` | Administrador | Crea un médico y devuelve la entidad creada. |
| PUT | `/api/doctors/{id}` | Administrador | Actualiza un médico y devuelve la entidad actualizada. |
| DELETE | `/api/doctors/{id}` | Administrador | Realiza la eliminación lógica de un médico. |

Body para `POST` y `PUT`:

```json
{
  "name": "Tadeo Torres",
  "licenseNumber": "MP12345",
  "specialtyId": "GUID-DE-LA-ESPECIALIDAD"
}
```

Validaciones: la especialidad indicada debe existir. El campo `licenseNumber` puede omitirse enviando `null`.

### Disponibilidades

| Método | Endpoint | Autorización | Descripción |
|---|---|---|---|
| POST | `/api/availabilities` | Administrador | Registra disponibilidades y genera slots para el resto del mes actual. |
| PUT | `/api/availabilities` | Administrador | Actualiza las disponibilidades futuras no reservadas del mes actual. |

Body para `POST` y `PUT`:

```json
{
  "doctorId": "GUID-DEL-MEDICO",
  "days": [
    {
      "day": "LUNES",
      "startTime": "09:00:00",
      "endTime": "12:00:00"
    },
    {
      "day": "MIERCOLES",
      "startTime": "14:00:00",
      "endTime": "17:00:00"
    }
  ]
}
```

Cada regla genera slots de 30 minutos. El sistema valida los horarios y los solapamientos y no genera slots para las fechas incluidas en `Dsw2026Tpi.Data/Sources/holidays.json`.

### Citas de pacientes

| Método | Endpoint | Autorización | Descripción |
|---|---|---|---|
| POST | `/api/appointments` | Paciente | Reserva una cita en un slot disponible. |
| GET | `/api/appointments/patient?dni=40123456` | Paciente | Lista las citas reservadas, activas y futuras del paciente autenticado. |
| DELETE | `/api/appointments/{id}` | Paciente | Cancela una cita propia y vuelve a liberar el slot. |

Body para reservar una cita:

```json
{
  "doctorId": "GUID-DEL-MEDICO",
  "availabilitySlotId": "GUID-DEL-SLOT",
  "patient": {
    "dni": 40123456
  },
  "reason": "Consulta médica general"
}
```

Validaciones: el DNI enviado al crear, consultar o cancelar una cita debe corresponder al paciente autenticado. En la consulta de citas, si se omite el parámetro `dni`, se utiliza el DNI incluido en el token. Si se envía, debe coincidir con el token.

Para las pruebas actuales, el identificador de un slot disponible se puede consultar directamente en la tabla `AvailabilitySlots` de la base de datos.

### Consultas administrativas de citas

| Método | Endpoint | Autorización | Descripción |
|---|---|---|---|
| GET | `/api/appointments?date=2026-08-01` | Administrador | Devuelve las citas correspondientes a la fecha obligatoria. |
| GET | `/api/appointments/search` | Administrador | Realiza una búsqueda combinada y paginada de citas. |

La búsqueda avanzada admite los siguientes filtros opcionales:

- `specialtyId`
- `doctorId`
- `dni`
- `date`
- `pageSize`, con valor predeterminado `10`
- `pageIndex`, con valor predeterminado `1`

Ejemplo sin filtros:

```http
GET /api/appointments/search?pageSize=10&pageIndex=1
```

Ejemplo con filtros combinados:

```http
GET /api/appointments/search?pageSize=10&pageIndex=1&doctorId=GUID-DEL-MEDICO&dni=40123456&date=2026-08-04
```

### Estado de la aplicación

| Método | Endpoint | Autorización | Descripción |
|---|---|---|---|
| GET | `/health-check` | Pública | Comprueba que la API se encuentre en funcionamiento. |

## Estados utilizados

Los slots de disponibilidad pueden tener los siguientes estados:

- `AVAILABLE`: disponible para reservar.
- `BOOKED`: reservado.
- `BLOCKED`: bloqueado.

Las citas pueden tener los siguientes estados:

- `BOOKED`: reservada.
- `CANCELLED`: cancelada.
- `ATTENDED`: atendida.
- `NO_SHOW`: el paciente no asistió.

## Paginación

Las respuestas paginadas tienen el siguiente formato general:

```json
{
  "pageSize": 10,
  "pageIndex": 1,
  "data": [],
  "total": 0
}
```

`total` representa la cantidad total de registros que cumplen el filtro, no solamente los elementos contenidos en la página actual.

## Manejo de errores

La API utiliza un middleware global para transformar las excepciones en respuestas consistentes:

```json
{
  "errorCode": "VALIDATION_ERROR",
  "message": "Uno o más errores de validación ocurrieron.",
  "details": [
    {
      "field": "name",
      "issue": "El nombre es inválido."
    }
  ]
}
```

Según el error, la API puede devolver:

- `400 Bad Request`: datos de entrada inválidos.
- `401 Unauthorized`: autenticación inválida o token ausente.
- `403 Forbidden`: el usuario está autenticado, pero no tiene permiso para la operación.
- `404 Not Found`: entidad inexistente.
- `409 Conflict`: conflicto de negocio, disponibilidad o reserva.
- `429 Too Many Requests`: se superó el límite de solicitudes.
- `500 Internal Server Error`: error no controlado del servidor.

## Rate limiting

La API utiliza el middleware de rate limiting provisto por ASP.NET Core con un algoritmo de ventana fija de un minuto.

| Operación | Límite | Partición |
|---|---:|---|
| Login de administrador | 5 solicitudes por minuto | Dirección IP |
| Login de paciente | 10 solicitudes por minuto | Dirección IP |
| Reserva de citas | 5 solicitudes por minuto | Usuario autenticado |
| Política general | 100 solicitudes por minuto | Usuario autenticado o dirección IP |

Los endpoints con una política específica también están sujetos a la política general.

La configuración utiliza `QueueLimit = 0`, por lo que las solicitudes que exceden el límite no se encolan: se rechazan inmediatamente con el código HTTP `429 Too Many Requests`.

Cada rechazo:

- Respeta el formato general de errores de la API.
- Se registra mediante el sistema de logging.
- No llega al controlador ni al servicio correspondiente.
- Debe ser enviado nuevamente por el cliente cuando existan permisos disponibles.

## Pruebas unitarias
El proyecto Dsw2026Tpi.Application.Tests contiene pruebas unitarias iniciales para casos de creación de médicos y eliminación lógica de especialidades, utilizando xUnit y NSubstitute para sustituir IPersistence.

## Consideraciones finales

- Los identificadores de las entidades utilizan GUID.
- Las propiedades JSON se exponen utilizando `camelCase`.
- Los tokens JWT vencen después de 60 minutos.
- Los roles del sistema son `Administrador` y `Paciente`.
- Las contraseñas son administradas por ASP.NET Core Identity y no se guardan como texto plano.
- Las eliminaciones de especialidades y médicos son lógicas: el registro permanece en la base con la marca de eliminado.
- La cancelación de una cita cambia su estado a `CANCELLED` y libera nuevamente el slot.
- El sistema evita reservar slots pasados, ocupados o pertenecientes a otro médico.
- Las credenciales y claves sensibles de ambientes reales no deben incorporarse al repositorio.

