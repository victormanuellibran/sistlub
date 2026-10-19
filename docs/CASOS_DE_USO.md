# Casos de uso del sistema de gestion del lubricentro

## 1. Alcance

El sistema permite registrar la atencion de clientes y vehiculos, administrar productos e inventario, gestionar servicios vehiculares, ventas, cobros, compras, caja, gastos, usuarios y reportes.

Los casos de uso describen operaciones del negocio. Las acciones internas de una pantalla, como seleccionar un cliente, agregar un producto o calcular un total, forman parte del caso de uso principal y no se consideran casos independientes.

## 2. Actores

- **Usuario autorizado:** Ruben, Judith o un empleado habilitado.
- **Responsable de caja:** usuario autorizado para abrir, cerrar y corregir jornadas de caja.
- **Administrador:** usuario autorizado para gestionar usuarios, permisos y configuracion.
- **Proveedor:** entidad externa que entrega mercaderia o recibe pagos.
- **Cliente:** persona asociada a un vehiculo o comprador de productos y servicios.

## 3. Reglas generales del negocio

1. Los registros historicos no se eliminan fisicamente. Se anulan, desactivan o revierten conservando el motivo.
2. Una operacion confirmada no se modifica libremente. Las correcciones se realizan mediante una anulacion o un movimiento compensatorio.
3. Una venta, compra o servicio que afecte stock debe generar sus movimientos de inventario una sola vez y dentro de la misma transaccion.
4. Los movimientos de inventario deben indicar su origen: compra, venta, servicio, devolucion, ajuste, anulacion o carga inicial.
5. Los detalles historicos conservan la cantidad, el precio y el costo aplicados en el momento de la operacion.
6. Toda operacion sensible registra usuario, fecha, hora y motivo cuando corresponda.
7. El cliente y el vehiculo atendidos quedan guardados en el servicio, aunque el titular del vehiculo cambie posteriormente.
8. Una venta de mostrador puede realizarse a consumidor final. Un servicio vehicular requiere un vehiculo identificado.
9. El stock no puede quedar negativo. Si no hay existencias suficientes, el servicio o la venta no se finaliza.
10. Las compras de mercaderia actualizan inventario al recibir la mercaderia, no solamente al registrar la deuda con el proveedor.

## 4. Estados principales

### 4.1 Presupuesto

`Borrador`, `Enviado`, `Aceptado`, `Rechazado`, `Vencido` y `Anulado`.

### 4.2 Servicio vehicular

`Borrador`, `En curso`, `Finalizado` y `Anulado`.

### 4.3 Venta

`Borrador`, `Confirmada`, `Pagada`, `Pendiente`, `Anulada` y `Devuelta parcialmente`.

### 4.4 Compra

`Borrador`, `Confirmada`, `Recibida parcialmente`, `Recibida`, `Pendiente de pago`, `Pagada` y `Anulada`.

### 4.5 Jornada de caja

`Abierta`, `Cerrada` y `Reabierta`.

## 5. Acceso y seguridad

### CU-01 Iniciar sesion

**Actor:** Usuario autorizado.

**Precondiciones:** El usuario debe estar registrado, activo y contar con una contrasena valida.

**Flujo principal:**

1. El usuario ingresa su correo o nombre de usuario y contrasena.
2. El sistema valida las credenciales y que la cuenta este activa.
3. El sistema identifica el rol y los permisos.
4. El sistema registra fecha, hora y resultado del acceso.
5. El sistema dirige al panel principal.

**Alternativas:** Si las credenciales son incorrectas, el sistema informa un error generico, registra el intento y permite reintentar. Luego de varios intentos fallidos puede bloquear temporalmente la cuenta.

### CU-02 Cerrar sesion

**Actor:** Usuario autenticado.

El sistema invalida la sesion, registra fecha y hora y evita acceder nuevamente a funciones protegidas sin autenticarse.

## 6. Clientes y vehiculos

### CU-03 Registrar cliente

**Actor:** Usuario autorizado.

El usuario registra nombre, apellido, telefono y datos opcionales. El sistema valida la informacion, evita duplicados evidentes y crea el cliente activo.

No se elimina un cliente que posea historial. Se lo desactiva para impedir nuevas operaciones sin perder sus datos.

### CU-04 Consultar y actualizar cliente

El usuario busca por nombre, apellido, telefono o correo y puede consultar sus vehiculos e historial. Los cambios no alteran las operaciones historicas.

### CU-05 Registrar vehiculo

**Actor:** Usuario autorizado.

El usuario selecciona un cliente activo y registra patente, marca, modelo, anio, kilometraje y observaciones. La patente se normaliza y debe ser unica.

### CU-06 Consultar historial del vehiculo

El sistema permite buscar por patente y consultar servicios, kilometrajes, productos utilizados, observaciones y ultimo cambio de aceite.

El historial conserva el cliente atendido en cada servicio, aunque el titular actual sea diferente.

### CU-07 Actualizar kilometraje y proximo cambio

El kilometraje nuevo no puede ser menor al ultimo registrado, salvo una correccion autorizada con motivo. El kilometraje del proximo cambio debe ser mayor al kilometraje actual.

El sistema puede registrar una fecha estimada de proximo contacto, pero no debe considerarla exacta si el negocio no conoce el kilometraje futuro del cliente.

## 7. Productos e inventario

### CU-08 Registrar producto

**Actor:** Usuario autorizado.

El usuario registra codigo, nombre, marca, categoria, presentacion, unidad de medida, precio, costo, stock inicial y stock minimo.

El codigo debe ser unico. Si el producto es fraccionado, su unidad base debe ser el litro.

### CU-09 Actualizar producto

El usuario puede actualizar datos comerciales. No puede cambiar la unidad de medida ni la modalidad de fraccionado cuando el producto tenga stock o movimientos, salvo mediante una conversion documentada.

Los cambios de precio y costo no modifican los valores historicos de ventas, servicios o compras anteriores.

### CU-10 Registrar movimiento manual de inventario

**Actor:** Usuario autorizado.

Se utiliza para carga inicial, ajuste positivo, ajuste negativo o correccion autorizada. El sistema exige tipo, cantidad, motivo y usuario.

Las salidas provenientes de ventas y servicios no se registran manualmente: las genera automaticamente la operacion de origen.

### CU-11 Consultar stock y movimientos

El sistema muestra stock actual, stock minimo, estado, movimientos, origen, fecha, usuario, stock anterior y stock resultante.

El sistema alerta productos `BajoStock` y `SinStock`. Nunca permite existencias negativas.

### CU-12 Registrar aceite fraccionado

**Actor:** Usuario autorizado.

El sistema registra el tambor o lote, aceite, capacidad inicial, litros disponibles, fecha de apertura y movimientos de extraccion o ajuste.

La extraccion descuenta litros y queda vinculada al servicio o venta que la produjo. Si el negocio no necesita distinguir tambores individuales, puede utilizarse un stock consolidado por producto.

## 8. Presupuestos y servicios

### CU-13 Generar presupuesto

**Actor:** Usuario autorizado.

El usuario selecciona cliente y vehiculo, agrega servicios, productos, cantidades, mano de obra, descuentos y observaciones.

El sistema calcula subtotales y total, guarda los precios aplicados y crea el presupuesto en estado `Borrador` o `Enviado`.

Generar un presupuesto no descuenta stock ni genera movimientos de caja.

### CU-14 Registrar servicio vehicular

**Actor:** Usuario autorizado.

El usuario selecciona cliente y vehiculo, registra kilometraje de ingreso, inspeccion previa, danos existentes, trabajo solicitado, productos previstos y observaciones. El servicio queda `En curso`.

### CU-15 Finalizar servicio vehicular

**Actor:** Usuario autorizado.

**Precondiciones:** El servicio esta `En curso`, el kilometraje es valido y existen productos suficientes.

**Flujo principal:**

1. El usuario confirma los productos realmente utilizados y la mano de obra.
2. El sistema valida las cantidades y el stock disponible.
3. El sistema registra el detalle historico con precios y costos.
4. El sistema genera automaticamente los movimientos de salida de inventario vinculados al servicio.
5. El sistema actualiza el kilometraje y el proximo cambio del vehiculo.
6. El sistema cambia el servicio a `Finalizado`.
7. El sistema genera o vincula la venta correspondiente.

Todos los pasos se confirman en una unica transaccion. Si uno falla, no se guarda ninguno.

### CU-16 Anular servicio

Un servicio finalizado no se elimina. El usuario autorizado indica un motivo y el sistema conserva el servicio original, genera movimientos compensatorios cuando corresponda y actualiza la venta relacionada.

## 9. Ventas y cobros

### CU-17 Registrar venta

**Actor:** Usuario autorizado.

El usuario registra productos o vincula un servicio finalizado. El sistema guarda cantidades, precios, descuentos, cliente opcional y referencia externa.

La venta de productos descuenta inventario automaticamente. El sistema no debe descontar nuevamente productos ya descontados por el servicio vinculado.

### CU-18 Registrar cobro

**Actor:** Usuario autorizado.

El usuario registra efectivo, transferencia, tarjeta o pago combinado. El sistema valida que la suma coincida con el importe cobrado y genera los movimientos de caja correspondientes.

Una venta puede quedar pendiente si el negocio permite fiado. En ese caso no se registra como dinero cobrado hasta que exista un pago.

### CU-19 Registrar devolucion o cambio

El usuario indica la venta original, los productos y el motivo. El sistema conserva la venta, genera el movimiento inverso de inventario y registra el ajuste economico o de caja correspondiente.

## 10. Compras y proveedores

### CU-20 Registrar compra

El usuario selecciona proveedor, productos, cantidades, costos, fecha y condicion de pago. La compra queda pendiente de recepcion o confirmacion.

### CU-21 Recibir mercaderia

El usuario verifica cantidades recibidas. El sistema actualiza inventario solo por lo efectivamente recibido y genera movimientos de entrada vinculados a la compra.

### CU-22 Registrar pago a proveedor

El sistema registra pago total o parcial, actualiza el saldo pendiente y genera el egreso de caja. Registrar una compra no implica automaticamente que haya sido pagada.

## 11. Caja y gastos

### CU-23 Abrir caja

El responsable registra fecha, usuario y saldo inicial por medio de pago. Solo puede existir una jornada abierta para la misma caja.

### CU-24 Registrar ingreso o egreso

El sistema registra cobros, gastos, pagos a proveedores, retiros y otros movimientos con importe, medio de pago, motivo y referencia.

### CU-25 Cerrar caja

El sistema calcula el saldo esperado por medio de pago. El responsable informa el saldo contado y el sistema registra diferencias sin modificar los movimientos originales.

La reapertura requiere permiso y motivo obligatorio. El cierre anterior se conserva y se genera una nueva version corregida.

### CU-26 Registrar gasto

El usuario registra categoria, descripcion, fecha, importe, vencimiento, estado y medio de pago. Un gasto pagado genera un egreso; uno pendiente genera una obligacion sin afectar el saldo disponible hasta su pago.

Las compras de mercaderia se registran en Compras, no como gastos comunes, para evitar duplicar inventario o egresos.

## 12. Reportes y panel

### CU-27 Consultar panel principal

El panel resume ventas, servicios, cobros, stock bajo, medios de pago y obligaciones proximas. No crea datos nuevos: consulta la informacion de los modulos.

### CU-28 Consultar reportes

El usuario autorizado puede consultar ventas, servicios, productos vendidos, rotacion, ingresos, gastos, caja e inventario por periodo y filtros.

Los reportes de rentabilidad utilizan el costo historico aplicado en cada operacion, no solamente el costo actual del producto.

## 13. Usuarios y auditoria

### CU-29 Gestionar usuarios y permisos

El administrador registra, modifica, desactiva usuarios y asigna roles. Los permisos determinan si el usuario puede consultar, crear, modificar, anular, corregir caja o ver costos.

### CU-30 Consultar auditoria

El sistema conserva usuario, fecha, operacion, entidad, identificador, valores relevantes y motivo de cambios sensibles. La auditoria no se puede editar desde la aplicacion.

## 14. Orden recomendado de implementacion

1. Ajustes de reglas de kilometraje, unidades de producto y origen de movimientos.
2. Servicios vehiculares y su historial.
3. Ventas y cobros.
4. Caja y medios de pago.
5. Compras, proveedores y recepcion de mercaderia.
6. Gastos y obligaciones.
7. Usuarios, permisos y auditoria.
8. Panel y reportes.
