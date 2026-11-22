namespace FloreHome.Core;

/// <summary>
/// Reglas de cobro en un solo lugar.
/// Los precios de los productos ya incluyen el IVA (13 %): si dice $40, el cliente paga $40.
///  - Consumidor final: la factura solo muestra los productos y el total (no menciona el IVA).
///  - Crédito fiscal: muestra los productos sin IVA, el monto del IVA y el total (la suma de los dos).
/// </summary>
public static class Fiscal
{
    public const decimal TasaIva = 0.13m;

    public const string ConsumidorFinal = "Consumidor final";
    public const string CreditoFiscal = "Crédito fiscal";

    // Cliente frecuente: cada 6 compras de más de $25, la siguiente lleva 10 % de descuento
    public const decimal DescuentoFrecuente = 0.10m;
    public const int ComprasParaDescuento = 6;
    public const decimal MontoMinimoCompra = 25m;

    public static readonly string[] MetodosPago = { "Efectivo", "Tarjeta", "Transferencia" };

    public static decimal Redondear(decimal v) => Math.Round(v, 2, MidpointRounding.AwayFromZero);

    /// <summary>Quita el IVA a un monto que ya lo incluye.</summary>
    public static decimal SinIva(decimal conIva) => Redondear(conIva / (1 + TasaIva));

    /// <summary>total = lo que paga el cliente; iva = parte del total que es IVA; subtotal = total sin IVA.</summary>
    public static void Calcular(decimal bruto, decimal descuento, out decimal subtotal, out decimal iva, out decimal total)
    {
        total = bruto - descuento;
        iva = Redondear(total - total / (1 + TasaIva));
        subtotal = total - iva;
    }
}
