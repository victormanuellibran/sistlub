# SistLubri

Sistema de gestion para Lubricentro Alvear 2.

## Arquitectura

El backend usa una arquitectura modular por capas:

- `SistLubri.Domain`: entidades y reglas centrales del negocio. No depende de otras capas.
- `SistLubri.Application`: casos de uso, contratos y validaciones. Depende de Domain.
- `SistLubri.Infrastructure`: persistencia y servicios externos. Depende de Application y Domain.
- `SistLubri.Api`: entrada HTTP y composicion de dependencias. Depende de Application e Infrastructure.
- `SistLubri.Domain.Tests`: pruebas de reglas del dominio y limites de arquitectura.

## Requisitos

- .NET SDK 10
- SQL Server se incorporara al implementar persistencia

## Comandos

```powershell
dotnet restore
dotnet build --no-restore
dotnet test --no-build
dotnet run --project src/SistLubri.Api
```

Durante el desarrollo, OpenAPI queda disponible en `/openapi/v1.json` y el estado de la API en `/api/health`.
