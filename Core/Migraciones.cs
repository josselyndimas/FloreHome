using System.Data;
using System.Text.RegularExpressions;

namespace FloreHome.Core;

/// <summary>
/// Agrega a una base de datos ya creada lo que se fue sumando (código de producto, método de pago,
/// tipo de factura, anulación, notificaciones…). Si todo ya existe, no hace nada.
/// </summary>
public static class Migraciones
{
    public static void Aplicar()
    {
        Agregar("Producto", "Codigo", "VARCHAR(20) NULL");
        Agregar("Factura", "TipoDocumento", "VARCHAR(20) NOT NULL DEFAULT 'Consumidor final'");
        Agregar("Factura", "MetodoPago", "VARCHAR(20) NOT NULL DEFAULT 'Efectivo'");
        Agregar("Factura", "Descuento", "DECIMAL(10,2) NOT NULL DEFAULT 0");
        Agregar("Factura", "Estado", "VARCHAR(10) NOT NULL DEFAULT 'Emitida'");
        Agregar("Factura", "FechaAnulacion", "DATETIME NULL");
        Agregar("Factura", "MotivoAnulacion", "VARCHAR(200) NULL");
        Agregar("Factura", "AnuladaPor", "INT NULL");

        Db.Ejecutar("CREATE TABLE IF NOT EXISTS Ajustes (Clave VARCHAR(50) PRIMARY KEY, Valor VARCHAR(100) NOT NULL)");
        Db.Ejecutar(
            "CREATE TABLE IF NOT EXISTS Notificacion (" +
            "Id INT AUTO_INCREMENT PRIMARY KEY, " +
            "IdUsuario INT NOT NULL, " +
            "Tipo VARCHAR(20) NOT NULL, " +
            "IdProducto INT NULL, " +
            "Mensaje VARCHAR(250) NOT NULL, " +
            "Fecha DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP, " +
            "Leida TINYINT(1) NOT NULL DEFAULT 0, " +
            "FechaLectura DATETIME NULL, " +
            "INDEX ix_notificacion_usuario (IdUsuario, Leida), " +
            "CONSTRAINT fk_notificacion_usuario FOREIGN KEY (IdUsuario) REFERENCES Usuario(IdUsuario) ON DELETE CASCADE, " +
            "CONSTRAINT fk_notificacion_producto FOREIGN KEY (IdProducto) REFERENCES Producto(Idproducto) ON DELETE CASCADE)");

        // Productos sin código: se les asigna uno según su nombre
        foreach (DataRow r in Db.Tabla("SELECT Idproducto, Descripcion FROM Producto WHERE Codigo IS NULL OR Codigo = '' ORDER BY Idproducto").Rows)
            Db.Ejecutar("UPDATE Producto SET Codigo = @c WHERE Idproducto = @id",
                ("@c", Codigos.Generar(Convert.ToString(r["Descripcion"]))), ("@id", Convert.ToInt32(r["Idproducto"])));

        // Una sola vez: los códigos de versiones anteriores (FLO-0001, ROS-0002…) pasan al formato por nombre
        if (Db.Contar("SELECT COUNT(*) FROM Ajustes WHERE Clave = 'codigos_por_nombre'") == 0)
        {
            foreach (DataRow r in Db.Tabla(
                "SELECT Idproducto, Descripcion, Codigo FROM Producto WHERE Codigo REGEXP '^[A-Z]{3}(-[A-Z]{3})?-[0-9]{4}$' ORDER BY Idproducto").Rows)
            {
                string nombre = Convert.ToString(r["Descripcion"]);
                string actual = Convert.ToString(r["Codigo"]);
                if (Regex.IsMatch(actual, "^" + Codigos.Prefijo(nombre) + "-[0-9]{4}$")) continue;
                Db.Ejecutar("UPDATE Producto SET Codigo = @c WHERE Idproducto = @id",
                    ("@c", Codigos.Generar(nombre)), ("@id", Convert.ToInt32(r["Idproducto"])));
            }
            Db.Ejecutar("INSERT INTO Ajustes (Clave, Valor) VALUES ('codigos_por_nombre', 'ok')");
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
