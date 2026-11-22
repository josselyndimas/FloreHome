using FloreHome.Core;
using FloreHome.Ui;

namespace FloreHome.Forms;

/// <summary>Inicio de sesión. No hay registro: los usuarios los crea el administrador.</summary>
public sealed class LoginForm : Form
{
    private readonly TextBox _usuario = Tema.Caja(50);
    private readonly TextBox _clave = Tema.Caja(60);
    private readonly PixelBox _logo = new PixelBox();

    public LoginForm()
    {
        Text = "FloreHome · Iniciar sesión";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterScreen;
        MaximizeBox = false;
        MinimizeBox = false;
        AutoScaleMode = AutoScaleMode.None;
        BackColor = Tema.Fondo;
        ForeColor = Tema.Cafe;
        Font = Tema.FNormal;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        Padding = new Padding(Tema.Px(40));

        var t = new TableLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            Dock = DockStyle.Fill
        };
        t.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, Tema.Px(320)));

        _logo.Size = new Size(Tema.Px(128), Tema.Px(128));
        _logo.BackColor = Tema.Fondo;
        _logo.MostrarBorde = false;
        _logo.Anchor = AnchorStyles.None;
        _logo.Margin = new Padding(0, 0, 0, Tema.Px(8));
        _logo.Imagen = Recursos.Logo();

        var titulo = new Label
        {
            Text = "FloreHome",
            Font = Tema.FTitulo,
            ForeColor = Tema.Cafe,
            AutoSize = true,
            Anchor = AnchorStyles.None,
            Margin = new Padding(0)
        };
        var subtitulo = new Label
        {
            Text = "Administración de floristería",
            Font = Tema.FNormal,
            ForeColor = Tema.CafeClaro,
            AutoSize = true,
            Anchor = AnchorStyles.None,
            Margin = new Padding(0, 0, 0, Tema.Px(24))
        };

        var lblUsuario = Tema.Etiqueta("Usuario", true);
        lblUsuario.Margin = new Padding(0, 0, 0, Tema.Px(4));
        _usuario.Dock = DockStyle.Fill;
        _usuario.Margin = new Padding(0, 0, 0, Tema.Px(14));

        var lblClave = Tema.Etiqueta("Contraseña", true);
        lblClave.Margin = new Padding(0, 0, 0, Tema.Px(4));
        _clave.Dock = DockStyle.Fill;
        _clave.UseSystemPasswordChar = true;
        _clave.Margin = new Padding(0, 0, 0, Tema.Px(22));

        var entrar = Tema.Boton("Ingresar", Estilo.Primario);
        entrar.Dock = DockStyle.Fill;
        entrar.Margin = new Padding(0);
        entrar.MinimumSize = new Size(0, Tema.Px(48));
        entrar.Click += (s, e) => Ingresar();

        t.Controls.Add(_logo, 0, 0);
        t.Controls.Add(titulo, 0, 1);
        t.Controls.Add(subtitulo, 0, 2);
        t.Controls.Add(lblUsuario, 0, 3);
        t.Controls.Add(_usuario, 0, 4);
        t.Controls.Add(lblClave, 0, 5);
        t.Controls.Add(_clave, 0, 6);
        t.Controls.Add(entrar, 0, 7);
        Controls.Add(t);

        AcceptButton = entrar;
    }

    private void Ingresar()
    {
        string usuario = _usuario.Text.Trim();
        string clave = _clave.Text;
        if (usuario.Length == 0 || clave.Length == 0)
        {
            MessageBox.Show(this, "Escribe tu usuario y tu contraseña.", "FloreHome", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            var dt = Db.Tabla(
                "SELECT IdUsuario, Nombre, NombreCompleto, PasswordHash, PasswordSalt, Rol FROM Usuario WHERE Nombre = @n",
                ("@n", usuario));

            if (dt.Rows.Count == 0 ||
                !Passwords.Verificar(clave, Convert.ToString(dt.Rows[0]["PasswordHash"]), Convert.ToString(dt.Rows[0]["PasswordSalt"])))
            {
                MessageBox.Show(this, "Usuario o contraseña incorrectos.", "FloreHome", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _clave.Clear();
                _clave.Focus();
                return;
            }

            var f = dt.Rows[0];
            Sesion.IdUsuario = Convert.ToInt32(f["IdUsuario"]);
            Sesion.Usuario = Convert.ToString(f["Nombre"]);
            Sesion.NombreCompleto = Convert.ToString(f["NombreCompleto"]);
            Sesion.Rol = Convert.ToString(f["Rol"]);
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "No se pudo iniciar sesión.\n\n" + ex.Message, "FloreHome", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
