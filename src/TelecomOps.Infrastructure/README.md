# TelecomOps.Infrastructure — EF Core and Data Access

## Propósito

Esta capa implementa los contratos del dominio (`Core`) con EF Core y maneja el acceso a datos en PostgreSQL.

## Cómo funciona EF Core en este proyecto

1. **DbContext centralizado**
   - `AppDbContext` hereda de `DbContext`.
   - Define `DbSet<NodeConfig> NodeConfigs` para mapear la entidad `NodeConfig` a la tabla `nodeconfigs`.
   - `OnModelCreating` configura la correspondencia de columnas:
     - `Id` → `id`
     - `Name` → `name`
     - `FrequencyBand` → `frequency_band`
     - `Status` → `status`
     - `CreatedAt` → `created_at`
     - `UpdatedAt` → `updated_at`

2. **Configuración de conexión**
   - En `src/TelecomOps.Api/Program.cs` y `src/TelecomOps.Worker/Program.cs` se registra `AppDbContext` en DI:
     ```csharp
     builder.Services.AddDbContext<AppDbContext>(options =>
         options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));
     ```
   - La cadena de conexión viene de `appsettings.json` o variables de entorno.

3. **Inicialización de la base de datos**
   - Al iniciar API y Worker, el código crea un scope de servicios y obtiene `AppDbContext`.
   - Si hay migraciones pendientes se aplica `MigrateAsync()`.
   - En desarrollo también usa `EnsureCreatedAsync()` como respaldo.

4. **Repositorio de datos**
   - `NodeConfigRepository` implementa `INodeConfigRepository` usando EF Core.
   - Las operaciones CRUD se hacen con:
     - `FindAsync(id)` para buscar por ID
     - `ToListAsync()` para listar entidades
     - `AddAsync()` para insertar
     - `Update()` para modificar
     - `SaveChangesAsync()` para persistir cambios
   - Todo el acceso es asíncrono para no bloquear hilos.

5. **Separación de responsabilidades**
   - El dominio (`Core`) define entidades e interfaces.
   - La infraestructura implementa esas interfaces con EF Core.
   - Esto mantiene `Core` independiente de la tecnología de persistencia.

## Componentes clave

- `AppDbContext`
  - Mapea `NodeConfig` al esquema de PostgreSQL.
  - Gestiona el ciclo de vida de las entidades.

- `NodeConfigRepository`
  - Traduce las operaciones del dominio a llamadas EF Core.
  - Proporciona métodos `GetAllAsync`, `GetByIdAsync`, `AddAsync`, `UpdateAsync`, `DeleteAsync`.

## Flujo típico de una petición

1. El controlador o el worker recibe la petición.
2. Obtiene `INodeConfigRepository` desde DI.
3. Llama al método adecuado del repositorio.
4. El repositorio usa `AppDbContext` para consultar o modificar datos.
5. EF Core traduce esas operaciones a SQL y las ejecuta en PostgreSQL.

## Notas de implementación

- No se usa `.Result` ni `.Wait()`.
- Se usa `SaveChangesAsync()` para persistir cambios.
- La capa `Core` no referencia `Microsoft.EntityFrameworkCore`.
- El proyecto puede evolucionar a migraciones formales si se requiere `dotnet ef migrations add`.

## Conexión de ejemplo

```text
ConnectionStrings__Default=Host=postgres;Port=5432;Database=telecomops;Username=telecom;Password=telecom123
```

## Beneficios

- Permite cambiar la base de datos sin alterar la lógica del dominio.
- Hace fácil escribir pruebas unitarias y mocks para `INodeConfigRepository`.
- Mantiene el acceso a datos centralizado en una sola capa.
