# EmployeeAPI

## Descripción general y alcance

EmployeeAPI es una API REST desarrollada con .NET 8 y MongoDB para gestionar información de empleados y registrar eventos de asistencia. Sus funcionalidades se dividen en dos dominios principales.

### Dominio de personas

Este dominio permite:

- Crear, consultar, actualizar y eliminar empleados.
- Filtrar empleados por departamento y posición.
- Consultar departamentos y las posiciones asociadas.
- Crear implícitamente departamentos y posiciones al registrar un empleado cuando todavía no existen.

### Dominio de asistencia

Este dominio permite:

- Administrar dispositivos de marcación.
- Enrolar a un empleado mediante un PIN, de forma global o asociado a un dispositivo.
- Consultar y crear tipos de marcación.
- Registrar marcaciones de asistencia.
- Identificar al empleado de una marcación mediante su identificador, DNI o PIN.
- Consultar marcaciones aplicando filtros por empleado, dispositivo y rango de fechas.

La API utiliza autenticación mediante tokens JWT para proteger sus operaciones. El login, el health check y la documentación Swagger cuentan con acceso público según las condiciones definidas por el middleware de autenticación.

## Requisitos previos

EmployeeAPI puede ejecutarse directamente en el equipo o mediante Docker Compose. Solo necesitas preparar uno de los dos modos.

| Modo | Herramientas necesarias |
|---|---|
| Ejecución local | .NET SDK 8 y una instancia accesible de MongoDB |
| Docker Compose | Docker con soporte para Docker Compose |

Si todavía no tienes el repositorio en tu equipo, también necesitarás Git para clonarlo.

### Comprobar las herramientas

Para comprobar Git:

```shell
git --version
```

Para la ejecución local, verifica que el SDK instalado corresponda a .NET 8:

```shell
dotnet --version
```

El resultado debe comenzar con `8.`.

También necesitas una instancia de MongoDB accesible. Puede estar instalada localmente, ejecutarse en otro contenedor o encontrarse en un servidor remoto.

> La configuración incluida utiliza `mongodb://mongodb:27017/`, donde `mongodb` es el nombre del servicio dentro de Docker Compose. Al ejecutar la API directamente en el equipo normalmente será necesario utilizar otro host, por ejemplo `localhost`. La configuración del override se explica en la siguiente sección.

Para ejecutar todo mediante contenedores, verifica Docker y Docker Compose:

```shell
docker --version
docker compose version
```

En este modo no necesitas instalar .NET ni MongoDB directamente: el `Dockerfile` construye la API y Docker Compose inicia la API, MongoDB y Mongo Express.

## Configuración

La aplicación obtiene su configuración desde `appsettings.json`. Los valores pueden reemplazarse mediante variables de entorno sin modificar el archivo incluido en el repositorio.

En las variables de entorno, ASP.NET Core representa la separación entre secciones mediante dos guiones bajos. Por ejemplo:

```text
MongoDB:ConnectionURI → MongoDB__ConnectionURI
Jwt:SecretKey         → Jwt__SecretKey
```

### Opciones disponibles

| Variable de entorno | Propósito | Valor incluido para desarrollo |
|---|---|---|
| `MongoDB__ConnectionURI` | Dirección utilizada para conectar con MongoDB | `mongodb://mongodb:27017/` |
| `MongoDB__DatabaseName` | Nombre de la base de datos | `sample_employee` |
| `MongoDB__CollectionName` | Colección de empleados | `employee` |
| `MongoDB__CollectionUsers` | Colección de usuarios | `user` |
| `Jwt__SecretKey` | Clave utilizada para firmar y validar los tokens JWT | Valor de desarrollo incluido en `appsettings.json` |
| `Jwt__ExpirationHours` | Duración del token expresada en horas | `12` |
| `ASPNETCORE_ENVIRONMENT` | Entorno de ejecución de ASP.NET Core | `Development` en los perfiles locales y Docker Compose |

### Configuración para ejecución local

Al ejecutar la API directamente en el equipo, normalmente se debe reemplazar la URI de MongoDB porque el nombre `mongodb` pertenece a la red de Docker Compose.

En PowerShell:

```powershell
$env:MongoDB__ConnectionURI = "mongodb://localhost:27017/"
$env:Jwt__SecretKey = "<CLAVE_DE_DESARROLLO_DE_32_CARACTERES_O_MAS>"
```

En Bash:

```bash
export MongoDB__ConnectionURI="mongodb://localhost:27017/"
export Jwt__SecretKey="<CLAVE_DE_DESARROLLO_DE_32_CARACTERES_O_MAS>"
```

Estas variables afectan únicamente a la terminal actual. Deben definirse nuevamente al abrir una terminal distinta.

### Uso de .NET User Secrets

El proyecto tiene configurado un `UserSecretsId`. Para desarrollo local también es posible guardar los valores fuera del repositorio:

```shell
dotnet user-secrets set "MongoDB:ConnectionURI" "mongodb://localhost:27017/"
dotnet user-secrets set "Jwt:SecretKey" "<CLAVE_DE_DESARROLLO_DE_32_CARACTERES_O_MAS>"
```

Los valores guardados mediante User Secrets no se escriben en `appsettings.json` ni se incluyen en Git.

### Tratamiento de secretos

Los valores incluidos en el repositorio son únicamente valores de desarrollo.

- No utilices el secreto JWT incluido en un ambiente real.
- No escribas secretos reales directamente en `README.md`, `appsettings.json` o `docker-compose.yml`.
- Sustituye los textos entre `<...>` por valores propios en tu entorno.
- La clave JWT debe tener al menos 32 caracteres, según la definición de `JwtSettings`.
- Una URI de MongoDB que contenga usuario y contraseña también debe tratarse como información sensible.

## Ejecución del proyecto

### Obtener el repositorio

```shell
git clone https://github.com/Manuu201/EmployeeAPI-Challenge.git
cd EmployeeAPI-Challenge
```

Después puedes elegir entre ejecución local o Docker Compose.

### Opción 1: ejecución local

Este modo ejecuta la API directamente mediante el SDK de .NET. Antes de continuar:

1. Comprueba que MongoDB esté disponible.
2. Configura la conexión y el secreto JWT como se explicó en la sección anterior.
3. Ejecuta los siguientes comandos desde la raíz del repositorio.

Restaura las dependencias de la solución:

```shell
dotnet restore EmployeeAPI.sln
```

Inicia la API utilizando el perfil HTTP:

```shell
dotnet run --project EmployeeAPI.csproj --launch-profile http
```

El perfil `http` configura:

```text
API:     http://localhost:5146
Swagger: http://localhost:5146/swagger/index.html
Health:  http://localhost:5146/health
```

Las variables de entorno configuradas en PowerShell o Bash deben permanecer definidas en la misma terminal desde la que se ejecuta `dotnet run`.

La API necesita conectarse con MongoDB durante el arranque para crear índices e inicializar los tipos de marcación. Si MongoDB no está disponible, la aplicación no llegará a comenzar a escuchar requests.

### Opción 2: Docker Compose

Este modo construye la API e inicia los siguientes servicios:

- EmployeeAPI.
- MongoDB.
- Mongo Express.

Desde la raíz del repositorio ejecuta:

```shell
docker compose up --build -d
```

Comprueba el estado de los contenedores:

```shell
docker compose ps
```

Los servicios quedan expuestos en:

| Servicio | Dirección |
|---|---|
| API | `http://localhost:8080` |
| Swagger | `http://localhost:8080/swagger/index.html` |
| Health check | `http://localhost:8080/health` |
| MongoDB | `mongodb://localhost:27017/` |
| Mongo Express | `http://localhost:8081` |

Mongo Express es una herramienta auxiliar para inspeccionar MongoDB; no es necesaria para utilizar la API.

### Ejecutar la API con overrides en Docker Compose

El `docker-compose.yml` actual define únicamente `ASPNETCORE_ENVIRONMENT` para la API. Por eso, los overrides configurados en la terminal no se reenvían automáticamente al contenedor al utilizar `docker compose up`.

Para iniciar la API con otros valores sin modificar el archivo, utiliza el siguiente comando en lugar del arranque normal:

```shell
docker compose run --build --rm -p 8080:8080 -e MongoDB__ConnectionURI="mongodb://mongodb:27017/" -e Jwt__SecretKey="<CLAVE_DE_DESARROLLO_DE_32_CARACTERES_O_MAS>" api
```

MongoDB se inicia como dependencia. La API permanece en primer plano y puede detenerse con `Ctrl+C`; `--rm` elimina ese contenedor de la API al finalizar. Posteriormente, `docker compose down` detiene los servicios restantes. Mongo Express no se inicia con este comando y no es necesario para utilizar la API.

Para observar los mensajes de la API:

```shell
docker compose logs -f api
```

Presiona `Ctrl+C` para dejar de seguir los logs. Esto no detiene los contenedores.

Para detener y retirar los contenedores:

```shell
docker compose down
```

Este comando conserva el volumen `mongo-data`.

> Para eliminar también los datos almacenados en MongoDB se puede utilizar `docker compose down -v`. Este comando borra el volumen persistente y debe usarse solamente cuando se quiera reiniciar completamente la base de datos.

### Verificar la ejecución

En PowerShell:

```powershell
Invoke-RestMethod http://localhost:8080/health
```

Con `curl`:

```shell
curl http://localhost:8080/health
```

Para la ejecución local sustituye el puerto `8080` por `5146`.

Cuando la API y MongoDB están disponibles, el endpoint responde con un estado equivalente a:

```json
{
  "status": "Healthy",
  "database": "Reachable",
  "checkedAtUtc": "<FECHA_Y_HORA_UTC>"
}
```

## Autenticación

Salvo las excepciones indicadas más adelante, los endpoints requieren un token JWT válido en el encabezado `Authorization`.

### Iniciar sesión

Para Docker:

```shell
curl -X POST http://localhost:8080/api/auth/login \
  -H "Content-Type: application/json" \
  -d "{\"username\":\"admin\",\"password\":\"admin\"}"
```

Para ejecución local, sustituye el puerto `8080` por `5146`.

El body debe contener:

```json
{
  "username": "admin",
  "password": "admin"
}
```

Las validaciones de entrada son:

| Campo | Reglas |
|---|---|
| `username` | Obligatorio; entre 3 y 50 caracteres |
| `password` | Obligatorio; entre 4 y 100 caracteres |

Cuando las credenciales son válidas, la API responde con una estructura equivalente a:

```json
{
  "username": "admin",
  "token": "<TOKEN_JWT>",
  "expiresAtUtc": "<FECHA_Y_HORA_UTC>"
}
```

`<TOKEN_JWT>` y `<FECHA_Y_HORA_UTC>` son marcadores de ejemplo. No representan credenciales ni valores reales.

Si el usuario no existe o la contraseña es incorrecta, la API responde `401 Unauthorized`. Si el body no cumple las validaciones anteriores, responde `400 Bad Request`.

> Comportamiento actual: si la colección de usuarios está vacía, el primer intento de login crea el usuario `admin` con contraseña `admin`. Estas son credenciales conocidas de inicialización, no un secreto seguro ni una configuración apropiada para producción.

### Utilizar el Bearer token

Copia el valor `token` retornado por el login y envíalo en las operaciones protegidas:

```http
Authorization: Bearer <TOKEN_JWT>
```

Ejemplo:

```shell
curl http://localhost:8080/api/employee \
  -H "Authorization: Bearer <TOKEN_JWT>"
```

El middleware comprueba la firma y la expiración del token antes de permitir que la request llegue al controller. Si el encabezado está ausente o el token no es válido, responde `401 Unauthorized`.

### Vigencia y renovación

La duración predeterminada del token es de 12 horas y puede cambiarse mediante `Jwt__ExpirationHours`.

Al iniciar sesión:

1. Si el usuario no tiene token o el token almacenado ya no es válido, la API genera uno nuevo y lo guarda en MongoDB.
2. Si el token almacenado continúa siendo válido, la API reutiliza ese mismo token.
3. `expiresAtUtc` informa la fecha y hora de expiración en UTC.

Los tokens se firman mediante HMAC-SHA256 utilizando `Jwt__SecretKey`.

### Rutas públicas

El middleware permite acceder sin Bearer token a:

| Método | Ruta | Propósito |
|---|---|---|
| `POST` | `/api/auth/login` | Obtener el token |
| `GET` | `/health` | Comprobar la API y MongoDB |
| Cualquiera | `/swagger/*` | Consultar Swagger |
| `OPTIONS` | Cualquier ruta | Atender solicitudes preliminares del navegador |

### Limitación de autorización

El sistema valida que el token sea correcto, pero no implementa roles ni permisos diferentes por usuario. Cualquier usuario con un token válido obtiene el mismo nivel de acceso a los endpoints protegidos.

## Catálogo de endpoints

- Todos los endpoints, excepto login, health y Swagger, requieren Bearer token.
- Un body JSON mal formado o que incumpla validaciones de modelo puede producir `400 Bad Request`.

| Área | Método y ruta | Entrada | Resultado y errores relevantes |
|---|---|---|---|
| Autenticación | `POST /api/auth/login` | Body: `username`, `password` | `200` con token; `400` validación; `401` credenciales |
| Salud | `GET /health` | Sin entrada | `200` saludable; `503` sin conexión a MongoDB |
| Empleados | `GET /api/employee` | Query opcional: `departmentName`, `positionName` | `200` con lista |
| Empleados | `GET /api/employee/{id}` | `id` de 24 caracteres | `200`; `404` si no existe |
| Empleados | `POST /api/employee` | Body: `name`, `email`, `department`, `position`; `dni` opcional; omitir `id` | `201`; `400` si incluye `id`; `409` por email |
| Empleados | `PUT /api/employee/{id}` | `id` y body Employee completo | `200`; `404` |
| Empleados | `PATCH /api/employee/{id}` | `id`; `name`, `email` o `department` | `200`; `404` |
| Empleados | `DELETE /api/employee/{id}` | `id` de 24 caracteres | `204`; `404` |
| Organización | `GET /api/employee/departments` | Sin entrada | `200` con departamentos |
| Organización | `GET /api/employee/departments/{departmentId}/positions` | `departmentId` | `200` con posiciones |
| Dispositivos | `GET /api/device` | Sin entrada | `200` con lista |
| Dispositivos | `GET /api/device/{id}` | `id` de 24 caracteres | `200`; `404` |
| Dispositivos | `POST /api/device` | `name`, `location`, `timezone`; sin `id` | `201`; `400` validación; `409` por nombre |
| Dispositivos | `PUT /api/device/{id}` | `id` y body Device completo | `200`; `400` validación; `404` |
| Dispositivos | `DELETE /api/device/{id}` | `id` de 24 caracteres | `204`; `404` |
| Enrolamientos | `GET /api/enrollment` | Query opcional: `employeeId`, `deviceId` | `200` con lista |
| Enrolamientos | `GET /api/enrollment/{id}` | `id` de 24 caracteres | `200`; `404` |
| Enrolamientos | `POST /api/enrollment` | `employee_Id`, `pin`; `device_Id` opcional | `201`; `400` validación; `404` relación inexistente; `409` PIN ocupado |
| Enrolamientos | `DELETE /api/enrollment/{id}` | `id` de 24 caracteres | `204`; `404` |
| Marcaciones | `GET /api/punch` | Query opcional: `employeeId`, `deviceId`, `from`, `to` | `200` con lista; `400` si `from > to` |
| Marcaciones | `GET /api/punch/{id}` | `id` de 24 caracteres | `200`; `404` |
| Marcaciones | `POST /api/punch` | `device_Id`, `punchType` y al menos uno de: `employee_Id`, `dni` o `pin` | `201`; `400`, `404` o `409` según validación |
| Tipos de marca | `GET /api/punch/types` | Sin entrada | `200` con tipos |
| Tipos de marca | `POST /api/punch/types` | `code`, `name`; `description` opcional | `201`; `400` validación; `409` por código |
| Residual | `GET /weatherforecast` | Sin entrada | `200` con datos simulados |

Al crear un empleado, la API crea el departamento o la posición cuando todavía no existen y asigna sus identificadores al empleado.

`GET /weatherforecast` existe en el código, pero no forma parte de los dominios descritos por el challenge; parece provenir de la plantilla inicial del proyecto.

## Ejemplo reproducible: registrar y consultar una marcación

Los bloques siguientes utilizan formato HTTP y pueden reproducirse en Postman o mediante Swagger.

Este ejemplo recorre el flujo completo:

```text
Employee + Device → Enrollment → Punch → consulta
```

Antes de comenzar:

1. Inicia la API.
2. Realiza el login explicado en [Autenticación](#autenticación).
3. Conserva el token obtenido.
4. Utiliza una URL base:

```text
Docker: http://localhost:8080
Local:  http://localhost:5146
```

En las siguientes requests debes reemplazar:

| Marcador | Valor |
|---|---|
| `<BASE_URL>` | URL base correspondiente al modo de ejecución |
| `<TOKEN_JWT>` | Token obtenido mediante el login |
| `<EMPLOYEE_ID>` | Identificador retornado al crear el empleado |
| `<DEVICE_ID>` | Identificador retornado al crear el dispositivo |

Utiliza un email, nombre de dispositivo y PIN que todavía no existan en la base de datos.

### 1. Preparar un empleado

Enrollment necesita un empleado existente.

```http
POST <BASE_URL>/api/employee
Authorization: Bearer <TOKEN_JWT>
Content-Type: application/json

{
  "name": "Ana Pérez",
  "email": "ana.perez@example.com",
  "dni": "12345678-9",
  "department": "Operaciones",
  "position": "Analista"
}
```

Respuesta `201 Created`, abreviada:

```json
{
  "id": "<EMPLOYEE_ID>",
  "name": "Ana Pérez",
  "department_Id": "<DEPARTMENT_ID>",
  "position_Id": "<POSITION_ID>"
}
```

Conserva `id` como `<EMPLOYEE_ID>`.

### 2. Crear el dispositivo

```http
POST <BASE_URL>/api/device
Authorization: Bearer <TOKEN_JWT>
Content-Type: application/json

{
  "name": "Reloj Recepción",
  "location": "Casa Matriz",
  "timezone": "America/Santiago"
}
```

Respuesta `201 Created`:

```json
{
  "id": "<DEVICE_ID>",
  "name": "Reloj Recepción",
  "location": "Casa Matriz",
  "timezone": "America/Santiago"
}
```

Conserva `id` como `<DEVICE_ID>`.

### 3. Enrolar el PIN

```http
POST <BASE_URL>/api/enrollment
Authorization: Bearer <TOKEN_JWT>
Content-Type: application/json

{
  "employee_Id": "<EMPLOYEE_ID>",
  "device_Id": "<DEVICE_ID>",
  "pin": "4827",
  "active": true
}
```

Respuesta `201 Created`:

```json
{
  "id": "<ENROLLMENT_ID>",
  "employee_Id": "<EMPLOYEE_ID>",
  "device_Id": "<DEVICE_ID>",
  "pin": "4827",
  "active": true
}
```

Este enrollment permite utilizar el PIN `4827` en el dispositivo indicado.

### 4. Registrar la marcación mediante PIN

Los tipos `IN`, `OUT`, `BREAK_IN` y `BREAK_OUT` son creados durante el arranque de la API.

```http
POST <BASE_URL>/api/punch
Authorization: Bearer <TOKEN_JWT>
Content-Type: application/json

{
  "device_Id": "<DEVICE_ID>",
  "punchType": "IN",
  "pin": "4827"
}
```

Respuesta `201 Created`, abreviada:

```json
{
  "id": "<PUNCH_ID>",
  "device_Id": "<DEVICE_ID>",
  "employee_Id": "<EMPLOYEE_ID>",
  "punchType": "IN",
  "punch_Dtm": "<FECHA_Y_HORA_UTC>",
  "timezone": "America/Santiago",
  "status": "VALID"
}
```

La API obtiene el empleado desde Enrollment y completa `employee_Id`. Como la request no incluye fecha ni zona horaria, utiliza la hora UTC de recepción y la zona del dispositivo.

No es necesario enviar `punchType_Id`. La API busca el tipo mediante `punchType` y asigna internamente su identificador.

### 5. Consultar las marcaciones

```http
GET <BASE_URL>/api/punch?employeeId=<EMPLOYEE_ID>&deviceId=<DEVICE_ID>
Authorization: Bearer <TOKEN_JWT>
```

Respuesta `200 OK`, abreviada:

```json
[
  {
    "id": "<PUNCH_ID>",
    "device_Id": "<DEVICE_ID>",
    "employee_Id": "<EMPLOYEE_ID>",
    "punchType": "IN",
    "punch_Dtm": "<FECHA_Y_HORA_UTC>",
    "status": "VALID"
  }
]
```

## Consideraciones conocidas y limitaciones

- La autenticación no distingue roles ni permisos.
- Las contraseñas de los usuarios se almacenan sin hash.
- La actualización de empleados puede dejar inconsistencias entre los nombres y los identificadores de Department y Position.
- La eliminación de entidades no elimina automáticamente sus referencias relacionadas.

La evidencia y el análisis detallado de estos hallazgos se presentan en `ANALISIS_TECNICO.md`. En esta etapa no se implementan correcciones.
