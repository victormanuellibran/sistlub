using System.ComponentModel.DataAnnotations;
using SistLubri.Domain.Gastos;

namespace SistLubri.Api.Contracts.Gastos;

public sealed record PagoGastoRequest(
    Guid MedioPagoId,
    [Range(0.01, 100000000)] decimal Importe,
    [StringLength(120)] string? Referencia,
    [StringLength(500)] string? Observacion);

public sealed record CrearGastoRequest(
    [EnumDataType(typeof(CategoriaGasto))] CategoriaGasto Categoria,
    [StringLength(120)] string? CategoriaDetalle,
    [Required, StringLength(500, MinimumLength = 3)] string Descripcion,
    DateOnly Fecha,
    [Range(0.01, 100000000)] decimal ImporteTotal,
    [StringLength(200)] string? EntidadRelacionada,
    Guid? ProveedorId,
    DateOnly? FechaVencimiento,
    bool VencimientoEstimado,
    [StringLength(120)] string? ReferenciaComprobante,
    [StringLength(1000)] string? Observaciones,
    DateOnly? FechaPagoInicial,
    IReadOnlyList<PagoGastoRequest>? PagosIniciales,
    bool EsCompraMercaderia = false);

public sealed record ActualizarGastoRequest(
    [EnumDataType(typeof(CategoriaGasto))] CategoriaGasto Categoria,
    [StringLength(120)] string? CategoriaDetalle,
    [Required, StringLength(500, MinimumLength = 3)] string Descripcion,
    DateOnly Fecha,
    [Range(0.01, 100000000)] decimal ImporteTotal,
    [StringLength(200)] string? EntidadRelacionada,
    Guid? ProveedorId,
    DateOnly? FechaVencimiento,
    bool VencimientoEstimado,
    [StringLength(120)] string? ReferenciaComprobante,
    [StringLength(1000)] string? Observaciones,
    [Required] string Version);

public sealed record RegistrarPagosGastoRequest(
    DateOnly Fecha,
    [Required, MinLength(1)] IReadOnlyList<PagoGastoRequest> Pagos);

public sealed record AnularGastoRequest(
    [Required, StringLength(500, MinimumLength = 3)] string Motivo,
    DateOnly? FechaReintegro,
    IReadOnlyList<PagoGastoRequest>? Reintegros);
