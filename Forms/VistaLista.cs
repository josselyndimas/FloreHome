using System.Data;
using System.Diagnostics;
using FloreHome.Core;
using FloreHome.Services;
using FloreHome.Ui;

namespace FloreHome.Forms;

/// <summary>
/// Base de las pantallas de lista (Productos, Clientes, Usuarios, Facturas):
/// título, botones, buscador y tabla. Los botones se acomodan solos si falta espacio.
/// </summary>
public abstract class VistaLista : UserControl
{
    protected readonly DataGridView Grid = new DataGridView();
    protected readonly FlowLayoutPanel Botones = new FlowLayoutPanel();
    protected readonly TextBox TxtBuscar = Tema.Caja();
    protected readonly Panel PanelExtra = new Panel();
    protected DataView Vista;
    private bool _columnasListas;
    private FlowLayoutPanel _panelBuscar;

    protected abstract string Titulo { get; }
    protected abstract DataTable Consultar();
    protected abstract void CrearColumnas();
    protected virtual string[] ColumnasBusqueda => Array.Empty<string>();
    protected virtual void AlRecargar() { }

    protected VistaLista()
    {
        Dock = DockStyle.Fill;
        BackColor = Tema.Fondo;
        ForeColor = Tema.Cafe;
        Font = Tema.FNormal;
        Padding = new Padding(Tema.Px(28));

        // Diseño con acoplamientos (Dock): título arriba, botones debajo, tabla al centro y
        // un panel opcional abajo. Así nada se sale de la ventana ni se corta.
        var lblTitulo = new Label
        {
            Text = Titulo,
            Font = Tema.FTitulo,
            ForeColor = Tema.Cafe,
            AutoSize = true,
            Dock = DockStyle.Top,
            Padding = new Padding(0, 0, 0, Tema.Px(12))
        };

        Botones.AutoSize = true;
        Botones.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        Botones.Dock = DockStyle.Top;
        Botones.WrapContents = true;
        Botones.FlowDirection = FlowDirection.LeftToRight;
        Botones.Padding = new Padding(0, 0, 0, Tema.Px(6));

        var panelBuscar = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            WrapContents = false,
            FlowDirection = FlowDirection.LeftToRight,
            Margin = new Padding(Tema.Px(16), 0, 0, 0)
        };
        var lblBuscar = Tema.Etiqueta("Buscar");
        lblBuscar.Margin = new Padding(0, Tema.Px(10), Tema.Px(10), 0);
        TxtBuscar.Width = Tema.Px(240);
        TxtBuscar.Margin = new Padding(0, Tema.Px(6), 0, 0);
        panelBuscar.Controls.Add(lblBuscar);
        panelBuscar.Controls.Add(TxtBuscar);
        _panelBuscar = panelBuscar;

        Tema.EstilizarGrid(Grid);
        Grid.Dock = DockStyle.Fill;

        PanelExtra.Dock = DockStyle.Bottom;
        PanelExtra.Height = 0;
        PanelExtra.Padding = new Padding(0, Tema.Px(12), 0, 0);
        PanelExtra.Visible = false;

        // Orden de acople: el último que se agrega queda más arriba
        Controls.Add(Grid);
        Controls.Add(PanelExtra);
        Controls.Add(Botones);
        Controls.Add(lblTitulo);

        TxtBuscar.TextChanged += (s, e) => Filtrar();
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        if (_panelBuscar != null && !Botones.Controls.Contains(_panelBuscar))
            Botones.Controls.Add(_panelBuscar);       // siempre al final de los botones
        Recargar();
    }

    // ------------------------------------------------------------------
    protected Button AgregarBoton(string texto, Estilo estilo, EventHandler alClic)
    {
        var b = Tema.Boton(texto, estilo);
        b.Click += alClic;
        Botones.Controls.Add(b);
        return b;
    }

    protected void AgregarExportaciones()
    {
        AgregarBoton("Exportar Excel", Estilo.Secundario, (s, e) => ExportarExcel());
        AgregarBoton("Exportar PDF", Estilo.Secundario, (s, e) => ExportarPdf());
    }

    protected DataGridViewTextBoxColumn Col(string propiedad, string encabezado, float peso,
        string formato = null, bool derecha = false, int minimo = 80)
    {
        var c = new DataGridViewTextBoxColumn
        {
            DataPropertyName = propiedad,
            Name = propiedad,
            HeaderText = encabezado,
            FillWeight = peso,
            MinimumWidth = Tema.Px(minimo)
        };
        if (formato != null) c.DefaultCellStyle.Format = formato;
        if (derecha)
        {
            c.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            c.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;
        }
        Grid.Columns.Add(c);
        return c;
    }

    protected void Recargar()
    {
        try
        {
            var dt = Consultar();
            if (!_columnasListas)
            {
                CrearColumnas();
                _columnasListas = true;
            }
            Vista = dt.DefaultView;
            Grid.DataSource = Vista;
            Filtrar();
            AlRecargar();
        }
        catch (Exception ex)
        {
            MessageBox.Show(FindForm(), "No se pudieron cargar los datos.\n\n" + ex.Message, "FloreHome",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private static string EscaparLike(string t) =>
        t.Replace("[", "[[]").Replace("%", "[%]").Replace("*", "[*]").Replace("'", "''");

    protected void Filtrar()
    {
        if (Vista == null) return;
        string t = TxtBuscar.Text.Trim();
        var cols = ColumnasBusqueda;
        if (t.Length == 0 || cols.Length == 0)
        {
            Vista.RowFilter = "";
            return;
        }
        string like = EscaparLike(t);
        Vista.RowFilter = string.Join(" OR ", cols.Select(c => "Convert([" + c + "], 'System.String') LIKE '%" + like + "%'"));
    }

    protected DataRow Seleccion()
    {
        if (Grid.CurrentRow == null) return null;
        var drv = Grid.CurrentRow.DataBoundItem as DataRowView;
        return drv == null ? null : drv.Row;
    }

    protected bool Confirmar(string mensaje) =>
        MessageBox.Show(FindForm(), mensaje, "FloreHome", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;

    protected void Avisar(string mensaje) =>
        MessageBox.Show(FindForm(), mensaje, "FloreHome", MessageBoxButtons.OK, MessageBoxIcon.Information);

    protected void ErrorMsg(string mensaje, Exception ex) =>
        MessageBox.Show(FindForm(), mensaje + "\n\n" + ex.Message, "FloreHome", MessageBoxButtons.OK, MessageBoxIcon.Error);

    // ------------------------------------------------------------------
    //  Exportar
    // ------------------------------------------------------------------
    protected virtual string NombreArchivo =>
        "FloreHome_" + Titulo.Replace(' ', '_') + "_" + DateTime.Now.ToString("yyyyMMdd");

    protected static void Abrir(string ruta)
    {
        try { Process.Start(new ProcessStartInfo(ruta) { UseShellExecute = true }); }
        catch { }
    }

    private void ExportarExcel()
    {
        using var dlg = new SaveFileDialog { Filter = "Excel (*.xlsx)|*.xlsx", FileName = NombreArchivo + ".xlsx" };
        if (dlg.ShowDialog(FindForm()) != DialogResult.OK) return;
        try
        {
            Exportador.Excel(Exportador.TablaDeGrid(Grid), Titulo, dlg.FileName);
            if (Confirmar("Archivo guardado.\n¿Quieres abrirlo ahora?")) Abrir(dlg.FileName);
        }
        catch (Exception ex) { ErrorMsg("No se pudo crear el archivo de Excel. Si lo tienes abierto, ciérralo e inténtalo de nuevo.", ex); }
    }

    private void ExportarPdf()
    {
        using var dlg = new SaveFileDialog { Filter = "PDF (*.pdf)|*.pdf", FileName = NombreArchivo + ".pdf" };
        if (dlg.ShowDialog(FindForm()) != DialogResult.OK) return;
        try
        {
            Exportador.Pdf(Exportador.TablaDeGrid(Grid), Titulo, dlg.FileName);
            if (Confirmar("Archivo guardado.\n¿Quieres abrirlo ahora?")) Abrir(dlg.FileName);
        }
        catch (Exception ex) { ErrorMsg("No se pudo crear el PDF. Si lo tienes abierto, ciérralo e inténtalo de nuevo.", ex); }
    }
}
