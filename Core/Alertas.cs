using System.Data;

namespace FloreHome.Core;

/// <summary>
/// Notificaciones de stock. Cada usuario tiene las suyas: aparecen cuando un producto queda con
/// 10 unidades o menos (stock bajo) o en 0 (agotado), y se quedan hasta que el usuario las marca como leídas.
/// Al iniciar sesión se vuelve a avisar de lo que siga bajo.
/// </summary>
public static class Alertas
{
    public const int UmbralStockBajo = 10;

    /// <summary>Se dispara cuando cambian las notificaciones (para actualizar la campana).</summary>
    public static event Action Cambio;

    /// <summary>Al iniciar sesión: revisa todo el inventario y avisa solo a ese usuario.</summary>
    public static void RevisarAlIniciar(int idUsuario) => Revisar(null, idUsuario);

    /// <summary>Deja constancia de cada inicio de sesión (solo el más reciente queda sin leer).</summary>
    public static void RegistrarInicioSesion(int idUsuario)
    {
        try
        {
            Db.Ejecutar(
                "UPDATE Notificacion SET Leida = 1, FechaLectura = NOW() WHERE IdUsuario = @u AND Tipo = 'Sesion' AND Leida = 0",
                ("@u", idUsuario));
            Db.Ejecutar(
                "INSERT INTO Notificacion (IdUsuario, Tipo, Mensaje) VALUES (@u, 'Sesion', @m)",
                ("@u", idUsuario), ("@m", "Iniciaste sesión en FloreHome."));
        }
        catch
        {
        }
        Cambio?.Invoke();
    }

    /// <summary>Después de vender, editar o anular: revisa esos productos (o todos si es null) para todos los usuarios.</summary>
    public static void RevisarProductos(IEnumerable<int> ids) => Revisar(ids == null ? null : ids.ToList(), 0);

    private static void Revisar(List<int> ids, int soloUsuario)
    {
        try
        {
            // Lo que ya se repuso deja de ser alerta
            Db.Ejecutar(
                "UPDATE Notificacion SET Leida = 1, FechaLectura = NOW() WHERE Leida = 0 " +
                "AND IdProducto IN (SELECT Idproducto FROM Producto WHERE Cantidad > @u)",
                ("@u", UmbralStockBajo));

            string filtro = ids == null ? "" : (ids.Count == 0 ? " AND 1 = 0" : " AND Idproducto IN (" + string.Join(",", ids) + ")");
            DataTable productos = Db.Tabla(
                "SELECT Idproducto, Descripcion, Cantidad FROM Producto WHERE Cantidad <= @u" + filtro,
                ("@u", UmbralStockBajo));

            if (productos.Rows.Count > 0)
            {
                var usuarios = new List<int>();
                if (soloUsuario > 0) usuarios.Add(soloUsuario);
                else
                    foreach (DataRow u in Db.Tabla("SELECT IdUsuario FROM Usuario").Rows)
                        usuarios.Add(Convert.ToInt32(u["IdUsuario"]));

                foreach (DataRow p in productos.Rows)
                {
                    int idProducto = Convert.ToInt32(p["Idproducto"]);
                    string nombre = Convert.ToString(p["Descripcion"]);
                    int cantidad = Convert.ToInt32(p["Cantidad"]);
                    string tipo = cantidad == 0 ? "Agotado" : "StockBajo";
                    string mensaje = cantidad == 0
                        ? "Agotado: " + nombre + ". Ya no quedan unidades."
                        : "Stock bajo: " + nombre + " (quedan " + cantidad + ").";

                    foreach (int usuario in usuarios)
                    {
                        if (tipo == "Agotado")
                            Db.Ejecutar(
                                "UPDATE Notificacion SET Leida = 1, FechaLectura = NOW() " +
                                "WHERE IdUsuario = @u AND IdProducto = @p AND Tipo = 'StockBajo' AND Leida = 0",
                                ("@u", usuario), ("@p", idProducto));

                        int sinLeer = Db.Contar(
                            "SELECT COUNT(*) FROM Notificacion WHERE IdUsuario = @u AND IdProducto = @p AND Tipo = @t AND Leida = 0",
                            ("@u", usuario), ("@p", idProducto), ("@t", tipo));
                        if (sinLeer > 0) continue;

                        Db.Ejecutar(
                            "INSERT INTO Notificacion (IdUsuario, Tipo, IdProducto, Mensaje) VALUES (@u, @t, @p, @m)",
                            ("@u", usuario), ("@t", tipo), ("@p", idProducto), ("@m", mensaje));
                    }
                }
            }
        }
        catch
        {
            // Las alertas nunca deben impedir vender o guardar
        }
        Cambio?.Invoke();
    }

    public static int ContarNoLeidas(int idUsuario)
    {
        try { return Db.Contar("SELECT COUNT(*) FROM Notificacion WHERE IdUsuario = @u AND Leida = 0", ("@u", idUsuario)); }
        catch { return 0; }
    }

    public static DataTable Listar(int idUsuario)
    {
        return Db.Tabla(
            "SELECT Id, Tipo, Mensaje, Fecha, Leida FROM Notificacion WHERE IdUsuario = @u " +
            "ORDER BY Leida ASC, Fecha DESC, Id DESC LIMIT 40",
            ("@u", idUsuario));
    }

    public static void Marcar(int id, int idUsuario)
    {
        Db.Ejecutar("UPDATE Notificacion SET Leida = 1, FechaLectura = NOW() WHERE Id = @id AND IdUsuario = @u",
            ("@id", id), ("@u", idUsuario));
        Cambio?.Invoke();
    }

    public static void MarcarTodas(int idUsuario)
    {
        Db.Ejecutar("UPDATE Notificacion SET Leida = 1, FechaLectura = NOW() WHERE IdUsuario = @u AND Leida = 0",
            ("@u", idUsuario));
        Cambio?.Invoke();
    }
}
