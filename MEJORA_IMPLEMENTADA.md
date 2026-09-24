# Mejora implementada: manejo de email duplicado concurrente

## 1. Problema detectado

`POST /api/employee` consulta primero `GetByEmailAsync` para comprobar si ya existe un Employee con el mismo email.

En ejecución secuencial se confirmó el siguiente comportamiento:

- email nuevo: `201 Created`;
- email repetido: `409 Conflict`.

Sin embargo, bajo concurrencia varias requests podían superar el pre-check antes de que la primera inserción finalizara. El índice único de `Employee.Email` impedía que MongoDB guardara documentos duplicados, pero las colisiones restantes producían una `MongoWriteException` no manejada, con categoría `DuplicateKey`, código `11000` e índice `Email_1`. Como consecuencia, la API respondía `500 Internal Server Error`.

La prueba base con 10 requests concurrentes produjo:

- 1 respuesta `201 Created`;
- 9 respuestas `500 Internal Server Error`;
- 1 Employee persistido.

## 2. Decisión técnica

Se mantuvo el pre-check existente mediante `GetByEmailAsync`.

La llamada a `CreateAsync(newEmployee)` se rodeó con una captura específica de `MongoWriteException`, usando el filtro:

`ex.WriteError?.Category == ServerErrorCategory.DuplicateKey`

Cuando se cumple esta condición, el endpoint devuelve `409 Conflict` con el mismo mensaje utilizado por el pre-check secuencial:

`An employee with the email '...' already exists.`

No se capturan genéricamente otras excepciones de MongoDB, por lo que los errores que no sean `DuplicateKey` continúan propagándose como antes.

La captura se realizó en `EmployeeController.Post` porque este componente ya traduce el email duplicado a una respuesta HTTP. `MongoDBService` conserva su responsabilidad de persistencia y el middleware de autenticación no conoce el contexto funcional del endpoint.

La mejora no elimina la condición de carrera: maneja correctamente la colisión detectada por el índice único de MongoDB.

## 3. Archivo modificado

`Controllers/EmployeeController.cs`

Cambios realizados:

- se agregó `using MongoDB.Driver`;
- se agregó un `try/catch` alrededor de `CreateAsync(newEmployee)`.

## 4. Riesgos y efectos secundarios

- Si en el futuro Employee incorpora otro índice único, una colisión `DuplicateKey` de ese índice también podría recibir el mensaje de email duplicado.
- La mejora no resuelve la falta de atomicidad entre la creación de Department, Position y Employee.
- Los errores de MongoDB que no sean `DuplicateKey` continúan propagándose como errores de servidor.
- No se agregaron dependencias ni infraestructura nueva.

## 5. Evidencia de validación

### Antes de la mejora

Validación secuencial:

- email nuevo: `201 Created`;
- email repetido: `409 Conflict`.

Validación concurrente:

- 10 requests con el mismo email;
- 1 respuesta `201 Created`;
- 9 respuestas `500 Internal Server Error`;
- 1 Employee persistido.

Los logs registraron:

- `MongoWriteException`;
- categoría `DuplicateKey`;
- código `11000`;
- índice `Email_1`.

### Después de la mejora

Se repitió la misma prueba concurrente con el email nuevo:

`race.email.20260924.02@example.com`

Resultado:

- 1 respuesta `201 Created`;
- 9 respuestas `409 Conflict`;
- 0 respuestas `500 Internal Server Error`;
- 1 Employee persistido.

Durante la prueba no aparecieron excepciones no manejadas asociadas al conflicto.

La prueba concurrente puede reproducirse mediante el script `prueba-concurrencia-email.ps1`.

Evidencias relacionadas:

- `Evidencia Problemas/Emails Concurrentes/Antes/Prueba Postman 201 .png`
- `Evidencia Problemas/Emails Concurrentes/Antes/Prueba Postman 409.png`
- `Evidencia Problemas/Emails Concurrentes/Antes/Pantallazo de CMD al ejecutar el script .ps1.png`
- `Evidencia Problemas/Emails Concurrentes/Antes/Log Docker DuplicateKey.png`
- `Evidencia Problemas/Emails Concurrentes/Despues/Pantallazo de CMD al ejecutar el script .ps1.png`
- `Evidencia Problemas/Emails Concurrentes/Despues/Log Docker sin excepciones.png`
- `Evidencia Problemas/Emails Concurrentes/Despues/Prueba Postman 201 .png`
- `Evidencia Problemas/Emails Concurrentes/Despues/Prueba Postman 409.png`

## 6. Pendientes fuera del alcance

- Evaluar un manejo específico si se agregan otros índices únicos a Employee.
- La atomicidad completa del alta de Employee queda fuera del alcance de esta mejora.
