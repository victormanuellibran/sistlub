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
- Swagger UI para operar y probar los endpoints

## Comandos

```powershell
dotnet restore
dotnet build --no-restore
dotnet test --no-build

# Configuracion local: ejecutar una sola vez desde PowerShell
dotnet user-secrets set "Auth:SigningKey" ([Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(64))) --project src/SistLubri.Api
dotnet user-secrets set "Auth:BootstrapSecret" ([Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(32))) --project src/SistLubri.Api

dotnet run --project src/SistLubri.Api
```

En Development, la API aplica automaticamente las migraciones sobre la base `SistLubriDev`. Los secretos quedan fuera del repositorio. Para levantar Swagger sin aplicar migraciones (por ejemplo, si LocalDB no esta disponible), se puede definir `Database:ApplyMigrationsOnStart` en `false`; las rutas que consultan datos seguiran necesitando la base.
Tambien se pueden aplicar de forma manual:

```powershell
dotnet ef database update --project src/SistLubri.Infrastructure --startup-project src/SistLubri.Api
```

Swagger queda disponible en `/swagger`, OpenAPI en `/openapi/v1.json` y el estado de la API en `/api/health`. Swagger incluye el esquema Bearer: primero se inicia el administrador en `POST /api/auth/bootstrap` usando el secreto configurado; luego se inicia sesion en `POST /api/auth/login`, se copia el token y se usa el boton **Authorize**.

## Modulos disponibles

Los cortes verticales disponibles implementan Clientes, Vehiculos, Productos, Inventario, Servicios vehiculares, Ventas, Caja, Proveedores y Gastos:

- alta, busqueda, consulta y modificacion de clientes;
- desactivacion y reactivacion sin eliminar informacion historica;
- alta de vehiculos asociados a clientes activos;
- busqueda por patente, modificacion, proximo cambio por kilometraje y cambio de titular;
- validacion de telefonos, patentes y registros duplicados;
- alta, busqueda, consulta, modificacion y desactivacion de productos;
- precios, costos, presentaciones, unidades de medida y productos fraccionados;
- carga inicial, entradas, salidas y ajustes de inventario con historial auditable;
- stock minimo, alertas de bajo stock, cantidades reservadas/disponibles y proteccion contra existencias negativas;
- apertura de servicios persistidos por patente, con reserva de los productos previstos;
- modificacion del servicio en curso que ajusta reservas y conserva precios, costos y unidades historicas;
- finalizacion o interrupcion con checklist de consumo, liberacion de sobrantes y movimientos atomicos de inventario;
- venta pendiente asociada al cierre del servicio, lista para cobrar sin descontar dos veces el stock;
- ventas directas con descuento inmediato de stock y cobro en el momento, pagos divididos, descuentos y consulta historica;
- anulaciones con reverso de caja, devoluciones parciales acumulativas y reingreso de productos solo cuando corresponde;
- jornadas de caja manuales o automaticas, ingresos/egresos, retiros, arqueo, verificacion, reapertura auditada y configuracion de medios de pago.
- alta, consulta, modificacion y desactivacion/reactivacion de proveedores, con CUIT unico, aviso de posibles duplicados e historial de estado auditable.
- registro de gastos pendientes, parciales o pagados, vencimientos, pagos combinados vinculados a Caja, consulta con filtros, modificacion con control de concurrencia y anulacion con reintegros auditables.

El estado de cuenta del proveedor y el modulo de Compras siguen pendientes; los gastos pueden vincularse opcionalmente a un proveedor, pero no constituyen todavia un estado de cuenta integral ni registran compras de mercaderia.

## Caja: comportamiento actual y reglas acordadas

La API ya permite abrir jornadas, consultar sus movimientos, cerrarlas, verificarlas y reabrirlas con permisos. Las ventas y cobros generan movimientos vinculados a los pagos; cada medio de pago indica si afecta el efectivo fisico. La tarea automatica de caja se ejecuta cada minuto mientras el proceso de la API esta activo. En desarrollo local, por lo tanto, la apertura y el cierre automaticos no se ejecutan cuando la aplicacion o la PC estan apagadas.

**Comportamiento implementado:** la primera apertura exige informar el efectivo contado. Las siguientes jornadas toman el saldo contado de la jornada anterior verificada, aunque haya días sin actividad; si la última jornada no está verificada, se bloquea la apertura manual y la automática no abre. El saldo heredado no puede reemplazarse al iniciar una nueva jornada. El cierre devuelve el efectivo esperado y la verificación posterior registra el conteo; si existe una diferencia, exige una observación y conserva los movimientos originales. Ventas y servicios registran sus pagos desde el módulo comercial; el endpoint de movimientos manuales solo acepta ingresos, egresos, retiros y ajustes permitidos.

La apertura y el cierre automáticos se ejecutan cada minuto mientras el proceso de la API está activo. En desarrollo local dejan de ejecutarse al apagar la aplicación o la PC; para automatización continua, el despliegue deberá mantener la API disponible. Swagger sigue mostrando el identificador GUID del medio de pago porque llama directamente a la API. Un desplegable con nombres requiere una interfaz de usuario, que todavía no está desarrollada.

Las reglas y casos de aceptación del módulo están en [`docs/CASOS_DE_USO.md`](docs/CASOS_DE_USO.md), sección **Caja y gastos**.

Las solicitudes de ejemplo se encuentran en `src/SistLubri.Api/SistLubri.Api.http`.

## Planificacion

El plan de sprints, capacidad del equipo y flujo de trabajo se encuentra en [`docs/PLAN_SCRUM.md`](docs/PLAN_SCRUM.md).
