# SistLubri

Sistema de gestion para Lubricentro Alvear 2.

## Arquitectura

El backend usa una arquitectura modular por capas:

- `SistLubri.Domain`: entidades y reglas centrales del negocio. No depende de otras capas.
- `SistLubri.Application`: casos de uso, contratos y validaciones. Depende de Domain.
- `SistLubri.Infrastructure`: persistencia y servicios externos. Depende de Application y Domain.
- `SistLubri.Api`: entrada HTTP y composicion de dependencias. Depende de Application e Infrastructure.
- `SistLubri.Domain.Tests`: pruebas de reglas del dominio y limites de arquitectura.
- `SistLubri.Application.Tests`: pruebas de los casos de uso y sus conflictos de negocio.

## Requisitos

- .NET SDK 10
- SQL Server LocalDB para desarrollo

## Comandos

```powershell
dotnet restore
dotnet tool restore
dotnet build --no-restore
dotnet test --no-build
dotnet run --project src/SistLubri.Api
```

En Development, la API aplica automaticamente las migraciones sobre la base `SistLubriDev`.
Tambien se pueden aplicar de forma manual:

```powershell
dotnet ef database update --project src/SistLubri.Infrastructure --startup-project src/SistLubri.Api
```

Swagger queda disponible en `/swagger`, OpenAPI en `/openapi/v1.json` y el estado de la API en `/api/health`.

## Modulos disponibles

Los cortes verticales disponibles implementan Clientes, Vehiculos, Productos e Inventario:

- alta, busqueda, consulta y modificacion de clientes;
- desactivacion y reactivacion sin eliminar informacion historica;
- alta de vehiculos asociados a clientes activos;
- busqueda por patente, modificacion, proximo cambio por kilometraje y cambio de titular;
- validacion de telefonos, patentes y registros duplicados;
- alta, busqueda, consulta, modificacion y desactivacion de productos;
- precios, costos, presentaciones, unidades de medida y productos fraccionados;
- carga inicial, entradas, salidas y ajustes de inventario con historial auditable;
- stock minimo, alertas de bajo stock y proteccion contra existencias negativas.

Las solicitudes de ejemplo se encuentran en `src/SistLubri.Api/SistLubri.Api.http`.

## Planificacion

El plan de sprints, capacidad del equipo y flujo de trabajo se encuentra en [`docs/PLAN_SCRUM.md`](docs/PLAN_SCRUM.md).
