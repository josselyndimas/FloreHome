using System.Data;
using FloreHome.Core;
using FloreHome.Ui;

namespace FloreHome.Forms;

public sealed class ClientesView : VistaLista
{
    protected override string Titulo => "Clientes";
    protected override string[] ColumnasBusqueda => new[] { "Nombre", "Telefono", "Email", "Direccion" };

    public ClientesView()
    {
        AgregarBoton("Nuevo cliente", Estilo.Primario, (s, e) => Nuevo());
        AgregarBoton("Editar", Estilo.Secundario, (s, e) => Editar());
        if (Sesion.EsAdmin) AgregarBoton("Eliminar", Estilo.Peligro, (s, e) => Eliminar());
        AgregarExportaciones();
        Grid.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0) Editar(); };
    }

    protected override DataTable Consultar() =>
        Db.Tabla("SELECT Idcliente, Nombre, Telefono, Email, Direccion FROM Cliente ORDER BY Nombre");

    protected override void CrearColumnas()
    {
        Col("Nombre", "Nombre", 28, minimo: 150);
        Col("Telefono", "Teléfono", 14, minimo: 110);
        Col("Email", "Correo", 28, minimo: 160);
        Col("Direccion", "Dirección", 30, minimo: 160);
    }

    private void Nuevo()
    {
        using var d = new ClienteDialog();
        if (d.ShowDialog(FindForm()) == DialogResult.OK) Recargar();
    }

    private void Editar()
    {
        var f = Seleccion();
        if (f == null) { Avisar("Selecciona un cliente de la lista."); return; }
        using var d = new ClienteDialog(Convert.ToInt32(f["Idcliente"]));
        if (d.ShowDialog(FindForm()) == DialogResult.OK) Recargar();
    }

    private void Eliminar()
    {
        var f = Seleccion();
        if (f == null) { Avisar("Selecciona un cliente de la lista."); return; }
        if (!Confirmar("¿Eliminar al cliente \"" + f["Nombre"] + "\"?\nSus facturas anteriores se conservan.")) return;
        try
        {
            Db.Ejecutar("DELETE FROM Cliente WHERE Idcliente = @id", ("@id", Convert.ToInt32(f["Idcliente"])));
            Recargar();
        }
        catch (Exception ex) { ErrorMsg("No se pudo eliminar el cliente.", ex); }
    }
}
