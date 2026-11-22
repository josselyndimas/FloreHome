using System.Data;
using FloreHome.Core;
using FloreHome.Ui;

namespace FloreHome.Forms;

/// <summary>Solo la ve el administrador.</summary>
public sealed class UsuariosView : VistaLista
{
    protected override string Titulo => "Usuarios";
    protected override string[] ColumnasBusqueda => new[] { "Nombre", "NombreCompleto", "Rol" };

    public UsuariosView()
    {
        AgregarBoton("Nuevo usuario", Estilo.Primario, (s, e) => Nuevo());
        AgregarBoton("Editar", Estilo.Secundario, (s, e) => Editar());
        AgregarBoton("Eliminar", Estilo.Peligro, (s, e) => Eliminar());
        AgregarExportaciones();
        Grid.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0) Editar(); };
    }

    protected override DataTable Consultar() =>
        Db.Tabla("SELECT IdUsuario, Nombre, NombreCompleto, Rol, FechaRegistro FROM Usuario ORDER BY Nombre");

    protected override void CrearColumnas()
    {
        Col("Nombre", "Usuario", 20, minimo: 120);
        Col("NombreCompleto", "Nombre completo", 34, minimo: 180);
        Col("Rol", "Rol", 18, minimo: 120);
        Col("FechaRegistro", "Registro", 20, "dd/MM/yyyy", minimo: 110);
    }

    private void Nuevo()
    {
        using var d = new UsuarioDialog();
        if (d.ShowDialog(FindForm()) == DialogResult.OK) Recargar();
    }

    private void Editar()
    {
        var f = Seleccion();
        if (f == null) { Avisar("Selecciona un usuario de la lista."); return; }
        using var d = new UsuarioDialog(Convert.ToInt32(f["IdUsuario"]));
        if (d.ShowDialog(FindForm()) == DialogResult.OK) Recargar();
    }

    private void Eliminar()
    {
        var f = Seleccion();
        if (f == null) { Avisar("Selecciona un usuario de la lista."); return; }
        int id = Convert.ToInt32(f["IdUsuario"]);

        if (id == Sesion.IdUsuario) { Avisar("No puedes eliminar tu propio usuario mientras estás conectada."); return; }
        if (Convert.ToString(f["Rol"]) == Roles.Admin &&
            Db.Contar("SELECT COUNT(*) FROM Usuario WHERE Rol = @r AND IdUsuario <> @id", ("@r", Roles.Admin), ("@id", id)) == 0)
        {
            Avisar("Debe existir al menos un administrador.");
            return;
        }
        if (!Confirmar("¿Eliminar al usuario \"" + f["Nombre"] + "\"?\nSus facturas anteriores se conservan.")) return;
        try
        {
            Db.Ejecutar("DELETE FROM Usuario WHERE IdUsuario = @id", ("@id", id));
            Recargar();
        }
        catch (Exception ex) { ErrorMsg("No se pudo eliminar el usuario.", ex); }
    }
}
