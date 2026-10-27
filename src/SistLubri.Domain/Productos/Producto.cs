using SistLubri.Domain.Common;

namespace SistLubri.Domain.Productos;

public sealed class Producto : Entity
{
    private readonly List<MovimientoInventario> _movimientos = [];

    private Producto()
    {
    }

    public Producto(
        string codigo,
        string nombre,
        string? marca,
        string? categoria,
        string presentacion,
        UnidadMedidaProducto unidadMedida,
        decimal precioVenta,
        decimal costoCompra,
        bool esFraccionado,
        decimal stockInicial = 0,
        decimal stockMinimo = 0)
    {
        Codigo = NormalizarCodigo(codigo);
        ActualizarDatos(
            nombre,
            marca,
            categoria,
            presentacion,
            unidadMedida,
            precioVenta,
            costoCompra,
            esFraccionado);
        ConfigurarStockMinimo(stockMinimo);
        Activo = true;
        CreadoEnUtc = DateTimeOffset.UtcNow;

        if (stockInicial < 0)
        {
            throw new DomainException("El stock inicial no puede ser negativo.");
        }

        if (stockInicial > 0)
        {
            RegistrarMovimiento(
                TipoMovimientoInventario.CargaInicial,
                stockInicial,
                "Carga inicial de inventario");
        }
    }

    public string Codigo { get; private set; } = string.Empty;

    public string Nombre { get; private set; } = string.Empty;

    public string? Marca { get; private set; }

    public string? Categoria { get; private set; }

    public string Presentacion { get; private set; } = string.Empty;

    public UnidadMedidaProducto UnidadMedida { get; private set; }

    public decimal PrecioVenta { get; private set; }

    public decimal CostoCompra { get; private set; }

    public decimal StockActual { get; private set; }

    public decimal StockMinimo { get; private set; }

    public bool EsFraccionado { get; private set; }

    public bool Activo { get; private set; }

    public DateTimeOffset CreadoEnUtc { get; private init; }

    public DateTimeOffset? ActualizadoEnUtc { get; private set; }

    public IReadOnlyCollection<MovimientoInventario> Movimientos => _movimientos.AsReadOnly();

    public EstadoInventario EstadoInventario => StockActual <= 0
        ? EstadoInventario.SinStock
        : StockMinimo > 0 && StockActual <= StockMinimo
            ? EstadoInventario.BajoStock
            : EstadoInventario.Disponible;

    public void ActualizarCodigo(string codigo)
    {
        Codigo = NormalizarCodigo(codigo);
        ActualizadoEnUtc = DateTimeOffset.UtcNow;
    }

    public void ActualizarDatos(
        string nombre,
        string? marca,
        string? categoria,
        string presentacion,
        UnidadMedidaProducto unidadMedida,
        decimal precioVenta,
        decimal costoCompra,
        bool esFraccionado)
    {
        if (!Enum.IsDefined(unidadMedida))
        {
            throw new DomainException("La unidad de medida no es valida.");
        }

        if ((UnidadMedida != unidadMedida || EsFraccionado != esFraccionado)
            && _movimientos.Count > 0)
        {
            throw new DomainException(
                "No se puede cambiar la unidad o el fraccionado de un producto con movimientos de inventario.");
        }

        if (precioVenta < 0)
        {
            throw new DomainException("El precio de venta no puede ser negativo.");
        }

        if (costoCompra < 0)
        {
            throw new DomainException("El costo de compra no puede ser negativo.");
        }

        if (esFraccionado && unidadMedida != UnidadMedidaProducto.Litro)
        {
            throw new DomainException("Un producto fraccionado debe controlarse por litros.");
        }

        Nombre = NormalizarRequerido(nombre, 120, "El nombre");
        Marca = NormalizarOpcional(marca, 80, "La marca");
        Categoria = NormalizarOpcional(categoria, 80, "La categoria");
        Presentacion = NormalizarRequerido(presentacion, 80, "La presentacion");
        UnidadMedida = unidadMedida;
        PrecioVenta = decimal.Round(precioVenta, 2);
        CostoCompra = decimal.Round(costoCompra, 2);
        EsFraccionado = esFraccionado;
        ActualizadoEnUtc = DateTimeOffset.UtcNow;
    }

    public void ConfigurarStockMinimo(decimal stockMinimo)
    {
        if (stockMinimo < 0)
        {
            throw new DomainException("El stock minimo no puede ser negativo.");
        }

        StockMinimo = decimal.Round(stockMinimo, 3);
        ActualizadoEnUtc = DateTimeOffset.UtcNow;
    }

    public MovimientoInventario RegistrarMovimiento(
        TipoMovimientoInventario tipo,
        decimal cantidad,
        string motivo,
        Guid? referenciaId = null)
    {
        if (!Activo)
        {
            throw new DomainException("No se puede modificar el stock de un producto inactivo.");
        }

        if (!Enum.IsDefined(tipo))
        {
            throw new DomainException("El tipo de movimiento no es valido.");
        }

        var motivoNormalizado = NormalizarRequerido(motivo, 500, "El motivo");
        var cantidadNormalizada = decimal.Round(cantidad, 3);
        if (cantidadNormalizada <= 0)
        {
            throw new DomainException("La cantidad del movimiento debe ser al menos 0,001.");
        }

        var esSalida = tipo is TipoMovimientoInventario.Salida or TipoMovimientoInventario.AjusteNegativo;
        var stockResultante = StockActual + (esSalida ? -cantidadNormalizada : cantidadNormalizada);

        if (stockResultante < 0)
        {
            throw new DomainException("El movimiento produciria un stock negativo.");
        }

        var movimiento = new MovimientoInventario(
            Id,
            tipo,
            cantidadNormalizada,
            StockActual,
            stockResultante,
            motivoNormalizado,
            referenciaId);

        _movimientos.Add(movimiento);
        StockActual = stockResultante;
        ActualizadoEnUtc = DateTimeOffset.UtcNow;
        return movimiento;
    }

    public void CambiarEstado(bool activo)
    {
        Activo = activo;
        ActualizadoEnUtc = DateTimeOffset.UtcNow;
    }

    public static string NormalizarCodigo(string? codigo)
    {
        var normalizado = NormalizarRequerido(codigo, 40, "El codigo");
        return normalizado.Replace(" ", string.Empty, StringComparison.Ordinal).ToUpperInvariant();
    }

    private static string NormalizarRequerido(string? valor, int longitudMaxima, string campo)
    {
        var normalizado = valor?.Trim();
        if (string.IsNullOrWhiteSpace(normalizado))
        {
            throw new DomainException($"{campo} es obligatorio.");
        }

        if (normalizado.Length > longitudMaxima)
        {
            throw new DomainException($"{campo} no puede superar los {longitudMaxima} caracteres.");
        }

        return normalizado;
    }

    private static string? NormalizarOpcional(string? valor, int longitudMaxima, string campo)
    {
        var normalizado = valor?.Trim();
        if (string.IsNullOrWhiteSpace(normalizado))
        {
            return null;
        }

        if (normalizado.Length > longitudMaxima)
        {
            throw new DomainException($"{campo} no puede superar los {longitudMaxima} caracteres.");
        }

        return normalizado;
    }
}
