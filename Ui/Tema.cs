using System.Drawing.Drawing2D;

namespace FloreHome.Ui;

public enum Estilo { Primario, Secundario, Peligro }

/// <summary>Paleta "Mint chocolate" y ayudas para crear controles con el mismo estilo.</summary>
public static class Tema
{
    // ---- Paleta ----
    public static readonly Color Cafe = ColorTranslator.FromHtml("#684F48");
    public static readonly Color CafeClaro = ColorTranslator.FromHtml("#85685F");
    public static readonly Color Menta = ColorTranslator.FromHtml("#D6E9ED");
    public static readonly Color MentaSel = ColorTranslator.FromHtml("#BFDDE3");
    public static readonly Color Gris = ColorTranslator.FromHtml("#CCD6D6");
    public static readonly Color Crema = ColorTranslator.FromHtml("#F9E7C7");
    public static readonly Color CremaSuave = ColorTranslator.FromHtml("#FDF6E6");
    public static readonly Color Fondo = ColorTranslator.FromHtml("#FFFCF0");
    public static readonly Color Peligro = ColorTranslator.FromHtml("#A65D57");
    public static readonly Color PeligroHover = ColorTranslator.FromHtml("#8E4A45");

    // ---- Tipografías ----
    public static readonly Font FNormal = new Font("Segoe UI", 10f);
    public static readonly Font FNegrita = new Font("Segoe UI", 10f, FontStyle.Bold);
    public static readonly Font FGrande = new Font("Segoe UI", 15f, FontStyle.Bold);
    public static readonly Font FPrecio = new Font("Segoe UI", 12f, FontStyle.Bold);
    public static readonly Font FTitulo = new Font("Georgia", 22f, FontStyle.Bold | FontStyle.Italic);

    // ---- Escala según el DPI de la pantalla (todos los tamaños fijos pasan por Px) ----
    public static readonly float Escala = CalcularEscala();

    private static float CalcularEscala()
    {
        using var g = Graphics.FromHwnd(IntPtr.Zero);
        return g.DpiX / 96f;
    }

    public static int Px(int valor) => (int)Math.Round(valor * Escala);

    // ---- Fábrica de controles ----
    public static Button Boton(string texto, Estilo estilo = Estilo.Primario)
    {
        var b = new Button
        {
            Text = texto,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlatStyle = FlatStyle.Flat,
            Font = FNegrita,
            Cursor = Cursors.Hand,
            UseVisualStyleBackColor = false,
            Padding = new Padding(Px(14), Px(6), Px(14), Px(6)),
            MinimumSize = new Size(Px(110), Px(42)),
            Margin = new Padding(0, 0, Px(10), Px(8))
        };
        b.FlatAppearance.BorderSize = 0;
        switch (estilo)
        {
            case Estilo.Primario:
                b.BackColor = Cafe; b.ForeColor = Crema; b.FlatAppearance.MouseOverBackColor = CafeClaro;
                break;
            case Estilo.Secundario:
                b.BackColor = Menta; b.ForeColor = Cafe; b.FlatAppearance.MouseOverBackColor = MentaSel;
                break;
            default:
                b.BackColor = Peligro; b.ForeColor = Color.White; b.FlatAppearance.MouseOverBackColor = PeligroHover;
                break;
        }
        return b;
    }

    public static Label Etiqueta(string texto, bool negrita = false)
    {
        return new Label
        {
            Text = texto,
            AutoSize = true,
            Font = negrita ? FNegrita : FNormal,
            ForeColor = Cafe,
            Margin = new Padding(0)
        };
    }

    public static TextBox Caja(int maxLength = 0)
    {
        var t = new TextBox
        {
            Font = FNormal,
            ForeColor = Cafe,
            BackColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle
        };
        if (maxLength > 0) t.MaxLength = maxLength;
        return t;
    }

    public static ComboBox Combo(params string[] opciones)
    {
        var c = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = FNormal,
            ForeColor = Cafe,
            BackColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        c.Items.AddRange(opciones);
        if (opciones.Length > 0) c.SelectedIndex = 0;
        return c;
    }

    public static void EstilizarGrid(DataGridView g)
    {
        g.AutoGenerateColumns = false;
        g.BorderStyle = BorderStyle.None;
        g.BackgroundColor = Fondo;
        g.GridColor = Menta;
        g.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        g.EnableHeadersVisualStyles = false;
        g.RowHeadersVisible = false;
        g.AllowUserToAddRows = false;
        g.AllowUserToDeleteRows = false;
        g.AllowUserToResizeRows = false;
        g.ReadOnly = true;
        g.MultiSelect = false;
        g.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        g.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        g.Font = FNormal;

        g.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
        g.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        g.ColumnHeadersHeight = Px(44);
        g.ColumnHeadersDefaultCellStyle.BackColor = Menta;
        g.ColumnHeadersDefaultCellStyle.ForeColor = Cafe;
        g.ColumnHeadersDefaultCellStyle.SelectionBackColor = Menta;
        g.ColumnHeadersDefaultCellStyle.SelectionForeColor = Cafe;
        g.ColumnHeadersDefaultCellStyle.Font = FNegrita;
        g.ColumnHeadersDefaultCellStyle.Padding = new Padding(Px(8), 0, Px(8), 0);
        g.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;

        g.RowTemplate.Height = Px(38);
        g.DefaultCellStyle.BackColor = Color.White;
        g.DefaultCellStyle.ForeColor = Cafe;
        g.DefaultCellStyle.SelectionBackColor = MentaSel;
        g.DefaultCellStyle.SelectionForeColor = Cafe;
        g.DefaultCellStyle.Padding = new Padding(Px(8), 0, Px(8), 0);
        g.AlternatingRowsDefaultCellStyle.BackColor = CremaSuave;
        g.AlternatingRowsDefaultCellStyle.SelectionBackColor = MentaSel;
        g.AlternatingRowsDefaultCellStyle.SelectionForeColor = Cafe;
    }

    /// <summary>Texto con "..." si no cabe en el espacio disponible.</summary>
    public static string Recortar(string texto, int max)
    {
        if (string.IsNullOrEmpty(texto) || texto.Length <= max) return texto ?? "";
        return texto.Substring(0, max - 1).TrimEnd() + "…";
    }
}

/// <summary>
/// Muestra un dibujo de pixel art siempre con cuadros del mismo tamaño
/// (sin suavizado) y centrado dentro del control.
/// </summary>
public class PixelBox : Control
{
    private Image _imagen;
    private bool _suavizado;
    public bool MostrarBorde { get; set; } = true;
    public string TextoVacio { get; set; } = "Sin imagen";

    public Image Imagen
    {
        get => _imagen;
        set { _imagen = value; Invalidate(); }
    }

    /// <summary>true para fotos normales (se escalan suave); false para pixel art (cuadros nítidos).</summary>
    public bool Suavizado
    {
        get => _suavizado;
        set { _suavizado = value; Invalidate(); }
    }

    public PixelBox()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        BackColor = Tema.Crema;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.Clear(BackColor);

        if (_imagen == null)
        {
            TextRenderer.DrawText(g, TextoVacio, Tema.FNormal, ClientRectangle, Tema.CafeClaro,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }
        else
        {
            int lado = Math.Min(Width, Height);
            int d = lado;
            if (!_suavizado)
                d = Math.Max(64, lado / 64 * 64);     // múltiplos de 64: los cuadros salen del mismo tamaño
            d = Math.Min(d, lado);
            int x = (Width - d) / 2, y = (Height - d) / 2;
            g.InterpolationMode = _suavizado ? InterpolationMode.HighQualityBicubic : InterpolationMode.NearestNeighbor;
            g.PixelOffsetMode = _suavizado ? PixelOffsetMode.HighQuality : PixelOffsetMode.Half;
            g.DrawImage(_imagen, new Rectangle(x, y, d, d), 0, 0, _imagen.Width, _imagen.Height, GraphicsUnit.Pixel);
        }

        if (MostrarBorde)
            using (var pen = new Pen(Tema.Gris))
                g.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
    }
}

/// <summary>Ventana de captura: título, campos en columna y botones Guardar / Cancelar siempre visibles.</summary>
public class DialogoBase : Form
{
    protected readonly TableLayoutPanel Campos;
    protected readonly FlowLayoutPanel Cuerpo;
    protected readonly FlowLayoutPanel Botonera;
    protected readonly Button BtnGuardar;
    protected readonly Button BtnCancelar;
    private readonly ErrorProvider _avisos = new ErrorProvider { BlinkStyle = ErrorBlinkStyle.NeverBlink };
    private readonly List<(Control Campo, Func<string> Regla)> _reglas = new List<(Control, Func<string>)>();

    public DialogoBase(string titulo, string encabezado, int anchoCampos = 320)
    {
        Text = titulo;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        AutoScaleMode = AutoScaleMode.None;
        BackColor = Tema.Fondo;
        ForeColor = Tema.Cafe;
        Font = Tema.FNormal;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        Padding = new Padding(Tema.Px(28));

        var raiz = new TableLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            RowCount = 3,
            Dock = DockStyle.Fill
        };

        var lbl = new Label
        {
            Text = encabezado,
            Font = Tema.FGrande,
            ForeColor = Tema.Cafe,
            AutoSize = true,
            Margin = new Padding(0, 0, 0, Tema.Px(18))
        };

        Campos = new TableLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 2,
            Margin = new Padding(0)
        };
        Campos.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        Campos.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, Tema.Px(anchoCampos)));

        Cuerpo = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            WrapContents = false,
            FlowDirection = FlowDirection.LeftToRight,
            Margin = new Padding(0)
        };
        Cuerpo.Controls.Add(Campos);

        Botonera = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            WrapContents = false,
            FlowDirection = FlowDirection.RightToLeft,
            Anchor = AnchorStyles.Right,
            Margin = new Padding(0, Tema.Px(20), 0, 0)
        };

        BtnGuardar = Tema.Boton("Guardar");
        BtnCancelar = Tema.Boton("Cancelar", Estilo.Secundario);
        BtnGuardar.Margin = new Padding(0);
        BtnCancelar.Margin = new Padding(0, 0, Tema.Px(10), 0);
        BtnCancelar.DialogResult = DialogResult.Cancel;
        Botonera.Controls.Add(BtnGuardar);   // en RightToLeft el primero queda a la derecha
        Botonera.Controls.Add(BtnCancelar);

        raiz.Controls.Add(lbl, 0, 0);
        raiz.Controls.Add(Cuerpo, 0, 1);
        raiz.Controls.Add(Botonera, 0, 2);
        Controls.Add(raiz);

        AcceptButton = BtnGuardar;
        CancelButton = BtnCancelar;
        BtnGuardar.Click += (s, e) =>
        {
            if (Guardar())
            {
                DialogResult = DialogResult.OK;
                Close();
            }
        };
    }

    /// <summary>Se llama al pulsar Guardar. Devuelve true si todo salió bien y se puede cerrar.</summary>
    protected virtual bool Guardar() => true;

    protected void AgregarCampo(string etiqueta, Control control)
    {
        int fila = Campos.RowCount;
        Campos.RowCount = fila + 1;
        Campos.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var l = Tema.Etiqueta(etiqueta);
        l.Anchor = AnchorStyles.Left;
        l.Margin = new Padding(0, Tema.Px(8), Tema.Px(20), Tema.Px(8));

        control.Dock = DockStyle.Fill;
        control.Margin = new Padding(0, Tema.Px(6), 0, Tema.Px(6));

        Campos.Controls.Add(l, 0, fila);
        Campos.Controls.Add(control, 1, fila);
    }

    /// <summary>
    /// Registra una regla para un campo: devuelve null si está bien o el mensaje si hay un problema.
    /// El aviso (ícono rojo) aparece al salir del campo y se quita solo cuando se corrige.
    /// </summary>
    protected void Regla(Control campo, Func<string> regla)
    {
        _avisos.ContainerControl = this;
        _avisos.SetIconAlignment(campo, ErrorIconAlignment.MiddleRight);
        _avisos.SetIconPadding(campo, -Tema.Px(26));
        _reglas.Add((campo, regla));
        campo.Validating += (s, e) => _avisos.SetError(campo, regla() ?? "");
        campo.TextChanged += (s, e) =>
        {
            if (_avisos.GetError(campo).Length > 0) _avisos.SetError(campo, regla() ?? "");
        };
    }

    /// <summary>Revisa todas las reglas; si alguna falla muestra el mensaje y deja el cursor en ese campo.</summary>
    protected bool ValidarTodo()
    {
        Control primero = null;
        string mensaje = null;
        foreach (var (campo, regla) in _reglas)
        {
            string m = regla();
            _avisos.SetError(campo, m ?? "");
            if (m != null && primero == null) { primero = campo; mensaje = m; }
        }
        if (primero == null) return true;
        Aviso(mensaje);
        primero.Focus();
        return false;
    }

    protected void Aviso(string mensaje) =>
        MessageBox.Show(this, mensaje, "FloreHome", MessageBoxButtons.OK, MessageBoxIcon.Warning);

    protected void ErrorBD(string mensaje, Exception ex) =>
        MessageBox.Show(this, mensaje + "\n\n" + ex.Message, "FloreHome", MessageBoxButtons.OK, MessageBoxIcon.Error);
}
