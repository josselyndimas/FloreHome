using FloreHome.Core;
using FloreHome.Ui;

namespace FloreHome.Forms;

public sealed class ClienteDialog : DialogoBase
{
    private readonly int? _id;
    private readonly TextBox _nombre = Tema.Caja(100);
    private readonly TextBox _telefono = Tema.Caja();
    private readonly TextBox _email = Tema.Caja(100);
    private readonly TextBox _direccion = Tema.Caja(200);

    public ClienteDialog(int? id = null)
        : base(id == null ? "Nuevo cliente" : "Editar cliente", id == null ? "Nuevo cliente" : "Editar cliente")
    {
        _id = id;
        Validar.SoloLetras(_nombre);
        Validar.Telefono(_telefono);

        AgregarCampo("Nombre", _nombre);
        AgregarCampo("Teléfono", _telefono);
        AgregarCampo("Correo (opcional)", _email);
        AgregarCampo("Dirección (opcional)", _direccion);

        Regla(_nombre, () => _nombre.Text.Trim().Length >= 2 && Validar.NombreValido(_nombre.Text)
            ? null : "Escribe el nombre. Solo se permiten letras y espacios, sin números.");
        Regla(_telefono, () => Validar.TelefonoValido(_telefono.Text.Trim())
            ? null : "El teléfono debe tener 8 números. Ejemplo: 1234-5678");
        Regla(_email, () => _email.Text.Trim().Length == 0 || Validar.EmailValido(_email.Text.Trim())
            ? null : "El correo no es válido. Ejemplo: nombre@correo.com");

        if (_id != null)
        {
            var dt = Db.Tabla("SELECT Nombre, Telefono, Email, Direccion FROM Cliente WHERE Idcliente = @id", ("@id", _id.Value));
            if (dt.Rows.Count > 0)
            {
                var f = dt.Rows[0];
                _nombre.Text = Convert.ToString(f["Nombre"]);
                _telefono.Text = Convert.ToString(f["Telefono"]);
                _email.Text = f["Email"] == DBNull.Value ? "" : Convert.ToString(f["Email"]);
                _direccion.Text = f["Direccion"] == DBNull.Value ? "" : Convert.ToString(f["Direccion"]);
            }
        }
    }

    protected override bool Guardar()
    {
        string nombre = _nombre.Text.Trim();
        string tel = _telefono.Text.Trim();
        string correo = _email.Text.Trim();
        string dir = _direccion.Text.Trim();

        if (!ValidarTodo()) return false;

        try
        {
            if (_id == null)
                Db.Ejecutar("INSERT INTO Cliente (Nombre, Telefono, Email, Direccion) VALUES (@n, @t, @e, @d)",
                    ("@n", nombre), ("@t", tel),
                    ("@e", correo.Length == 0 ? null : correo),
                    ("@d", dir.Length == 0 ? null : dir));
            else
                Db.Ejecutar("UPDATE Cliente SET Nombre=@n, Telefono=@t, Email=@e, Direccion=@d WHERE Idcliente=@id",
                    ("@n", nombre), ("@t", tel),
                    ("@e", correo.Length == 0 ? null : correo),
                    ("@d", dir.Length == 0 ? null : dir),
                    ("@id", _id.Value));
            return true;
        }
        catch (Exception ex)
        {
            ErrorBD("No se pudo guardar el cliente.", ex);
            return false;
        }
    }
}
