# Plan Scrum de SistLubri

## Equipo y capacidad

- Equipo: 2 desarrolladores (Victor y Josue).
- Dedicacion: entre 1 y 2 horas por persona, de lunes a viernes.
- Duracion recomendada del sprint: 2 semanas (10 dias laborables).
- Capacidad bruta por sprint: entre 20 y 40 horas-persona.
- Capacidad de referencia: 30 horas-persona por sprint, usando 1,5 horas diarias por persona.
- Reserva para reuniones, revision, integracion e imprevistos: 20 %.
- Capacidad planificable recomendada: 24 horas-persona por sprint.

No se debe comprometer el maximo teorico de 40 horas. La planificacion se realiza con 24 horas y se agregan tareas opcionales solo cuando el trabajo comprometido esta terminado.

## Forma de trabajo diaria

Cada bloque diario de 1 a 2 horas se organiza asi:

1. Cinco minutos para actualizar el tablero y comunicar bloqueos.
2. Entre 45 y 100 minutos de trabajo concentrado.
3. Diez minutos para ejecutar pruebas, registrar avances y subir cambios.

Cada desarrollador mantiene como maximo una tarea en progreso. Una tarea no deberia superar 6 horas; si supera ese valor, se divide antes de comenzar.

## Ceremonias

- Planificacion: 30 minutos al inicio de cada sprint.
- Seguimiento diario: actualizacion asincronica de 5 minutos por persona.
- Refinamiento: 20 minutos durante la segunda semana.
- Revision: 30 minutos al finalizar el sprint.
- Retrospectiva: 20 minutos despues de la revision.

Las ceremonias consumen aproximadamente 3 horas-persona por sprint. El resto de la reserva cubre revisiones de codigo, integracion y correcciones.

## Flujo de Git

- `main`: version estable e integrada. No se desarrolla directamente en esta rama.
- `victor`: rama personal de Victor.
- `josue`: rama personal de Josue.
- Cada historia se integra a `main` mediante un pull request revisado por el otro desarrollador.
- Antes de iniciar una historia, la rama personal se actualiza desde `main`.
- Los cambios se suben diariamente y los pull requests deben ser pequenos.
- Una rama personal no debe permanecer mas de 3 dias sin sincronizarse con `main`.

## Tablero

Estados recomendados:

1. Backlog
2. Listo para desarrollar
3. En progreso
4. En revision
5. En pruebas
6. Terminado

Cada tarjeta debe contener objetivo, criterios de aceptacion, responsable, estimacion en horas y dependencias.

## Definicion de terminado

Una historia se considera terminada cuando:

- Cumple sus criterios de aceptacion.
- Compila sin errores ni advertencias.
- Incluye pruebas proporcionales al riesgo.
- No rompe las pruebas existentes.
- Fue revisada por el otro desarrollador.
- Tiene documentados los cambios de API o base de datos.
- Fue integrada en `main` mediante pull request.

## Hoja de ruta del MVP

La estimacion usa una capacidad media de 24 horas-persona efectivas por sprint.

### Sprint 0 - Base tecnica (completado)

- Solucion .NET 10 y arquitectura por capas.
- Proyectos Domain, Application, Infrastructure y Api.
- Proyecto de pruebas y control inicial de dependencias.
- Endpoint de salud, repositorio y flujo de Git.

### Sprint 1 - Persistencia, clientes y vehiculos

Objetivo: registrar clientes y sus vehiculos mediante la API.

- Configurar Entity Framework Core y SQL Server: 4 h.
- Modelar Cliente y sus reglas: 3 h.
- Modelar Vehiculo y su relacion con Cliente: 3 h.
- Casos de uso de alta, consulta y modificacion: 5 h.
- Endpoints y validaciones: 4 h.
- Migracion inicial y datos de desarrollo: 2 h.
- Pruebas e integracion: 3 h.

### Sprint 2 - Productos e inventario

Objetivo: controlar productos, existencias y movimientos de stock.

- Modelo de Producto, categorias y unidades: 4 h.
- Movimientos de inventario: 5 h.
- Entradas, salidas y ajustes: 5 h.
- Stock minimo y alertas: 3 h.
- Endpoints, pruebas e integracion: 5 h.
- Tarea opcional: preparacion de aceites fraccionados: 2 h.

### Sprint 3 - Servicios vehiculares

Objetivo: registrar un servicio y descontar los productos utilizados.

- Modelo de ServicioVehicular y estados: 4 h.
- Detalle de productos y tipos de servicio: 4 h.
- Inicio, modificacion, finalizacion y anulacion: 6 h.
- Actualizacion transaccional del inventario: 4 h.
- Historial por patente y kilometraje: 3 h.
- Pruebas e integracion: 3 h.

### Sprint 4 - Ventas, cobros y caja

Objetivo: cobrar productos o servicios y reflejar correctamente el dinero.

- Venta y detalle de venta: 4 h.
- Medios y combinaciones de pago: 4 h.
- Jornada y movimientos de caja: 5 h.
- Cobro de servicios y venta de productos: 4 h.
- Cierre diario: 3 h.
- Pruebas e integracion: 4 h.

### Sprint 5 - Compras, proveedores y gastos

Objetivo: registrar egresos y reponer el inventario.

- Proveedores y compras: 5 h.
- Recepcion de productos y entrada de stock: 5 h.
- Gastos, pagos y saldos pendientes: 5 h.
- Movimientos de caja asociados: 3 h.
- Pruebas e integracion: 4 h.
- Tarea opcional: obligaciones de pago: 2 h.

### Sprint 6 - Interfaz operativa

Objetivo: disponer de una interfaz web utilizable para el flujo principal.

- Base Angular, navegacion y autenticacion visual: 5 h.
- Clientes y vehiculos: 4 h.
- Productos e inventario: 4 h.
- Servicios: 4 h.
- Ventas y caja: 4 h.
- Pruebas de flujo e integracion: 3 h.

### Sprint 7 - Seguridad, reportes y entrega del MVP

Objetivo: preparar una version demostrable y recuperable.

- Usuarios, contrasenas y autorizacion: 5 h.
- Panel y reportes basicos: 5 h.
- Auditoria de operaciones sensibles: 3 h.
- Manejo uniforme de errores y logs: 3 h.
- Copia de seguridad y configuracion de despliegue: 3 h.
- Pruebas integrales, correcciones y documentacion: 5 h.

## Duracion estimada

- Siete sprints pendientes de dos semanas: 14 semanas.
- Esfuerzo medio restante: aproximadamente 168 horas-persona efectivas.
- Rango razonable considerando cambios y aprendizaje: entre 14 y 18 semanas.

El alcance se revisa al finalizar cada sprint. Si aparecen cambios, primero se reordena el backlog; no se agrega trabajo al sprint activo salvo que sea una correccion bloqueante.

## Reparto del trabajo

El reparto se decide por historia y se alterna para evitar conocimiento aislado. Como punto de partida:

- Victor: primera historia de dominio y API del sprint.
- Josue: persistencia, segunda historia funcional o interfaz asociada.
- Ambos: diseno de reglas, revision de pull requests, migraciones y pruebas de integracion.

En el sprint siguiente se intercambian las areas principales. Ningun modulo queda bajo responsabilidad permanente de una sola persona.
