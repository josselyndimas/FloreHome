using System.Security.Cryptography;

namespace FloreHome.Core;

public static class Roles
{
    public const string Admin = "Administrador";
    public const string Empleado = "Empleado";
    public static readonly string[] Todos = { Admin, Empleado };
}

/// <summary>Usuario que inició sesión.</summary>
public static class Sesion
{
    public static int IdUsuario { get; set; }
    public static string Usuario { get; set; } = "";
    public static string NombreCompleto { get; set; } = "";
    public static string Rol { get; set; } = "";

    public static bool EsAdmin => Rol == Roles.Admin;

    public static void Cerrar()
    {
        IdUsuario = 0;
        Usuario = "";
        NombreCompleto = "";
        Rol = "";
    }
}

/// <summary>Contraseñas con sal aleatoria + PBKDF2 (SHA-256, 100 000 iteraciones).</summary>
public static class Passwords
{
    public static string GenerarSalt()
    {
        byte[] bytes = RandomNumberGenerator.GetBytes(16);
        return Convert.ToBase64String(bytes);
    }

    public static string GenerarHash(string password, string salt)
    {
        byte[] saltBytes = Convert.FromBase64String(salt);
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(password, saltBytes, 100000, HashAlgorithmName.SHA256, 32);
        return Convert.ToBase64String(hash);
    }

    public static bool Verificar(string password, string hashGuardado, string salt)
    {
        string calculado = GenerarHash(password, salt);
        return CryptographicOperations.FixedTimeEquals(
            System.Text.Encoding.UTF8.GetBytes(calculado),
            System.Text.Encoding.UTF8.GetBytes(hashGuardado));
    }
}
