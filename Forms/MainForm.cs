using FloreHome.Core;
using FloreHome.Ui;

namespace FloreHome.Forms;

/// <summary>Ventana principal: menú lateral y área de contenido.</summary>
public sealed class MainForm : Form
{
    private readonly Panel _contenido = new Panel();
    private readonly List<Button> _botonesMenu = new List<Button>();
    private readonly Font _fuenteMenu = new Font("Segoe UI", 11f);
    private readonly Campana _campana = new Campana();
    private DateTime _ultimoCierre = DateTime.MinValue;

    public bool CerrarSesionSolicitado { get; private set; }

    public MainForm()
    {
        Text = "FloreHome";
        StartPosition = FormStartPosition.CenterScreen;
        WindowState = FormWindowState.Maximized;
        MinimumSize = new Size(Tema.Px(1000), Tema.Px(660));
        AutoScaleMode = AutoScaleMode.None;
        BackColor = Tema.Fondo;
        ForeColor = Tema.Cafe;
        Font = Tema.FNormal;

        // ---------- Menú lateral ----------
        int anchoMenu = Tema.Px(250);
        var lateral = new TableLayoutPanel
        {
            Dock = DockStyle.Left,
            Width = anchoMenu,
            BackColor = Tema.Cafe,
            ColumnCount = 1,
            RowCount = 3
        };
        lateral.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, anchoMenu));
        lateral.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        lateral.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        lateral.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        // Cabecera con logo
        var cabecera = new TableLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            Dock = DockStyle.Fill,
            Padding = new Padding(0, Tema.Px(24), 0, Tema.Px(20))
        };
        var logo = new PixelBox
        {
            Size = new Size(Tema.Px(128), Tema.Px(128)),
            BackColor = Tema.Cafe,
            MostrarBorde = false,
            Anchor = AnchorStyles.None,
            Margin = new Padding(0, 0, 0, Tema.Px(6)),
            Imagen = Recursos.Logo()
        };
        var marca = new Label
        {
            Text = "FloreHome",
            Font = new Font("Georgia", 20f, FontStyle.Bold | FontStyle.Italic),
            ForeColor = Tema.Crema,
            AutoSize = true,
            Anchor = AnchorStyles.None,
            Margin = new Padding(0)
        };
        cabecera.Controls.Add(logo, 0, 0);
        cabecera.Controls.Add(marca, 0, 1);

        // Opciones
        var menu = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = false,
            Margin = new Padding(0)
        };
        var btnProductos = CrearBotonMenu("Productos", () => new ProductosView(), anchoMenu);
        var btnClientes = CrearBotonMenu("Clientes", () => new ClientesView(), anchoMenu);
        var btnFactura = CrearBotonMenu("Nueva factura", () => new NuevaFacturaView(), anchoMenu);
        var btnFacturas = CrearBotonMenu("Facturas", () => new FacturasView(), anchoMenu);
        menu.Controls.Add(btnProductos);
        menu.Controls.Add(btnClientes);
        menu.Controls.Add(btnFactura);
        menu.Controls.Add(btnFacturas);
        if (Sesion.EsAdmin)                       // solo el administrador ve y crea usuarios
            menu.Controls.Add(CrearBotonMenu("Usuarios", () => new UsuariosView(), anchoMenu));

        // Pie: quién está conectado + acciones
        var pie = new TableLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            Dock = DockStyle.Fill,
            Padding = new Padding(Tema.Px(20), Tema.Px(10), Tema.Px(20), Tema.Px(16))
        };
        var quien = new Label
        {
            Text = Sesion.NombreCompleto + "\n" + Sesion.Rol,
            AutoSize = false,
            Height = Tema.Px(52),
            Dock = DockStyle.Fill,
            Font = Tema.FNormal,
            ForeColor = Tema.Crema,
            Margin = new Padding(0, 0, 0, Tema.Px(8))
        };
        var btnClave = Tema.Boton("Cambiar contraseña", Estilo.Secundario);
        btnClave.Dock = DockStyle.Fill;
        btnClave.Margin = new Padding(0, 0, 0, Tema.Px(8));
        btnClave.Click += (s, e) =>
        {
            using var d = new CambiarClaveDialog();
            if (d.ShowDialog(this) == DialogResult.OK)
                MessageBox.Show(this, "Contraseña actualizada.", "FloreHome", MessageBoxButtons.OK, MessageBoxIcon.Information);
        };
        var btnSalir = Tema.Boton("Cerrar sesión", Estilo.Secundario);
        btnSalir.Dock = DockStyle.Fill;
        btnSalir.Margin = new Padding(0);
        btnSalir.Click += (s, e) =>
        {
            CerrarSesionSolicitado = true;
            Close();
        };
        pie.Controls.Add(quien, 0, 0);
        pie.Controls.Add(btnClave, 0, 1);
        pie.Controls.Add(btnSalir, 0, 2);

        lateral.Controls.Add(cabecera, 0, 0);
        lateral.Controls.Add(menu, 0, 1);
        lateral.Controls.Add(pie, 0, 2);

        // ---------- Contenido (barra superior con la campanita + pantalla) ----------
        _contenido.Dock = DockStyle.Fill;
        _contenido.BackColor = Tema.Fondo;

        var barra = new Panel
        {
            Dock = DockStyle.Top,
            Height = Tema.Px(56),
            BackColor = Tema.Fondo,
            Padding = new Padding(0, Tema.Px(6), Tema.Px(24), 0)
        };
        _campana.Dock = DockStyle.Right;
        _campana.Width = Tema.Px(48);
        _campana.Click += (s, e) => AbrirNotificaciones();
        barra.Controls.Add(_campana);

        var derecha = new Panel { Dock = DockStyle.Fill, BackColor = Tema.Fondo };
        derecha.Controls.Add(_contenido);   // primero el que llena…
        derecha.Controls.Add(barra);        // …y después la barra que se acopla arriba

        Controls.Add(derecha);
        Controls.Add(lateral);

        Alertas.Cambio += ActualizarCampana;
        Shown += (s, e) =>
        {
            Alertas.RegistrarInicioSesion(Sesion.IdUsuario);   // "Iniciaste sesión" con fecha y hora
            Alertas.RevisarAlIniciar(Sesion.IdUsuario);        // y avisa de lo que esté bajo de stock
        };
        ActualizarCampana();

        Mostrar(btnProductos, () => new ProductosView());
    }

    private void ActualizarCampana()
    {
        if (IsDisposed) return;
        _campana.Conteo = Alertas.ContarNoLeidas(Sesion.IdUsuario);
    }

    private void AbrirNotificaciones()
    {
        // Si el panel acaba de cerrarse porque se hizo clic en la campana, no lo vuelvas a abrir
        if ((DateTime.Now - _ultimoCierre).TotalMilliseconds < 300) return;

        var panel = new NotificacionesForm();
        panel.Location = _campana.PointToScreen(new Point(_campana.Width - panel.Width, _campana.Height + Tema.Px(4)));
        panel.FormClosed += (s, e) =>
        {
            _ultimoCierre = DateTime.Now;
            panel.Dispose();
            ActualizarCampana();
        };
        panel.Show(this);
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        Alertas.Cambio -= ActualizarCampana;
        base.OnFormClosed(e);
    }

    private Button CrearBotonMenu(string texto, Func<Control> crear, int ancho)
    {
        var b = new Button
        {
            Text = texto,
            Width = ancho,
            Height = Tema.Px(54),
            FlatStyle = FlatStyle.Flat,
            BackColor = Tema.Cafe,
            ForeColor = Tema.Crema,
            Font = _fuenteMenu,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(Tema.Px(28), 0, 0, 0),
            Margin = new Padding(0),
            Cursor = Cursors.Hand,
            UseVisualStyleBackColor = false
        };
        b.FlatAppearance.BorderSize = 0;
        b.FlatAppearance.MouseOverBackColor = Tema.CafeClaro;
        b.Click += (s, e) => Mostrar(b, crear);
        _botonesMenu.Add(b);
        return b;
    }

    private void Mostrar(Button activo, Func<Control> crear)
    {
        foreach (var b in _botonesMenu)
            b.BackColor = ReferenceEquals(b, activo) ? Tema.CafeClaro : Tema.Cafe;

        Cursor = Cursors.WaitCursor;
        try
        {
            var anteriores = _contenido.Controls.Cast<Control>().ToList();
            _contenido.Controls.Clear();
            foreach (var c in anteriores) c.Dispose();

            var vista = crear();
            vista.Dock = DockStyle.Fill;
            _contenido.Controls.Add(vista);
        }
        finally
        {
            Cursor = Cursors.Default;
        }
    }
}
