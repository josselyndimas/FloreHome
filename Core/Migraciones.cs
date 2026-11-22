namespace FloreHome.Core;

/// <summary>
/// Agrega a una base de datos ya creada las columnas nuevas (código de producto, método de pago,
/// descuento y anulación de facturas). Si todo ya existe, no hace nada.
/// </summary>
public static class Migraciones
{
    public static void Aplicar()
    {
        Agregar("Producto", "Codigo", "VARCHAR(20) NULL");
        Agregar("Factura", "MetodoPago", "VARCHAR(20) NOT NULL DEFAULT 'Efectivo'");
        Agregar("Factura", "Descuento", "DECIMAL(10,2) NOT NULL DEFAULT 0");
        Agregar("Factura", "TipoDocumento", "VARCHAR(20) NOT NULL DEFAULT 'Consumidor final'");
        Agregar("Factura", "Estado", "VARCHAR(10) NOT NULL DEFAULT 'Emitida'");
        Agregar("Factura", "FechaAnulacion", "DATETIME NULL");
        Agregar("Factura", "MotivoAnulacion", "VARCHAR(200) NULL");
        Agregar("Factura", "AnuladaPor", "INT NULL");

        // Productos sin código: se les asigna uno según su nombre
        foreach (System.Data.DataRow r in Db.Tabla("SELECT Idproducto, Descripcion FROM Producto WHERE Codigo IS NULL OR Codigo = ''").Rows)
            Db.Ejecutar("UPDATE Producto SET Codigo = @c WHERE Idproducto = @id",
                ("@c", Codigos.Generar(Convert.ToString(r["Descripcion"]))), ("@id", Convert.ToInt32(r["Idproducto"])));

        // Códigos de una versión anterior (FLO-0001 por categoría): pasan a usar las letras del nombre
        foreach (System.Data.DataRow r in Db.Tabla(
            "SELECT Idproducto, Descripcion, Codigo FROM Producto " +
            "WHERE Codigo REGEXP '^(FLO|RAM|PLA|ARR|REG|EXT|OTR)-[0-9]{4}$'").Rows)
        {
            string nombre = Convert.ToString(r["Descripcion"]);
            string actual = Convert.ToString(r["Codigo"]);
            if (actual.StartsWith(Codigos.Prefijo(nombre) + "-")) continue;
            Db.Ejecutar("UPDATE Producto SET Codigo = @c WHERE Idproducto = @id",
                ("@c", Codigos.Generar(nombre)), ("@id", Convert.ToInt32(r["Idproducto"])));
        }

        int indice = Db.Contar(
            "SELECT COUNT(*) FROM INFORMATION_SCHEMA.STATISTICS WHERE TABLE_SCHEMA = DATABASE() " +
            "AND LOWER(TABLE_NAME) = 'producto' AND INDEX_NAME = 'ux_producto_codigo'");
        if (indice == 0)
            Db.Ejecutar("CREATE UNIQUE INDEX ux_producto_codigo ON Producto (Codigo)");
    }

    private static void Agregar(string tabla, string columna, string definicion)
    {
        int existe = Db.Contar(
            "SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = DATABASE() " +
            "AND LOWER(TABLE_NAME) = LOWER(@t) AND LOWER(COLUMN_NAME) = LOWER(@c)",
            ("@t", tabla), ("@c", columna));
        if (existe == 0)
            Db.Ejecutar("ALTER TABLE " + tabla + " ADD COLUMN " + columna + " " + definicion);
    }
}
