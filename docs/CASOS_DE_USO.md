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
3. Las reservas y los movimientos de inventario se registran de forma atomica con la operacion que los origina; al cerrar un servicio, el consumo, la liberacion de reservas, el cambio de estado y la venta asociada se confirman juntos.
4. Los movimientos de inventario deben indicar su origen: compra, venta, servicio, devolucion, ajuste, anulacion o carga inicial.
5. Los detalles historicos conservan la cantidad, el precio y el costo aplicados en el momento de la operacion.
6. Toda operacion sensible registra usuario, fecha, hora y motivo cuando corresponda.
7. El cliente y el vehiculo atendidos quedan guardados en el servicio, aunque el titular del vehiculo cambie posteriormente.
8. Una venta de mostrador puede realizarse a consumidor final. Un servicio vehicular requiere un vehiculo identificado.
9. El stock fisico no puede quedar negativo. El stock disponible para nuevas operaciones es el stock fisico menos las cantidades reservadas por servicios en curso.
10. Las compras de mercaderia actualizan inventario al recibir la mercaderia, no solamente al registrar la deuda con el proveedor.
11. Todo cobro confirmado de ventas o servicios genera movimientos de caja vinculados a sus pagos. Un medio no efectivo se registra como ingreso, pero no aumenta el efectivo fisico del cajon.
12. El saldo de apertura de la primera jornada se establece mediante un conteo inicial autorizado. Las jornadas posteriores toman como base el efectivo contado y verificado al cierre anterior; las diferencias se conservan y justifican, nunca se absorben silenciosamente.
13. Los gastos, retiros, depositos y otros movimientos que afecten el efectivo deben registrarse. Las compras y obligaciones pendientes no afectan la caja hasta que se paga efectivamente.
14. Los identificadores de medios de pago son internos de la API. La interfaz muestra nombres seleccionables y envia el identificador asociado sin pedir al usuario que lo escriba.
15. El cierre compara los movimientos registrados con el conteo fisico. Si hay una diferencia, se registra con una observacion obligatoria y sin alterar los movimientos originales.

## 4. Estados principales

### 4.1 Presupuesto

`Borrador`, `Enviado`, `Aceptado`, `Rechazado`, `Vencido` y `Anulado`.

### 4.2 Servicio vehicular

`En curso`, `Finalizado` y `Anulado`. Un servicio anulado conserva su historial y motivo; si hubo productos consumidos antes de la interrupcion, estos quedan registrados y se incluyen en la operacion de cobro correspondiente.

### 4.3 Venta

`Borrador`, `Confirmada`, `Pagada`, `Pendiente`, `Anulada` y `Devuelta parcialmente`.

### 4.4 Compra

`Borrador`, `Confirmada`, `Recibida parcialmente`, `Recibida`, `Pendiente de pago`, `Pagada` y `Anulada`.

### 4.5 Jornada de caja

`Abierta`, `Cerrada`, `Verificada` y `Reabierta`.

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

**Precondiciones:** El usuario esta autenticado; el cliente y el vehiculo estan activos y registrados.

**Flujo principal:**

1. El usuario busca el vehiculo por patente y registra el kilometraje de ingreso, el trabajo solicitado, la inspeccion previa, los danos existentes y las observaciones.
2. El usuario agrega los productos y cantidades previstos para el trabajo.
3. El sistema valida que los productos existan, esten activos y tengan cantidad disponible suficiente.
4. El sistema crea el servicio en estado `En curso` y persiste sus datos.
5. El sistema reserva las cantidades previstas para ese servicio, sin descontarlas todavia del stock fisico.
6. El sistema informa que el servicio y sus reservas quedaron guardados.

El servicio y sus reservas sobreviven al cierre del navegador o al reinicio del equipo. El stock reservado deja de estar disponible para otras ventas o servicios.

**Alternativa — stock insuficiente:** El sistema informa que producto y cantidad no pueden reservarse; no inicia el servicio con reservas parciales inadvertidas. El usuario puede corregir las cantidades o resolver la diferencia mediante el proceso autorizado de inventario.

### CU-15 Modificar servicio vehicular

**Actor:** Usuario autorizado.

**Precondiciones:** El servicio existe y esta `En curso`.

**Flujo principal:**

1. El usuario busca el servicio por patente, cliente, fecha o numero y lo abre.
2. El sistema muestra los datos del servicio y sus productos previstos y reservados.
3. El usuario actualiza los campos permitidos: productos, cantidades, trabajos adicionales y observaciones.
4. Al agregar productos o aumentar cantidades, el sistema comprueba y reserva la diferencia antes de guardar.
5. Al reducir o quitar productos, el sistema libera la parte de la reserva que ya no se necesita.
6. El usuario guarda los cambios.
7. El sistema conserva el estado `En curso` y registra fecha, hora y usuario de la modificacion.

La modificacion no descuenta stock fisico ni genera la venta definitiva. Los productos previstos pueden reemplazarse; el checklist de cierre determinara cuales se usaron realmente.

**Alternativas:**

- Si el servicio esta `Finalizado` o `Anulado`, el sistema permite consultarlo, pero no modificarlo libremente.
- Si no hay stock disponible para una nueva reserva o aumento, el sistema informa el producto y la cantidad faltante, conserva los datos anteriores y no aplica parcialmente el cambio.
- Si otro usuario modifico el servicio desde otra sesion, el sistema informa que hay una version mas reciente y solicita recargarla antes de guardar.
- Si el usuario cancela la edicion, se conservan los datos y reservas anteriores.

### CU-16 Finalizar servicio vehicular

**Actor:** Usuario autorizado.

**Precondiciones:** El servicio esta `En curso`, el trabajo fisico termino y el kilometraje final es valido.

**Flujo principal:**

1. El usuario abre el servicio `En curso` y selecciona `Finalizar servicio`.
2. El sistema muestra en una sola pantalla el cliente, vehiculo, kilometrajes, inspeccion, observaciones, trabajos y productos reservados.
3. El usuario confirma para cada producto si se utilizo y la cantidad realmente consumida; puede corregir la cantidad prevista.
4. El usuario confirma la mano de obra y los datos finales, incluido el proximo cambio cuando corresponda.
5. El sistema valida cantidades, reservas, productos activos y kilometrajes.
6. El usuario confirma el cierre.
7. El sistema registra salidas de inventario por las cantidades consumidas y libera las cantidades reservadas no utilizadas.
8. El sistema actualiza el kilometraje y el proximo cambio del vehiculo, cambia el servicio a `Finalizado` y registra usuario y fecha/hora.
9. El sistema genera una venta asociada con los productos y trabajos realizados, en estado `Pendiente de cobro`, y la deja disponible en Ventas y Cobros.
10. El sistema informa que el servicio fue finalizado y permite continuar con el cobro.

El movimiento de inventario, la liberacion de reservas, la finalizacion y la creacion de la venta se confirman en una unica transaccion. Si falla algun paso, no se confirma ninguno y el servicio permanece `En curso` para volver a intentarlo. El stock reservado no se descuenta dos veces.

**Alternativas:**

- Si el usuario cancela antes de confirmar, el servicio permanece `En curso` y las reservas no cambian.
- Si faltan datos o hay cantidades invalidas, el sistema identifica los campos y no cierra el servicio.
- Si la cantidad consumida supera la reservada, el sistema impide el cierre hasta que se actualice la reserva o se registre y autorice la discrepancia de inventario correspondiente.
- Si otro usuario finalizo el servicio simultaneamente, el sistema no repite movimientos; informa el nuevo estado y muestra la operacion existente.
- Si ocurre un error al guardar, se revierten todos los cambios de esta operacion; el usuario puede reintentar sin duplicar el consumo ni la venta.

### CU-17 Interrumpir o anular servicio en curso

**Actor:** Usuario autorizado.

**Precondiciones:** El servicio esta `En curso` y el usuario informa el motivo de la interrupcion.

**Flujo principal:**

1. El usuario selecciona `Interrumpir servicio`.
2. El sistema muestra el mismo checklist de productos reservados que se utiliza al finalizar.
3. El usuario indica que productos y cantidades se consumieron y cuales no se utilizaron; registra el motivo y las observaciones.
4. El usuario confirma la interrupcion.
5. El sistema registra la salida de los productos consumidos y libera las reservas de los no utilizados.
6. El sistema cambia el estado a `Anulado`, conserva el motivo y registra usuario y fecha/hora.
7. Si hubo productos consumidos o importes que cobrar, el sistema genera la venta asociada pendiente de cobro; si no hubo consumo ni cargos, no crea una venta vacia.

Los pasos se confirman en una unica transaccion. Anular el servicio no devuelve al inventario productos que ya fueron consumidos. La anulacion de un servicio finalizado se trata mediante los procesos de anulacion o devolucion de la venta y los movimientos compensatorios correspondientes.

## 9. Ventas y cobros

### CU-18 Registrar venta

**Actor:** Usuario autorizado.

Para una venta directa, el usuario selecciona los productos y cantidades; el sistema valida disponibilidad, registra el cobro y descuenta el stock en el acto. Para un servicio, la venta asociada se genera al cerrar el servicio y queda pendiente de cobro.

El sistema conserva cantidades, precios, descuentos, cliente opcional y referencia externa cuando corresponda. Los productos consumidos en un servicio no se descuentan nuevamente al cobrar la venta asociada.

### CU-19 Registrar cobro

**Actor:** Usuario autorizado.

El usuario registra efectivo, transferencia, tarjeta o pago combinado. El sistema valida que la suma coincida con el importe cobrado y genera los movimientos de caja correspondientes.

Una venta de servicio puede quedar pendiente hasta que se registre el cobro. Una venta directa normal se registra y cobra en el mismo flujo; si el negocio permite fiado, se registra el saldo como pendiente sin contabilizar dinero en caja hasta recibir el pago.

### CU-20 Registrar devolucion o cambio

El usuario indica la venta original, los productos y el motivo. El sistema conserva la venta, genera el movimiento inverso de inventario y registra el ajuste economico o de caja correspondiente.

## 10. Compras y proveedores

El primer incremento implementado de Proveedores cubre alta, consulta, busqueda, modificacion, desactivacion y reactivacion. El CUIT se normaliza y se mantiene unico cuando se informa; coincidencias exactas por razon social, telefono o correo se presentan como posibles duplicados que el usuario puede confirmar. La baja conserva el historial y exige motivo. Compras, obligaciones y estado de cuenta todavia no se implementan en este incremento.

### CU-21 Registrar compra

El usuario selecciona proveedor, productos, cantidades, costos, fecha y condicion de pago. La compra queda pendiente de recepcion o confirmacion.

### CU-22 Recibir mercaderia

El usuario verifica cantidades recibidas. El sistema actualiza inventario solo por lo efectivamente recibido y genera movimientos de entrada vinculados a la compra.

### CU-23 Registrar pago a proveedor

El sistema registra pago total o parcial, actualiza el saldo pendiente y genera el egreso de caja. Registrar una compra no implica automaticamente que haya sido pagada.

## 11. Caja y gastos

### CU-24 Abrir caja

**Actor:** Responsable de caja. La apertura inicial requiere autorizacion del administrador o responsable designado.

**Precondiciones:** El usuario esta autenticado y no existe otra jornada abierta.

**Primera apertura del sistema:**

1. El responsable cuenta el efectivo que constituye el fondo inicial.
2. El responsable informa el importe contado y la fecha de apertura.
3. El sistema registra el saldo inicial, el usuario y la fecha/hora como una apertura inicial auditable.

**Aperturas siguientes:**

1. El sistema propone como saldo inicial el efectivo contado y verificado en la jornada anterior.
2. El responsable confirma la apertura y el sistema registra usuario y fecha/hora.
3. Si se retiro o deposito dinero entre jornadas, se registra como movimiento antes de cerrar la jornada anterior o por el flujo autorizado correspondiente; el sistema no permite cambiar el saldo inicial para ocultarlo.

Solo puede haber una jornada operativa abierta. Si la jornada previa no fue verificada o mantiene una diferencia sin justificar, la apertura automatica se bloquea y el sistema solicita una conciliacion o autorizacion. La apertura manual tampoco permite reemplazar silenciosamente ese control.

El efectivo que queda fisicamente en el cajon es distinto del total de fondos disponibles en bancos u otros medios. Solo los movimientos configurados para afectar efectivo modifican el saldo fisico esperado.

### CU-25 Registrar ingreso o egreso

Los cobros de ventas directas, servicios y pagos pendientes se registran desde sus respectivos flujos y generan movimientos de caja vinculados a cada pago. El usuario no vuelve a cargar manualmente esos cobros en Caja.

El responsable puede registrar ingresos, gastos pagados, pagos a proveedores y retiros manuales con importe, medio de pago, motivo y referencia. Los movimientos manuales y ajustes requieren motivo. Las compras y gastos pendientes no afectan caja hasta que se registra el pago efectivo.

El sistema obtiene los medios de pago configurados. En la interfaz el usuario selecciona el nombre en una lista; la aplicacion envia internamente su identificador. Cada medio indica si afecta el efectivo fisico. Por ejemplo, efectivo aumenta el saldo del cajon; una transferencia no.

### CU-26 Cerrar caja

**Flujo principal:**

1. Antes del cierre, el sistema muestra el resumen de movimientos, el efectivo esperado en el cajon y los totales por medio de pago.
2. El responsable revisa el resumen y selecciona cerrar la jornada.
3. El sistema impide nuevos movimientos ordinarios y conserva el saldo esperado y quien/cuando cerro.
4. El responsable cuenta el efectivo fisico y registra el importe contado.
5. El sistema compara el efectivo contado con el efectivo esperado y registra la verificacion, el usuario y la fecha/hora.
6. Si coinciden, la diferencia es cero y la jornada queda `Verificada`.
7. Si no coinciden, el responsable ingresa una observacion obligatoria; el sistema registra el faltante o sobrante sin modificar los cobros ni egresos originales.

El efectivo esperado se calcula como: efectivo inicial vigente + ingresos que afectan efectivo - egresos que afectan efectivo. Las transferencias, tarjetas y otros medios que no afectan efectivo se excluyen de ese calculo, aunque aparecen en los totales generales.

La jornada siguiente toma como saldo inicial el efectivo realmente contado y verificado, no el saldo esperado si ambos difieren. La diferencia previa permanece visible y auditable. Si se retira dinero del cajon antes de la siguiente apertura, debe registrarse como retiro para que el saldo arrastrado coincida con el efectivo real.

La reapertura requiere rol de administrador y motivo obligatorio. El cierre y la verificacion anteriores se conservan; la reapertura agrega un nuevo registro de apertura y no borra ni reescribe movimientos previos.

### Pruebas de aceptacion del modulo de Caja

Las pruebas se ejecutan con usuarios, medios de pago y operaciones ficticias en una base de desarrollo aislada. No se usan datos ni movimientos reales.

1. **Fondo inicial:** abrir por primera vez con un importe contado autorizado; verificar saldo, usuario, fecha y estado `Abierta`.
2. **Arrastre de jornada:** cerrar y verificar una jornada; abrir la siguiente y comprobar que propone como saldo inicial el efectivo contado anterior, no un importe arbitrario.
3. **Pagos mixtos:** con un fondo inicial de $20.000, registrar una venta pagada con $30.000 en efectivo y $10.000 por transferencia; el efectivo esperado aumenta $30.000 y el total de ingresos aumenta $40.000.
4. **Egreso de efectivo:** registrar un gasto de $5.000 pagado en efectivo; el efectivo esperado del ejemplo queda en $45.000. Un gasto pagado por transferencia no reduce el efectivo del cajon.
5. **Cierre sin diferencia:** cerrar con $45.000 esperados, contar $45.000 y comprobar diferencia cero y estado `Verificada`.
6. **Faltante o sobrante:** contar un importe distinto; comprobar que se exige observacion, se conserva la diferencia y los movimientos originales no cambian. La jornada siguiente debe partir del conteo verificado, manteniendo visible la diferencia anterior.
7. **Cierre no conciliado:** comprobar que la apertura automatica no ignora una jornada previa sin verificar o con una diferencia pendiente de justificar.
8. **Controles de estado y permisos:** impedir una segunda apertura simultanea, movimientos en jornada cerrada, verificacion antes del cierre y reapertura sin rol administrador o sin motivo.
9. **Trazabilidad y atomicidad:** verificar que ventas y cobros generan un solo movimiento por pago, que errores no dejan movimientos parciales y que cada operacion conserva usuario y fecha/hora.

Se cubren con pruebas unitarias las reglas de saldos y estados; con pruebas de integracion se comprueba la persistencia y vinculacion de movimientos con pagos en SQL Server. Swagger se usa para pruebas exploratorias manuales, no como sustituto de las pruebas automatizadas.

### CU-27 Registrar gasto

**Actor:** Usuario autenticado con permisos de gestion de gastos.

**Precondiciones:** La categoria y, si corresponde, el proveedor deben existir; para registrar un pago debe haber una jornada de caja abierta y un medio de pago activo.

**Flujo principal:**

1. El usuario registra categoria, descripcion, fecha, importe total y, opcionalmente, vencimiento, proveedor, entidad relacionada, referencia y observaciones.
2. Si selecciona la categoria `Otro`, detalla la categoria.
3. Puede dejar el gasto pendiente o registrar en el mismo paso uno o varios pagos iniciales, incluso combinando medios de pago.
4. El sistema calcula el estado y saldo pendiente a partir de los pagos: `Pendiente`, `ParcialmentePagado` o `Pagado`. El estado no se ingresa manualmente.
5. Cada pago genera un movimiento de egreso en Caja, vinculado al gasto, al medio de pago y al usuario. Los pagos se guardan en una transaccion junto con los movimientos, de modo que no quede registrado uno sin el otro.
6. El gasto pendiente se conserva como obligacion consultable; no afecta Caja hasta que se registra un pago. El sistema muestra vencimientos vencidos o proximos como situaciones derivadas, sin cambiar el estado financiero.

Las compras de mercaderia se registran en Compras, no como gastos comunes, para evitar duplicar inventario o egresos.

No se permite que un egreso en efectivo supere el efectivo disponible, aunque el gasto se haya registrado previamente como obligacion.

### CU-28 Consultar y modificar gastos

El usuario puede buscar y filtrar gastos por periodo, vencimiento, categoria, estado, situacion, medio de pago, proveedor, entidad relacionada, importe, usuario o texto. La consulta presenta total, abonado, saldo pendiente, pagos, reintegros y sus movimientos vinculados.

Solo se pueden modificar gastos pendientes o parcialmente pagados. La modificacion no reescribe pagos previos y no puede reducir el total por debajo de lo ya pagado. La version de concurrencia impide guardar cambios basados en una copia desactualizada. Los gastos pagados o anulados no son editables.

### CU-29 Registrar pago de gasto

El usuario selecciona el gasto pendiente, indica fecha, importe y uno o varios medios de pago con sus referencias. El importe debe ser positivo y no superar el saldo pendiente. El sistema valida que los medios esten activos y que la caja este abierta; si el pago incluye efectivo, valida que alcance el saldo fisico. Pago, estado del gasto y movimientos de caja se guardan atomicamente.

### CU-30 Anular gasto

La anulacion requiere permiso de administrador y motivo. Un gasto sin pagos se anula conservando el registro y sin generar movimientos de caja. Si hubo pagos, el usuario registra reintegros que cubren exactamente lo abonado, con los medios realmente usados para devolver el dinero. Cada reintegro agrega un ingreso vinculado a Caja; solo cuando todos los reintegros y la anulacion pueden confirmarse juntos se marca el gasto como `Anulado`. Los pagos originales nunca se eliminan ni se modifican.

**Rutas de API implementadas:** `GET /api/gastos/categorias`, `GET /api/gastos`, `GET /api/gastos/{id}`, `POST /api/gastos`, `PUT /api/gastos/{id}`, `POST /api/gastos/{id}/pagos` y `POST /api/gastos/{id}/anular`.

### Pruebas de aceptacion del modulo de Gastos

1. Registrar una obligacion pendiente y comprobar que no genera movimiento de Caja.
2. Registrar un gasto con pagos iniciales combinados y comprobar que cada pago tiene un movimiento vinculado y que el saldo se calcula correctamente.
3. Pagar parcialmente y luego cancelar el saldo; verificar las transiciones a `ParcialmentePagado` y `Pagado`.
4. Intentar sobrepagar, elegir un medio inactivo, pagar sin caja abierta o exceder el efectivo disponible; verificar que la operacion se rechaza sin dejar movimientos parciales.
5. Modificar una obligacion y comprobar que el total no puede quedar por debajo de lo abonado ni puede modificarse un gasto pagado.
6. Consultar filtros de vencido, proximo a vencer y parcialmente pagado y contrastar la situacion derivada con las fechas.
7. Anular un gasto pendiente y comprobar que queda auditable sin movimiento de caja; intentar anular uno pagado sin reintegro completo y verificar el rechazo.
8. Anular un gasto pagado con reintegros completos; comprobar que los pagos se conservan, se agregan ingresos de reintegro y el gasto queda anulado.
9. Intentar registrar una compra de mercaderia como gasto y verificar que el sistema la deriva al modulo de Compras.

## 12. Reportes y panel

### CU-31 Consultar panel principal

El panel resume ventas, servicios, cobros, stock bajo, medios de pago y obligaciones proximas. No crea datos nuevos: consulta la informacion de los modulos.

### CU-32 Consultar reportes

El usuario autorizado puede consultar ventas, servicios, productos vendidos, rotacion, ingresos, gastos, caja e inventario por periodo y filtros.

Los reportes de rentabilidad utilizan el costo historico aplicado en cada operacion, no solamente el costo actual del producto.

## 13. Usuarios y auditoria

### CU-33 Gestionar usuarios y permisos

El administrador registra, modifica, desactiva usuarios y asigna roles. Los permisos determinan si el usuario puede consultar, crear, modificar, anular, corregir caja o ver costos.

### CU-34 Consultar auditoria

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
