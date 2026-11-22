using System.Data;
using System.Globalization;
using FloreHome.Core;
using FloreHome.Services;
using FloreHome.Ui;
using MySqlConnector;

namespace FloreHome.Forms;

/// <summary>
/// Historial de facturas. Las facturas no se borran: se anulan (queda el registro y el stock vuelve).
/// El administrador ve todas; cada empleado ve solo las que hizo.
/// </summary>
public sealed class FacturasView : VistaLista
{
    private readonly DateTimePicker _desde = new DateTimePicker();
    private readonly DateTimePicker _hasta = new DateTimePicker();
    private readonly Label _resumen = new Label();
    private readonly DataGridView _detalle = new DataGridView();
    private static readonly Font FTachado = new Font(Tema.FNormal, FontStyle.Strikeout);
    private bool _cargando = true;

    protected override string Titulo => "Facturas";
    protected override string[] ColumnasBusqueda => new[] { "Id", "Cliente", "Vendedor", "Estado", "Pago", "Tipo" };

    public FacturasView()
    {
        ConfigurarFecha(_desde, DateTime.Today.AddDays(-30));
        ConfigurarFecha(_hasta, DateTime.Today);

        var lblDesde = Tema.Etiqueta("Desde");
        lblDesde.Margin = new Padding(0, Tema.Px(10), Tema.Px(8), 0);
        var lblHasta = Tema.Etiqueta("Hasta");
        lblHasta.Margin = new Padding(Tema.Px(16), Tema.Px(10), Tema.Px(8), 0);
        var espacio = new Label { AutoSize = false, Width = Tema.Px(14), Height = Tema.Px(10), Margin = new Padding(0) };
        Botones.Controls.Add(lblDesde);
        Botones.Controls.Add(_desde);
        Botones.Controls.Add(lblHasta);
        Botones.Controls.Add(_hasta);
        Botones.Controls.Add(espacio);

        AgregarBoton("Comprobante PDF", Estilo.Primario, (s, e) => Comprobante());
        if (Sesion.EsAdmin) AgregarBoton("Anular factura", Estilo.Peligro, (s, e) => Anular());
        AgregarExportaciones();

        // Resumen y detalle debajo de la tabla
        _resumen.AutoSize = true;
        _resumen.Font = Tema.FNegrita;
        _resumen.ForeColor = Tema.Cafe;
        _resumen.Dock = DockStyle.Top;
        _resumen.Padding = new Padding(0, 0, 0, Tema.Px(8));

        Tema.EstilizarGrid(_detalle);
        _detalle.Dock = DockStyle.Fill;
        _detalle.ColumnHeadersHeight = Tema.Px(36);
        _detalle.RowTemplate.Height = Tema.Px(32);
        AgregarColumnaDetalle("Descripcion", "Producto", 50, null, false, 180);
        AgregarColumnaDetalle("cantidad", "Cant.", 10, null, true, 60);
        AgregarColumnaDetalle("Precio", "Precio", 20, "$#,##0.00", true, 90);
        AgregarColumnaDetalle("Total", "Subtotal", 20, "$#,##0.00", true, 90);

        PanelExtra.Height = Tema.Px(240);
        PanelExtra.Visible = true;
        PanelExtra.Controls.Add(_detalle);
        PanelExtra.Controls.Add(_resumen);

        _desde.ValueChanged += (s, e) => { if (!_cargando) Recargar(); };
        _hasta.ValueChanged += (s, e) => { if (!_cargando) Recargar(); };
        Grid.SelectionChanged += (s, e) => CargarDetalle();
        Grid.CellFormatting += MarcarAnuladas;
        TxtBuscar.TextChanged += (s, e) => ActualizarResumen();
    }

    private static void ConfigurarFecha(DateTimePicker dtp, DateTime valor)
    {
        dtp.Format = DateTimePickerFormat.Custom;
        dtp.CustomFormat = "dd/MM/yyyy";
        dtp.Font = Tema.FNormal;
        dtp.Width = Tema.Px(130);
        dtp.Margin = new Padding(0, Tema.Px(6), 0, 0);
        dtp.Value = valor;
    }

    private void AgregarColumnaDetalle(string prop, string encabezado, float peso, string formato, bool derecha, int minimo)
    {
        var c = new DataGridViewTextBoxColumn
        {
            DataPropertyName = prop,
            Name = prop,
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
        _detalle.Columns.Add(c);
    }

    protected override DataTable Consultar()
    {
        DateTime d = _desde.Value.Date;
        DateTime h = _hasta.Value.Date.AddDays(1);
        int usuario = Sesion.EsAdmin ? 0 : Sesion.IdUsuario;     // 0 = todas
        return Db.Tabla(
            "SELECT f.Idfactura AS Id, f.Fecha, f.ClienteNombre AS Cliente, " +
            "COALESCE(u.NombreCompleto, '(usuario eliminado)') AS Vendedor, f.MetodoPago AS Pago, " +
            "f.TipoDocumento AS Tipo, f.Descuento, f.Total, f.Estado " +
            "FROM Factura f LEFT JOIN Usuario u ON u.IdUsuario = f.IdUsuario " +
            "WHERE f.Fecha >= @d AND f.Fecha < @h AND (@u = 0 OR f.IdUsuario = @u) ORDER BY f.Fecha DESC",
            ("@d", d), ("@h", h), ("@u", usuario));
    }

    protected override void CrearColumnas()
    {
        Col("Id", "N°", 6, minimo: 64);
        Col("Fecha", "Fecha", 13, "dd/MM/yyyy HH:mm", minimo: 135);
        Col("Cliente", "Cliente", 17, minimo: 120);
        Col("Vendedor", "Atendió", 14, minimo: 110);
        Col("Tipo", "Tipo", 12, minimo: 120);
        Col("Pago", "Pago", 9, minimo: 90);
        Col("Descuento", "Descuento", 8, "$#,##0.00", true, 85);
        Col("Total", "Total", 9, "$#,##0.00", true, 85);
        Col("Estado", "Estado", 10, minimo: 90);
    }

    private void MarcarAnuladas(object sender, DataGridViewCellFormattingEventArgs e)
    {
        if (e.RowIndex < 0) return;
        var drv = Grid.Rows[e.RowIndex].DataBoundItem as DataRowView;
        if (drv == null || Convert.ToString(drv["Estado"]) != "Anulada") return;
        e.CellStyle.ForeColor = Tema.Peligro;
        e.CellStyle.Font = FTachado;
    }

    protected override void AlRecargar()
    {
        _cargando = false;
        ActualizarResumen();
        CargarDetalle();
    }

    private void ActualizarResumen()
    {
        decimal suma = 0;
        int emitidas = 0, anuladas = 0;
        foreach (DataGridViewRow fila in Grid.Rows)
        {
            var drv = fila.DataBoundItem as DataRowView;
            if (drv == null) continue;
            if (Convert.ToString(drv["Estado"]) == "Anulada") { anuladas++; continue; }
            suma += Convert.ToDecimal(drv["Total"]);
            emitidas++;
        }
        _resumen.Text = "Facturas: " + emitidas + "     Anuladas: " + anuladas +
                        "     Total vendido: $" + suma.ToString("N2", CultureInfo.InvariantCulture);
    }

    private void CargarDetalle()
    {
        var f = Seleccion();
        if (f == null) { _detalle.DataSource = null; return; }
        try
        {
            _detalle.DataSource = Db.Tabla(
                "SELECT p.Descripcion, d.cantidad, d.Precio, d.Total FROM Detallefactura d " +
                "JOIN Producto p ON p.Idproducto = d.idproducto WHERE d.idfactura = @id ORDER BY d.Iddetalle",
                ("@id", Convert.ToInt32(f["Id"])));
        }
        catch { _detalle.DataSource = null; }
    }

    private void Comprobante()
    {
        var f = Seleccion();
        if (f == null) { Avisar("Selecciona una factura de la lista."); return; }
        int id = Convert.ToInt32(f["Id"]);
        using var dlg = new SaveFileDialog { Filter = "PDF (*.pdf)|*.pdf", FileName = "Comprobante_" + id.ToString("D6") + ".pdf" };
        if (dlg.ShowDialog(FindForm()) != DialogResult.OK) return;
        try
        {
            Exportador.Comprobante(id, dlg.FileName);
            if (Confirmar("Comprobante guardado.\n¿Quieres abrirlo ahora?")) Abrir(dlg.FileName);
        }
        catch (Exception ex) { ErrorMsg("No se pudo crear el comprobante. Si lo tienes abierto, ciérralo e inténtalo de nuevo.", ex); }
    }

    private void Anular()
    {
        var f = Seleccion();
        if (f == null) { Avisar("Selecciona una factura de la lista."); return; }
        int id = Convert.ToInt32(f["Id"]);
        if (Convert.ToString(f["Estado"]) == "Anulada") { Avisar("Esa factura ya está anulada."); return; }

        using var dlg = new AnularFacturaDialog(id);
        if (dlg.ShowDialog(FindForm()) != DialogResult.OK) return;
        if (!Confirmar("¿Anular la factura N° " + id.ToString("D6") + "?\nLos productos vuelven al inventario y la factura queda marcada como anulada.")) return;

        try
        {
            using var cn = Conexion.Crear();
            cn.Open();
            using var tx = cn.BeginTransaction();
            try
            {
                using (var cmd = new MySqlCommand(
                    "UPDATE Producto p JOIN Detallefactura d ON d.idproducto = p.Idproducto " +
                    "SET p.Cantidad = p.Cantidad + d.cantidad WHERE d.idfactura = @id", cn, tx))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.ExecuteNonQuery();
                }
                using (var cmd = new MySqlCommand(
                    "UPDATE Factura SET Estado = 'Anulada', FechaAnulacion = NOW(), MotivoAnulacion = @m, AnuladaPor = @u " +
                    "WHERE Idfactura = @id AND Estado = 'Emitida'", cn, tx))
                {
                    cmd.Parameters.AddWithValue("@m", dlg.Motivo);
                    cmd.Parameters.AddWithValue("@u", Sesion.IdUsuario);
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.ExecuteNonQuery();
                }
                tx.Commit();
            }
            catch
            {
                tx.Rollback();
                throw;
            }
            Alertas.RevisarProductos(null);           // el stock regresó: se limpian las alertas que ya no aplican
            Recargar();
        }
        catch (Exception ex) { ErrorMsg("No se pudo anular la factura.", ex); }
    }
}
