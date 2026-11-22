using System.Data;
using MySqlConnector;

namespace FloreHome.Core;

/// <summary>Atajos para consultas con parámetros.</summary>
public static class Db
{
    private static void Parametros(MySqlCommand cmd, (string Nombre, object Valor)[] p)
    {
        foreach (var par in p)
            cmd.Parameters.AddWithValue(par.Nombre, par.Valor ?? DBNull.Value);
    }

    public static DataTable Tabla(string sql, params (string Nombre, object Valor)[] p)
    {
        using var cn = Conexion.Crear();
        cn.Open();
        using var cmd = new MySqlCommand(sql, cn);
        Parametros(cmd, p);
        using var da = new MySqlDataAdapter(cmd);
        var dt = new DataTable();
        da.Fill(dt);
        return dt;
    }

    public static object Escalar(string sql, params (string Nombre, object Valor)[] p)
    {
        using var cn = Conexion.Crear();
        cn.Open();
        using var cmd = new MySqlCommand(sql, cn);
        Parametros(cmd, p);
        return cmd.ExecuteScalar();
    }

    public static int Contar(string sql, params (string Nombre, object Valor)[] p)
    {
        object r = Escalar(sql, p);
        return (r == null || r == DBNull.Value) ? 0 : Convert.ToInt32(r);
    }

    public static int Ejecutar(string sql, params (string Nombre, object Valor)[] p)
    {
        using var cn = Conexion.Crear();
        cn.Open();
        using var cmd = new MySqlCommand(sql, cn);
        Parametros(cmd, p);
        return cmd.ExecuteNonQuery();
    }
}
