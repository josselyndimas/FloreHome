using FloreHome.Core;
using FloreHome.Forms;

namespace FloreHome;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.ThreadException += (s, e) =>
            MessageBox.Show("Ocurrió un error inesperado.\n\n" + e.Exception.Message, "FloreHome",
                MessageBoxButtons.OK, MessageBoxIcon.Error);

        // 1) ¿Hay conexión y existen las tablas?
        if (!Conexion.Probar(out string error))
        {
            MessageBox.Show(
                "No se pudo conectar con la base de datos.\n\n" +
                "Revisa que MySQL esté encendido, que hayas ejecutado Database\\florehome.sql " +
                "y que el archivo conexion.txt tenga tu usuario y contraseña de MySQL.\n\n" + error,
                "FloreHome", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        // 1b) Si la base de datos es de una versión anterior, agrega lo nuevo (códigos, pago, anulación…)
        try
        {
            Migraciones.Aplicar();
        }
        catch (Exception ex)
        {
            MessageBox.Show("No se pudo actualizar la base de datos.\n\n" + ex.Message, "FloreHome",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        // 2) Primer arranque: todavía no existe ningún usuario → crear al administrador
        if (Db.Contar("SELECT COUNT(*) FROM Usuario") == 0)
        {
            using var inicial = new UsuarioDialog(null, true);
            if (inicial.ShowDialog() != DialogResult.OK) return;
        }

        // 3) Iniciar sesión; al cerrar sesión se vuelve a pedir
        while (true)
        {
            using (var login = new LoginForm())
            {
                if (login.ShowDialog() != DialogResult.OK) break;
            }

            using var principal = new MainForm();
            Application.Run(principal);
            Sesion.Cerrar();
            if (!principal.CerrarSesionSolicitado) break;
        }
    }
}
