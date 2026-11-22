using System.Data;
using System.Drawing.Drawing2D;
using FloreHome.Core;

namespace FloreHome.Ui;

/// <summary>Campanita de la esquina superior derecha, con un círculo rojo que cuenta las notificaciones sin leer.</summary>
public class Campana : Control
{
    private static readonly Font FConteo = new Font("Segoe UI", 8f, FontStyle.Bold);
    private static readonly Color Rojo = Color.FromArgb(0xD9, 0x53, 0x4F);
    private int _conteo;
    private bool _encima;

    public int Conteo
    {
        get => _conteo;
        set { _conteo = value; Invalidate(); }
    }

    public Campana()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        BackColor = Tema.Fondo;
        Cursor = Cursors.Hand;
        Size = new Size(Tema.Px(48), Tema.Px(48));
    }

    protected override void OnMouseEnter(EventArgs e) { _encima = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { _encima = false; Invalidate(); base.OnMouseLeave(e); }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.Clear(BackColor);
        g.SmoothingMode = SmoothingMode.AntiAlias;

        float k = Math.Min(Width, Height) / 44f;
        float cx = Width / 2f, cy = Height / 2f + 2 * k;

        if (_encima)
            using (var fondo = new SolidBrush(Tema.Menta))
                g.FillEllipse(fondo, cx - 20 * k, cy - 22 * k, 40 * k, 40 * k);

        using (var pincel = new SolidBrush(Tema.Cafe))
        using (var cuerpo = new GraphicsPath())
        {
            // cabito
            g.FillEllipse(pincel, cx - 2 * k, cy - 14 * k, 4 * k, 4 * k);
            // cúpula + falda de la campana
            cuerpo.AddArc(cx - 9 * k, cy - 12 * k, 18 * k, 18 * k, 180, 180);
            cuerpo.AddLine(cx + 9 * k, cy - 3 * k, cx + 11 * k, cy + 5 * k);
            cuerpo.AddLine(cx + 11 * k, cy + 5 * k, cx + 14 * k, cy + 8 * k);
            cuerpo.AddLine(cx + 14 * k, cy + 8 * k, cx - 14 * k, cy + 8 * k);
            cuerpo.AddLine(cx - 14 * k, cy + 8 * k, cx - 11 * k, cy + 5 * k);
            cuerpo.CloseFigure();
            g.FillPath(pincel, cuerpo);
            // badajo
            g.FillEllipse(pincel, cx - 3.5f * k, cy + 9.5f * k, 7 * k, 4.5f * k);
        }

        if (_conteo > 0)
        {
            string texto = _conteo > 9 ? "9+" : _conteo.ToString();
            float d = 19 * k;
            var rect = new RectangleF(Width - d - 3 * k, 3 * k, d, d);
            using (var rojo = new SolidBrush(Rojo))
                g.FillEllipse(rojo, rect);
            TextRenderer.DrawText(g, texto, FConteo, Rectangle.Round(rect), Color.White,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
        }
    }
}

/// <summary>
/// Una notificación de la lista. El check para marcarla como leída solo aparece
/// cuando se pasa el cursor sobre una notificación sin leer.
/// </summary>
public sealed class ItemNotificacion : Control
{
    private static readonly Color Ambar = Color.FromArgb(0xC9, 0x95, 0x4A);
    private static readonly Color Rojo = Color.FromArgb(0xD9, 0x53, 0x4F);
    private static readonly Color SinLeer = Color.FromArgb(0xEE, 0xF7, 0xF9);

    private readonly int _id;
    private readonly string _tipo;
    private readonly string _mensaje;
    private readonly DateTime _fecha;
    private readonly bool _leida;
    private bool _encima;

    public event Action<int> Marcada;

    public ItemNotificacion(int id, string tipo, string mensaje, DateTime fecha, bool leida)
    {
        _id = id;
        _tipo = tipo;
        _mensaje = mensaje;
        _fecha = fecha;
        _leida = leida;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        Height = Tema.Px(86);
        Margin = new Padding(0);
    }

    private Rectangle RectCheck => new Rectangle(Width - Tema.Px(56), (Height - Tema.Px(36)) / 2, Tema.Px(36), Tema.Px(36));

    protected override void OnMouseEnter(EventArgs e) { _encima = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { _encima = false; Cursor = Cursors.Default; Invalidate(); base.OnMouseLeave(e); }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        Cursor = (!_leida && RectCheck.Contains(e.Location)) ? Cursors.Hand : Cursors.Default;
        base.OnMouseMove(e);
    }

    protected override void OnMouseClick(MouseEventArgs e)
    {
        if (!_leida && RectCheck.Contains(e.Location))
        {
            var h = Marcada;
            if (h != null) h(_id);
        }
        base.OnMouseClick(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        Color fondo = _leida ? Color.White : (_encima ? Tema.Menta : SinLeer);
        g.Clear(fondo);
        g.SmoothingMode = SmoothingMode.AntiAlias;

        // punto de color: sin leer = rojo (agotado) o ámbar (stock bajo)
        if (!_leida)
        {
            using (var punto = new SolidBrush(_tipo == "Agotado" ? Rojo : Ambar))
                g.FillEllipse(punto, Tema.Px(14), Tema.Px(16), Tema.Px(11), Tema.Px(11));
        }

        int x = Tema.Px(36);
        int ancho = Width - x - Tema.Px(66);
        var rectMensaje = new Rectangle(x, Tema.Px(10), ancho, Tema.Px(46));
        TextRenderer.DrawText(g, _mensaje, _leida ? Tema.FNormal : Tema.FNegrita, rectMensaje,
            _leida ? Tema.CafeClaro : Tema.Cafe,
            TextFormatFlags.Left | TextFormatFlags.Top | TextFormatFlags.WordBreak | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);

        var rectFecha = new Rectangle(x, Tema.Px(58), ancho, Tema.Px(22));
        TextRenderer.DrawText(g, _fecha.ToString("dd/MM/yyyy  HH:mm"), Tema.FNormal, rectFecha, Tema.CafeClaro,
            TextFormatFlags.Left | TextFormatFlags.Top | TextFormatFlags.NoPadding);

        // check para marcar como leída (solo con el cursor encima)
        if (!_leida && _encima)
        {
            var c = RectCheck;
            using (var circulo = new SolidBrush(Tema.Cafe))
                g.FillEllipse(circulo, c);
            using (var lapiz = new Pen(Tema.Crema, Math.Max(2f, Tema.Px(3))) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round })
            {
                var puntos = new[]
                {
                    new PointF(c.X + c.Width * 0.28f, c.Y + c.Height * 0.52f),
                    new PointF(c.X + c.Width * 0.44f, c.Y + c.Height * 0.68f),
                    new PointF(c.X + c.Width * 0.74f, c.Y + c.Height * 0.34f)
                };
                g.DrawLines(lapiz, puntos);
            }
        }

        using (var linea = new Pen(Tema.Menta))
            g.DrawLine(linea, 0, Height - 1, Width, Height - 1);
    }
}

/// <summary>Lista desplegable de notificaciones que se abre debajo de la campanita.</summary>
public sealed class NotificacionesForm : Form
{
    private readonly FlowLayoutPanel _lista = new FlowLayoutPanel();

    public NotificacionesForm()
    {
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        AutoScaleMode = AutoScaleMode.None;
        BackColor = Color.White;
        Font = Tema.FNormal;
        Size = new Size(Tema.Px(450), Tema.Px(540));
        Padding = new Padding(1);

        var cabecera = new Panel { Dock = DockStyle.Top, Height = Tema.Px(64), BackColor = Tema.Fondo, Padding = new Padding(Tema.Px(16), 0, Tema.Px(12), 0) };
        var titulo = new Label
        {
            Text = "Notificaciones",
            Font = Tema.FPrecio,
            ForeColor = Tema.Cafe,
            AutoSize = true,
            Dock = DockStyle.Left,
            TextAlign = ContentAlignment.MiddleLeft
        };
        var marcarTodas = Tema.Boton("Marcar todas como leídas", Estilo.Secundario);
        marcarTodas.Dock = DockStyle.Right;
        marcarTodas.Margin = new Padding(0);
        marcarTodas.MinimumSize = new Size(Tema.Px(90), Tema.Px(38));
        marcarTodas.Click += (s, e) =>
        {
            try { Alertas.MarcarTodas(Sesion.IdUsuario); } catch { }
            Cargar();
        };
        var contenedorBoton = new Panel { Dock = DockStyle.Right, Width = Tema.Px(250), Padding = new Padding(0, Tema.Px(12), 0, Tema.Px(12)) };
        contenedorBoton.Controls.Add(marcarTodas);
        cabecera.Controls.Add(contenedorBoton);
        cabecera.Controls.Add(titulo);

        _lista.Dock = DockStyle.Fill;
        _lista.FlowDirection = FlowDirection.TopDown;
        _lista.WrapContents = false;
        _lista.AutoScroll = true;
        _lista.BackColor = Color.White;
        _lista.Padding = new Padding(0);

        Controls.Add(_lista);
        Controls.Add(cabecera);

        Paint += (s, e) =>
        {
            using var pen = new Pen(Tema.Gris);
            e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
        };
        Deactivate += (s, e) => Close();
        Shown += (s, e) => Cargar();
    }

    private void Cargar()
    {
        _lista.SuspendLayout();
        foreach (Control c in _lista.Controls.Cast<Control>().ToList()) c.Dispose();

        int ancho = Math.Max(100, _lista.ClientSize.Width - SystemInformation.VerticalScrollBarWidth - 2);
        try
        {
            DataTable dt = Alertas.Listar(Sesion.IdUsuario);
            if (dt.Rows.Count == 0)
            {
                _lista.Controls.Add(new Label
                {
                    Text = "No tienes notificaciones.",
                    AutoSize = false,
                    Width = ancho,
                    Height = Tema.Px(90),
                    TextAlign = ContentAlignment.MiddleCenter,
                    Font = Tema.FNormal,
                    ForeColor = Tema.CafeClaro,
                    Margin = new Padding(0)
                });
            }
            foreach (DataRow r in dt.Rows)
            {
                var item = new ItemNotificacion(
                    Convert.ToInt32(r["Id"]),
                    Convert.ToString(r["Tipo"]),
                    Convert.ToString(r["Mensaje"]),
                    Convert.ToDateTime(r["Fecha"]),
                    Convert.ToInt32(r["Leida"]) != 0)
                { Width = ancho };
                item.Marcada += id =>
                {
                    try { Alertas.Marcar(id, Sesion.IdUsuario); } catch { }
                    Cargar();
                };
                _lista.Controls.Add(item);
            }
        }
        catch
        {
            _lista.Controls.Add(new Label
            {
                Text = "No se pudieron cargar las notificaciones.",
                AutoSize = false,
                Width = ancho,
                Height = Tema.Px(90),
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Tema.CafeClaro,
                Margin = new Padding(0)
            });
        }
        _lista.ResumeLayout();
    }
}
