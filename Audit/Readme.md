# Sistema de Auditoría en .NET 8

Sistema de auditoría en 3 capas implementado con .NET 8, que registra cambios sobre entidades de dominio y los expone a través de múltiples protocolos API y una interfaz web.

---

## Arquitectura

| Proyecto | Rol | Tecnología |
|---|---|---|
| `Audit.Dom` | Dominio | Entidades `AuditEntry`, `AuditDetail`, enum `AuditAction`, interfaces `IAuditService`, `IAuditRepository` |
| `Audit.Entities.Dom` | Entidades de ejemplo | `Persona`, `Direccion`, `Producto` |
| `Audit.Data` | Acceso a datos | Dapper + SQLite, `AuditRepository` |
| `Audit.Svc` | Servicios | `AuditService` implementa `IAuditService` |
| `Audit.Api` | API | OData `/odata/Audits`, REST `/api/audits`, JSON:API `/jsonapi/audits`, GraphQL `/graphql`, Swagger `/swagger` |
| `Audit.Web` | UI | Razor Pages + Tabulator.js con diff estilo git |
| `Audit.Demo` | Demo | `AuditGeneratorService` (BackgroundService) genera auditorías cada 5 segundos |
| `Audit.Tests` | Tests | 6 unit tests (Moq) + 6 integration tests (WebApplicationFactory) |
| `Audit.BDD.Tests` | BDD | 7 escenarios Gherkin con SpecFlow + LivingDocPlugin, SQLite temporal por escenario |

---

## Puntos clave

- **OData** soporta `$filter`, `$orderby`, `$top`, `$skip`, `$select`, `$count` en `/odata/Audits`
- **GraphQL** (HotChocolate): queries `audits`, `auditById`, `auditsByUser`, `auditCount` + mutación `createAudit`, accesible en `/graphql`
- **JSON:API** retorna el formato `{"data": [...], "meta": {...}, "links": {...}}` con paginación `page[number]` / `page[size]` en `/jsonapi/audits`
- **Tabulator** con paginación remota, filtro por UserId y modal diff estilo git (rojo/verde por propiedad), con soporte de valores JSON anidados como `Persona.Direccion.Calle`
- **19 tests pasan** (7 BDD + 12 unitarios/integración)

---

## Requisitos previos

| Herramienta | Versión mínima |
|---|---|
| [.NET SDK](https://dotnet.microsoft.com/download) | 8.0 |
| SQLite | Embebido vía `Microsoft.Data.Sqlite`, no requiere instalación |

---

## Paquetes y tecnologías

### Audit.Data
| Paquete | Uso |
|---|---|
| `Dapper` | Micro-ORM para mapeo SQL → objetos |
| `Microsoft.Data.Sqlite` | Proveedor SQLite para .NET |
| `Microsoft.Extensions.DependencyInjection` | Registro de servicios en el contenedor DI |

### Audit.Svc
| Paquete | Uso |
|---|---|
| `Microsoft.Extensions.DependencyInjection` | Registro de servicios en el contenedor DI |

### Audit.Api
| Paquete | Uso |
|---|---|
| `Microsoft.AspNetCore.OData` | Soporte OData v4 ($filter, $select, $expand…) |
| `HotChocolate.AspNetCore` | Servidor GraphQL |
| `HotChocolate.Data` | Proyecciones, filtros y ordenación en GraphQL |
| `Swashbuckle.AspNetCore` | Generación de Swagger / OpenAPI |

### Audit.Tests
| Paquete | Uso |
|---|---|
| `xunit` | Framework de tests |
| `Moq` | Mocking de dependencias |
| `FluentAssertions` | Aserciones legibles |
| `Microsoft.AspNetCore.Mvc.Testing` | Tests de integración sobre WebApplication |

### Audit.BDD.Tests
| Paquete | Uso |
|---|---|
| `SpecFlow.xUnit` | Motor BDD (Gherkin) con runner xUnit |
| `SpecFlow.Plus.LivingDocPlugin` | Genera `TestExecution.json` durante los tests |
| `SpecFlow.Plus.LivingDoc.Cli` | CLI local (dotnet tool) que convierte el JSON a HTML |
| `FluentAssertions` | Aserciones legibles |
| `Microsoft.Data.Sqlite` | Base de datos temporal por escenario |

### Audit.Web (CDN, sin paquetes NuGet)
| Librería | Uso |
|---|---|
| [Tabulator 6.3](https://tabulator.info) | Tabla interactiva con paginación remota y filtros |
| [Bootstrap 5.3](https://getbootstrap.com) | Estilos y componentes (modal, badges, navbar) |
| [Bootstrap Icons 1.11](https://icons.getbootstrap.com) | Iconografía |

---

## Compilar

```bash
# Desde la raíz de la solución
dotnet restore
dotnet build
```

---

## Ejecutar

Los proyectos `Audit.Api`, `Audit.Web` y `Audit.Demo` se ejecutan de forma independiente. Lo más habitual es arrancar primero la API y luego la Web.

### 1. Audit.Api (puerto 5000 por defecto)

```bash
cd Audit.Api
dotnet run
```

Endpoints disponibles:

| Protocolo | URL |
|---|---|
| Swagger UI | `http://localhost:5000/swagger` |
| REST | `http://localhost:5000/api/audits` |
| OData | `http://localhost:5000/odata/Audits?$filter=Action eq 'Create'&$orderby=Timestamp desc` |
| JSON:API | `http://localhost:5000/jsonapi/audits?page[number]=1&page[size]=20` |
| GraphQL Playground | `http://localhost:5000/graphql` |

La base de datos SQLite se crea automáticamente en `Audit.Api/audit.db` al primer arranque.

### 2. Audit.Web (puerto 5001 por defecto)

```bash
cd Audit.Web
dotnet run
```

Abrir `http://localhost:5001` en el navegador. Asegúrate de que `Audit.Api` está corriendo, o ajusta la URL de la API en `appsettings.json`:

```json
{
  "AuditApi": {
    "BaseUrl": "http://localhost:5000/"
  }
}
```

### 3. Audit.Demo (generador de auditorías)

```bash
cd Audit.Demo
dotnet run
```

Genera auditorías aleatorias de entidades `Persona` y `Producto` cada 5 segundos en `audit-demo.db`. Útil para poblar datos mientras se visualiza la UI.

> **Nota:** Si quieres que el Demo alimente la misma BD que la API, configura ambos con el mismo `ConnectionStrings:AuditDb` apuntando al mismo fichero `.db`.

---

## Ejecutar los tests

```bash
# Todos los tests
dotnet test

# Solo unitarios e integración
dotnet test Audit.Tests

# Solo BDD (Gherkin/SpecFlow)
dotnet test Audit.BDD.Tests

# Con salida detallada
dotnet test --logger "console;verbosity=detailed"
```

### Reporte LivingDoc (SpecFlow.Plus.LivingDocPlugin)

El reporte HTML se genera automáticamente al finalizar `dotnet test Audit.BDD.Tests`.

#### Primera ejecución — restaurar la herramienta CLI

```bash
cd Audit.BDD.Tests
dotnet tool restore    # instala SpecFlow.Plus.LivingDoc.Cli desde .config/dotnet-tools.json
```

#### Flujo automático

Al ejecutar `dotnet test Audit.BDD.Tests`, el proceso es:

1. **`SpecFlow.Plus.LivingDocPlugin`** recopila los resultados de ejecución y genera `TestExecution.json` junto al ensamblado.
2. El target MSBuild `GenerateLivingDoc` (`AfterTargets="VSTest"`) invoca automáticamente el CLI de LivingDoc.
3. El reporte HTML se escribe en:

```
Audit.BDD.Tests/bin/Debug/net8.0/TestResults/LivingDoc.html
```

#### Script de conveniencia

Para ejecutar tests + reporte en un solo paso desde la raíz:

```powershell
./run-bdd-report.ps1
```

El reporte es un fichero HTML interactivo con navegación de features, escenarios y pasos, que se abre directamente en cualquier navegador.

---

## Estructura de la base de datos (SQLite)

```sql
CREATE TABLE AuditEntries (
    Id          TEXT NOT NULL PRIMARY KEY,  -- GUID como texto
    UserId      TEXT NOT NULL,
    EntityId    TEXT NOT NULL,              -- GUID como texto
    EntityName  TEXT NOT NULL,
    Action      INTEGER NOT NULL,           -- 0=Create 1=Update 2=Delete 3=Read
    Timestamp   TEXT NOT NULL              -- ISO 8601
);

CREATE TABLE AuditDetails (
    Id           TEXT NOT NULL PRIMARY KEY,
    AuditId      TEXT NOT NULL REFERENCES AuditEntries(Id),
    PropertyName TEXT NOT NULL,
    OldValue     TEXT,
    NewValue     TEXT
);
```

---

## Ejemplos de uso

### Crear una auditoría (REST)

```bash
curl -X POST http://localhost:5000/api/audits \
  -H "Content-Type: application/json" \
  -d '{
    "userId": "user-001",
    "entityId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "entityName": "Persona",
    "action": 1,
    "details": [
      { "propertyName": "Nombre", "oldValue": "Juan", "newValue": "Pedro" },
      { "propertyName": "Direccion.Ciudad", "oldValue": "Madrid", "newValue": "Barcelona" }
    ]
  }'
```

### Consulta OData con filtros

```
GET /odata/Audits?$filter=UserId eq 'user-001' and Action eq 1&$orderby=Timestamp desc&$top=10
```

### Consulta GraphQL

```graphql
query {
  auditsByUser(userId: "user-001", skip: 0, take: 10) {
    id
    entityName
    action
    timestamp
    details {
      propertyName
      oldValue
      newValue
    }
  }
}
```

### Consulta JSON:API

```
GET /jsonapi/audits?filter[userId]=user-001&page[number]=1&page[size]=20
```
