Quiero un sistema de auditoría en 3 capas en dotnet core.

Quiero los siguientes proyectos:
- Audit.Dom:
  - Entidad Audit: 
    - Id de tipo Guid
    - UserId de tipo string
    - EntityId de tipo Guid
    - EntityName de tipo string
    - Action de tipo Enum (Create, Update, Delete, Read)
    - Timestamp de tipo DateTime
  - Entidad AuditDetail:
    - Id de tipo Guid
    - AuditId de tipo Guid
    - PropertyName de tipo string
    - OldValue de tipo string
    - NewValue de tipo string
  - Interfaz IAuditService con métodos para crear auditorías

- Audit.Data:
    - Inicialmente se usará una base de datos sqlite. Preferiría usar Dapper en lugar de Entity Framework, pero si es necesario usar EF con DbContext con DbSet<Audit> y DbSet<AuditDetail>

- Audit.Svc: 
    - Implementación de IAuditService que utiliza el DbContext para guardar auditorías en la base de datos 
- Audit.Api:
    - Controlador AuditController con endpoints para crear auditorías y obtener auditorías por UserId
    - Debe implementar Odata
    - Debe implementar Json API
    - Debe implementar GraphQL
    - Debe generar un swagger para documentar la API

- Audit.Web:
    - Interfaz de usuario para mostrar auditorías por UserId
    - Debe usar el control Tabulator para mostrar las auditorías en una tabla con paginación y búsqueda
    - Debe consumir la API de Audit.Api para obtener las auditorías
    - Debe mostrar los detalles de cada auditoría al hacer clic en una fila de la tabla, al estilo de cambios en GIT. Debe permitir comparar los valores antiguos y nuevos de cada propiedad auditada.
    - Algunas propiedades de objeto pueden ser objetos complejos, por lo que se deben mostrar de forma legible en la interfaz de usuario, por ejemplo, Persona.Dirección.Calle, Persona.Dirección.Numero, etc..

- Audit.Tests:
    - Pruebas unitarias para la implementación de IAuditService
    - Pruebas de integración para el controlador AuditController

- Audit.Demo:
    - Un proyecto de consola o una aplicación simple para demostrar cómo usar el servicio de auditoría para crear auditorías y obtener auditorías por UserId. Este proyecto puede ser útil para validar la funcionalidad del servicio de auditoría antes de integrarlo con la API y la interfaz de usuario. Debe usar un BackgroundService que vaya generando auditorías de forma periódica para simular actividad en el sistema y permitir probar la visualización de auditorías en la interfaz de usuario.

- Audit.Entities.Dom: Entidades de ejemplo para simular la creación de auditorías, por ejemplo, una entidad Persona con propiedades como Nombre, Edad, Dirección, etc. Una entidad Producto con propiedades como Nombre, Precio, Stock, etc. Estas entidades se pueden usar en el proyecto de demostración para generar auditorías y validar la funcionalidad del sistema de auditoría.

- Audit.BDD.Tests:
    - Pruebas basadas en gherkin para validar el flujo completo de auditoría desde la creación hasta la visualización en la interfaz de usuario. Estas pruebas deben cubrir escenarios como:
        - Crear una auditoría para una entidad y verificar que se guarda correctamente en la base de datos.
        - Obtener auditorías por UserId y verificar que se devuelven los resultados correctos.
        - Verificar que los detalles de la auditoría se muestran correctamente en la interfaz de usuario, incluyendo la comparación de valores antiguos y nuevos.
    - Debe usar reqnroll para estas ptuebas basadas en gherkin.
    - Pruebas de integración para el controlador AuditController

