namespace SistLubri.Application.Proveedores;

public sealed record CrearProveedorCommand(
    string RazonSocial, string? Cuit, string? ContactoNombre, string Telefono, string? Email,
    string? Direccion, string? MarcasYProductos, string? CondicionPagoHabitual,
    int? PlazoPagoHabitualDias, string? PeriodoPagoHabitual, string? Observaciones,
    bool ConfirmarPosiblesDuplicados = false);

public sealed record ActualizarProveedorCommand(
    string RazonSocial, string? Cuit, string? ContactoNombre, string Telefono, string? Email,
    string? Direccion, string? MarcasYProductos, string? CondicionPagoHabitual,
    int? PlazoPagoHabitualDias, string? PeriodoPagoHabitual, string? Observaciones,
    bool ConfirmarPosiblesDuplicados = false);

public sealed record DesactivarProveedorCommand(string Motivo);

public sealed record ProveedorResumenDto(Guid Id, string RazonSocial, string? Cuit, string Telefono, bool Activo);

public sealed record HistorialEstadoProveedorDto(
    string Estado, string? Motivo, DateTimeOffset FechaUtc, Guid UsuarioId);

public sealed record ProveedorDetalleDto(
    Guid Id,
    string RazonSocial,
    string? Cuit,
    string? ContactoNombre,
    string Telefono,
    string? Email,
    string? Direccion,
    string? MarcasYProductos,
    string? CondicionPagoHabitual,
    int? PlazoPagoHabitualDias,
    string? PeriodoPagoHabitual,
    string? Observaciones,
    bool Activo,
    DateTimeOffset CreadoEnUtc,
    Guid CreadoPorUsuarioId,
    DateTimeOffset? ActualizadoEnUtc,
    Guid? ActualizadoPorUsuarioId,
    IReadOnlyList<HistorialEstadoProveedorDto> HistorialEstados);
