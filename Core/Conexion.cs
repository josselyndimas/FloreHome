using MySqlConnector;

namespace FloreHome.Core;

/// <summary>
/// Lee la cadena de conexión del archivo conexion.txt (junto al programa).
/// Ese archivo NO se sube a GitHub (está en .gitignore).
/// </summary>
public static class Conexion
{
    private const string Defecto = "Server=localhost;Port=3306;Database=florehome;Uid=root;Pwd=D(imas26);";

    public static string Cadena { get; } = Cargar();

    private static string Cargar()
    {
        string texto = Defecto;
        try
        {
            string ruta = Path.Combine(AppContext.BaseDirectory, "conexion.txt");
            if (File.Exists(ruta))
            {
                string leido = File.ReadAllText(ruta).Trim();
                if (leido.Length > 0) texto = leido;
            }
        }
        catch
        {
            // si no se puede leer, se usa la conexión local por defecto
        }
        return texto.TrimEnd(';') + ";CharSet=utf8mb4;AllowPublicKeyRetrieval=True;";
    }

    public static MySqlConnection Crear() => new MySqlConnection(Cadena);

    /// <summary>Comprueba que hay conexión y que las tablas existen.</summary>
    public static bool Probar(out string error)
    {
        try
        {
            using var cn = Crear();
            cn.Open();
            using var cmd = new MySqlCommand("SELECT COUNT(*) FROM Usuario", cn);
            cmd.ExecuteScalar();
            error = "";
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }
}
