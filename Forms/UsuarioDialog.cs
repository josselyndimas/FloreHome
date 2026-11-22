using FloreHome.Core;
using FloreHome.Ui;

namespace FloreHome.Forms;

/// <summary>Crear o editar usuarios. También se usa en el primer arranque para crear al administrador.</summary>
public sealed class UsuarioDialog : DialogoBase
{
    private readonly int? _id;
    private readonly bool _inicial;
    private readonly TextBox _usuario = Tema.Caja(50);
    private readonly TextBox _nombre = Tema.Caja(100);
    private readonly ComboBox _rol = Tema.Combo(Roles.Todos);
    private readonly TextBox _clave = Tema.Caja(60);
    private readonly TextBox _clave2 = Tema.Caja(60);

    public UsuarioDialog(int? id = null, bool inicial = false)
        : base(inicial ? "Bienvenida a FloreHome" : (id == null ? "Nuevo usuario" : "Editar usuario"),
               inicial ? "Crea el administrador" : (id == null ? "Nuevo usuario" : "Editar usuario"))
    {
        _id = id;
        _inicial = inicial;
        Validar.SoloUsuario(_usuario);
        Validar.SoloLetras(_nombre);
        _clave.UseSystemPasswordChar = true;
        _clave2.UseSystemPasswordChar = true;

        AgregarCampo("Usuario", _usuario);
        AgregarCampo("Nombre completo", _nombre);
        AgregarCampo("Rol", _rol);
        AgregarCampo(id == null ? "Contraseña" : "Nueva contraseña (opcional)", _clave);
        AgregarCampo("Repetir contraseña", _clave2);

        Regla(_usuario, () => Validar.UsuarioValido(_usuario.Text.Trim())
            ? null : "El usuario debe tener entre 3 y 30 caracteres: letras, números, punto o guion bajo.");
        Regla(_nombre, () => _nombre.Text.Trim().Length >= 2 && Validar.NombreValido(_nombre.Text)
            ? null : "Escribe el nombre completo. Solo se permiten letras y espacios, sin números.");
        Regla(_clave, () => (_id != null && _clave.Text.Length == 0) || _clave.Text.Length >= 6
            ? null : "La contraseña debe tener al menos 6 caracteres.");
        Regla(_clave2, () => _clave2.Text == _clave.Text ? null : "Las contraseñas no coinciden.");

        if (inicial)
        {
            _rol.SelectedItem = Roles.Admin;
            _rol.Enabled = false;
            BtnCancelar.Text = "Salir";
        }

        if (_id != null)
        {
            var dt = Db.Tabla("SELECT Nombre, NombreCompleto, Rol FROM Usuario WHERE IdUsuario = @id", ("@id", _id.Value));
            if (dt.Rows.Count > 0)
            {
                var f = dt.Rows[0];
                _usuario.Text = Convert.ToString(f["Nombre"]);
                _nombre.Text = Convert.ToString(f["NombreCompleto"]);
                _rol.SelectedItem = Convert.ToString(f["Rol"]);
            }
            // Para no quedarse sin acceso, nadie cambia su propio rol
            if (_id.Value == Sesion.IdUsuario) _rol.Enabled = false;
        }
    }

    protected override bool Guardar()
    {
        string usuario = _usuario.Text.Trim();
        string nombre = _nombre.Text.Trim();
        string rol = Convert.ToString(_rol.SelectedItem) ?? Roles.Empleado;
        string clave = _clave.Text;

        if (!ValidarTodo()) return false;
        bool cambiaClave = _id == null || clave.Length > 0;

        try
        {
            int repetido = Db.Contar("SELECT COUNT(*) FROM Usuario WHERE Nombre = @n AND IdUsuario <> @id",
                ("@n", usuario), ("@id", _id ?? 0));
            if (repetido > 0)
            {
                Aviso("Ese nombre de usuario ya existe. Elige otro.");
                _usuario.Focus();
                return false;
            }

            if (_id != null && rol != Roles.Admin)
            {
                int otrosAdmin = Db.Contar("SELECT COUNT(*) FROM Usuario WHERE Rol = @r AND IdUsuario <> @id",
                    ("@r", Roles.Admin), ("@id", _id.Value));
                if (otrosAdmin == 0)
                {
                    Aviso("Debe existir al menos un administrador.");
                    return false;
                }
            }

            if (_id == null)
            {
                string salt = Passwords.GenerarSalt();
                Db.Ejecutar(
                    "INSERT INTO Usuario (Nombre, NombreCompleto, PasswordHash, PasswordSalt, Rol) VALUES (@n, @c, @h, @s, @r)",
                    ("@n", usuario), ("@c", nombre), ("@h", Passwords.GenerarHash(clave, salt)), ("@s", salt), ("@r", rol));
            }
            else if (cambiaClave)
            {
                string salt = Passwords.GenerarSalt();
                Db.Ejecutar(
                    "UPDATE Usuario SET Nombre=@n, NombreCompleto=@c, Rol=@r, PasswordHash=@h, PasswordSalt=@s WHERE IdUsuario=@id",
                    ("@n", usuario), ("@c", nombre), ("@r", rol),
                    ("@h", Passwords.GenerarHash(clave, salt)), ("@s", salt), ("@id", _id.Value));
            }
            else
            {
                Db.Ejecutar("UPDATE Usuario SET Nombre=@n, NombreCompleto=@c, Rol=@r WHERE IdUsuario=@id",
                    ("@n", usuario), ("@c", nombre), ("@r", rol), ("@id", _id.Value));
            }

            if (_id != null && _id.Value == Sesion.IdUsuario)
            {
                Sesion.Usuario = usuario;
                Sesion.NombreCompleto = nombre;
            }
            return true;
        }
        catch (Exception ex)
        {
            ErrorBD("No se pudo guardar el usuario.", ex);
            return false;
        }
    }
}

/// <summary>Cambiar la propia contraseña (disponible para cualquier rol).</summary>
public sealed class CambiarClaveDialog : DialogoBase
{
    private readonly TextBox _actual = Tema.Caja(60);
    private readonly TextBox _nueva = Tema.Caja(60);
    private readonly TextBox _nueva2 = Tema.Caja(60);

    public CambiarClaveDialog() : base("Cambiar contraseña", "Cambiar contraseña")
    {
        _actual.UseSystemPasswordChar = true;
        _nueva.UseSystemPasswordChar = true;
        _nueva2.UseSystemPasswordChar = true;
        AgregarCampo("Contraseña actual", _actual);
        AgregarCampo("Nueva contraseña", _nueva);
        AgregarCampo("Repetir nueva", _nueva2);
    }

    protected override bool Guardar()
    {
        try
        {
            var dt = Db.Tabla("SELECT PasswordHash, PasswordSalt FROM Usuario WHERE IdUsuario = @id", ("@id", Sesion.IdUsuario));
            if (dt.Rows.Count == 0) { Aviso("No se encontró el usuario."); return false; }
            string hash = Convert.ToString(dt.Rows[0]["PasswordHash"]);
            string salt = Convert.ToString(dt.Rows[0]["PasswordSalt"]);

            if (!Passwords.Verificar(_actual.Text, hash, salt))
            {
                Aviso("La contraseña actual no es correcta.");
                _actual.Focus();
                return false;
            }
            if (_nueva.Text.Length < 6)
            {
                Aviso("La nueva contraseña debe tener al menos 6 caracteres.");
                _nueva.Focus();
                return false;
            }
            if (_nueva.Text != _nueva2.Text)
            {
                Aviso("Las contraseñas nuevas no coinciden.");
                _nueva2.Focus();
                return false;
            }

            string nuevaSalt = Passwords.GenerarSalt();
            Db.Ejecutar("UPDATE Usuario SET PasswordHash=@h, PasswordSalt=@s WHERE IdUsuario=@id",
                ("@h", Passwords.GenerarHash(_nueva.Text, nuevaSalt)), ("@s", nuevaSalt), ("@id", Sesion.IdUsuario));
            return true;
        }
        catch (Exception ex)
        {
            ErrorBD("No se pudo cambiar la contraseña.", ex);
            return false;
        }
    }
}
