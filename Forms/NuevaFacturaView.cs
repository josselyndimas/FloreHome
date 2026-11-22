using System.Data;
using System.Globalization;
using FloreHome.Core;
using FloreHome.Imaging;
using FloreHome.Services;
using FloreHome.Ui;
using MySqlConnector;

namespace FloreHome.Forms;

/// <summary>Facturación: un menú con tarjetas de productos y el carrito a la derecha.</summary>
public sealed class NuevaFacturaView : UserControl
{
    private readonly FlowLayoutPanel _tarjetas = new FlowLayoutPanel();
    private readonly TextBox _buscar = Tema.Caja();
    private readonly ComboBox _categoria = Tema.Combo(MarcaCategoria);
    private readonly Label _lblCliente = new Label();
    private readonly CheckBox _chkFrecuente = new CheckBox();
    private readonly ComboBox _pago = Tema.Combo(Fiscal.MetodosPago);
    private readonly DataGridView _carrito = new DataGridView();
    private readonly DataTable _lineas = new DataTable();
    private readonly RadioButton _rbConsumidor = new RadioButton();
    private readonly RadioButton _rbCredito = new RadioButton();
    private Label _etiSuma;
    private readonly Label _lblSuma = new Label();
    private readonly Label _lblDescuento = new Label();
    private readonly Label _lblIva = new Label();
    private readonly Label _lblTotal = new Label();
    private readonly List<Control> _filasOpcionales = new List<Control>();
    private readonly List<Image> _imagenes = new List<Image>();
    private DataTable _productos = new DataTable();

    private const string MarcaCategoria = "Elige una categoría";
    private const string ClienteGeneral = "Sin cliente registrado";

    private int _idCliente;
    private string _nombreCliente = ClienteGeneral;

    private bool EsCreditoFiscal => _rbCredito.Checked;

    public NuevaFacturaView()
    {
        Dock = DockStyle.Fill;
        BackColor = Tema.Fondo;
        ForeColor = Tema.Cafe;
        Font = Tema.FNormal;
        Padding = new Padding(Tema.Px(28));

        _lineas.Columns.Add("Id", typeof(int));
        _lineas.Columns.Add("Producto", typeof(string));
        _lineas.Columns.Add("Cant", typeof(int));
        _lineas.Columns.Add("Precio", typeof(decimal));
        _lineas.Columns.Add("Subtotal", typeof(decimal));
        _lineas.Columns.Add("Stock", typeof(int));

        var menu = ConstruirMenu();
        var lateral = ConstruirCarrito();
        var separador = new Panel { Dock = DockStyle.Right, Width = Tema.Px(16) };

        // El último que se agrega se acopla primero: el carrito a la derecha, luego el espacio y el menú al centro
        Controls.Add(menu);
        Controls.Add(separador);
        Controls.Add(lateral);

        _buscar.TextChanged += (s, e) => ConstruirTarjetas();
        _categoria.SelectedIndexChanged += (s, e) => ConstruirTarjetas();
        ActualizarTotales();
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        CargarProductos();
    }

    // ------------------------------------------------------------------
    //  Construcción de la pantalla (todo acoplado con Dock para que nada quede fuera de la ventana)
    // ------------------------------------------------------------------
    private Control ConstruirMenu()
    {
        var panel = new Panel { Dock = DockStyle.Fill };

        var titulo = new Label
        {
            Text = "Nueva factura",
            Font = Tema.FTitulo,
            ForeColor = Tema.Cafe,
            AutoSize = true,
            Dock = DockStyle.Top,
            Padding = new Padding(0, 0, 0, Tema.Px(12))
        };

        var filtros = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            WrapContents = true,
            Dock = DockStyle.Top,
            Padding = new Padding(0, 0, 0, Tema.Px(10))
        };
        var lblBuscar = Tema.Etiqueta("Buscar (nombre o código)");
        lblBuscar.Margin = new Padding(0, Tema.Px(10), Tema.Px(10), 0);
        _buscar.Width = Tema.Px(220);
        _buscar.Margin = new Padding(0, Tema.Px(6), Tema.Px(24), 0);
        var lblCat = Tema.Etiqueta("Categoría");
        lblCat.Margin = new Padding(0, Tema.Px(10), Tema.Px(10), 0);
        _categoria.Width = Tema.Px(160);
        _categoria.Margin = new Padding(0, Tema.Px(6), 0, 0);
        filtros.Controls.Add(lblBuscar);
        filtros.Controls.Add(_buscar);
        filtros.Controls.Add(lblCat);
        filtros.Controls.Add(_categoria);

        _tarjetas.Dock = DockStyle.Fill;
        _tarjetas.AutoScroll = true;
        _tarjetas.WrapContents = true;
        _tarjetas.FlowDirection = FlowDirection.LeftToRight;
        _tarjetas.BackColor = Tema.Fondo;

        panel.Controls.Add(_tarjetas);
        panel.Controls.Add(filtros);
        panel.Controls.Add(titulo);
        return panel;
    }

    private Control ConstruirCarrito()
    {
        var lateral = new Panel
        {
            Dock = DockStyle.Right,
            Width = Tema.Px(450),
            BackColor = Color.White,
            Padding = new Padding(Tema.Px(18))
        };
        lateral.Paint += (s, e) =>
        {
            using var pen = new Pen(Tema.Gris);
            e.Graphics.DrawRectangle(pen, 0, 0, lateral.Width - 1, lateral.Height - 1);
        };

        // ---- Parte de arriba: tipo de factura y cliente ----
        var lblTipo = Tema.Etiqueta("Tipo de factura", true);
        lblTipo.Dock = DockStyle.Top;
        lblTipo.Padding = new Padding(0, 0, 0, Tema.Px(4));

        var tipos = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            WrapContents = true,
            Dock = DockStyle.Top,
            Padding = new Padding(0, 0, 0, Tema.Px(10))
        };
        foreach (var rb in new[] { _rbConsumidor, _rbCredito })
        {
            rb.AutoSize = true;
            rb.Font = Tema.FPrecio;
            rb.ForeColor = Tema.Cafe;
            rb.Margin = new Padding(0, 0, Tema.Px(22), 0);
            rb.Cursor = Cursors.Hand;
            rb.CheckedChanged += (s, e) => ActualizarTotales();
        }
        _rbConsumidor.Text = Fiscal.ConsumidorFinal;
        _rbCredito.Text = Fiscal.CreditoFiscal;
        _rbConsumidor.Checked = true;
        tipos.Controls.Add(_rbConsumidor);
        tipos.Controls.Add(_rbCredito);

        var lblTituloCliente = Tema.Etiqueta("Cliente", true);
        lblTituloCliente.Dock = DockStyle.Top;
        lblTituloCliente.Padding = new Padding(0, 0, 0, Tema.Px(2));

        _lblCliente.Text = _nombreCliente;
        _lblCliente.Dock = DockStyle.Top;
        _lblCliente.AutoSize = false;
        _lblCliente.Height = Tema.Px(32);
        _lblCliente.AutoEllipsis = true;
        _lblCliente.TextAlign = ContentAlignment.MiddleLeft;
        _lblCliente.Font = Tema.FNormal;
        _lblCliente.ForeColor = Tema.Cafe;

        var accionesCliente = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            WrapContents = true,
            Dock = DockStyle.Top,
            Padding = new Padding(0, Tema.Px(2), 0, Tema.Px(4))
        };
        var btnBuscar = Tema.Boton("Buscar cliente", Estilo.Secundario);
        var btnNuevo = Tema.Boton("+ Nuevo", Estilo.Secundario);
        var btnFinal = Tema.Boton("Sin cliente", Estilo.Secundario);
        foreach (var b in new[] { btnBuscar, btnNuevo, btnFinal })
            b.MinimumSize = new Size(Tema.Px(90), Tema.Px(38));
        btnBuscar.Click += (s, e) => BuscarCliente();
        btnNuevo.Click += (s, e) => NuevoCliente();
        btnFinal.Click += (s, e) => SeleccionarCliente(0, ClienteGeneral);
        accionesCliente.Controls.Add(btnBuscar);
        accionesCliente.Controls.Add(btnNuevo);
        accionesCliente.Controls.Add(btnFinal);

        _chkFrecuente.Text = "Cliente frecuente: aplicar 10 % de descuento";
        _chkFrecuente.AutoSize = true;
        _chkFrecuente.Dock = DockStyle.Top;
        _chkFrecuente.Font = Tema.FNegrita;
        _chkFrecuente.ForeColor = Tema.Cafe;
        _chkFrecuente.Padding = new Padding(0, Tema.Px(2), 0, Tema.Px(6));
        _chkFrecuente.Visible = false;
        _chkFrecuente.CheckedChanged += (s, e) => ActualizarTotales();

        // ---- Carrito (centro) ----
        Tema.EstilizarGrid(_carrito);
        _carrito.Dock = DockStyle.Fill;
        _carrito.ColumnHeadersHeight = Tema.Px(38);
        _carrito.RowTemplate.Height = Tema.Px(36);
        _carrito.DefaultCellStyle.Padding = new Padding(Tema.Px(4), 0, Tema.Px(4), 0);
        _carrito.ColumnHeadersDefaultCellStyle.Padding = new Padding(Tema.Px(4), 0, Tema.Px(4), 0);
        AgregarColumna("Producto", "Producto", 44, null, false, 110);
        AgregarColumna("Cant", "Cant.", 14, null, true, 48);
        AgregarColumna("Precio", "Precio", 20, "$#,##0.00", true, 70);
        AgregarColumna("Subtotal", "Subtotal", 22, "$#,##0.00", true, 80);
        _carrito.DataSource = _lineas;

        // La cantidad se puede escribir directamente en la tabla (ej. 100)
        _carrito.ReadOnly = false;
        _carrito.SelectionMode = DataGridViewSelectionMode.CellSelect;
        _carrito.EditMode = DataGridViewEditMode.EditOnEnter;
        foreach (DataGridViewColumn c in _carrito.Columns)
            c.ReadOnly = c.Name != "Cant";
        _carrito.CellValidating += ValidarCantidad;
        _carrito.CellEndEdit += CantidadEditada;
        _carrito.EditingControlShowing += PrepararEdicion;
        _carrito.DataError += (s, e) => e.ThrowException = false;

        // ---- Parte de abajo: botones del carrito, pago, totales y guardar ----
        var botones = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            WrapContents = true,
            Dock = DockStyle.Bottom,
            Padding = new Padding(0, Tema.Px(10), 0, Tema.Px(2))
        };
        var menos = Tema.Boton("−", Estilo.Secundario);
        var mas = Tema.Boton("+", Estilo.Secundario);
        var quitar = Tema.Boton("Quitar", Estilo.Secundario);
        var vaciar = Tema.Boton("Vaciar", Estilo.Secundario);
        menos.MinimumSize = new Size(Tema.Px(48), Tema.Px(38));
        mas.MinimumSize = new Size(Tema.Px(48), Tema.Px(38));
        quitar.MinimumSize = new Size(Tema.Px(90), Tema.Px(38));
        vaciar.MinimumSize = new Size(Tema.Px(90), Tema.Px(38));
        menos.Click += (s, e) => CambiarCantidad(-1);
        mas.Click += (s, e) => CambiarCantidad(1);
        quitar.Click += (s, e) => QuitarLinea();
        vaciar.Click += (s, e) => { _lineas.Rows.Clear(); ActualizarTotales(); };
        botones.Controls.Add(menos);
        botones.Controls.Add(mas);
        botones.Controls.Add(quitar);
        botones.Controls.Add(vaciar);

        var filaPago = new TableLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 2,
            Dock = DockStyle.Bottom,
            Padding = new Padding(0, Tema.Px(4), 0, Tema.Px(8))
        };
        filaPago.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        filaPago.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        var lblPago = Tema.Etiqueta("Método de pago", true);
        lblPago.Anchor = AnchorStyles.Left;
        lblPago.Margin = new Padding(0, 0, Tema.Px(14), 0);
        _pago.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _pago.Margin = new Padding(0);
        filaPago.Controls.Add(lblPago, 0, 0);
        filaPago.Controls.Add(_pago, 1, 0);

        var totales = new TableLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 2,
            Dock = DockStyle.Bottom,
            Padding = new Padding(0, Tema.Px(4), 0, Tema.Px(12))
        };
        totales.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        totales.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        _etiSuma = FilaTotal(totales, 0, "Productos", _lblSuma, false, false);
        FilaTotal(totales, 1, "Descuento cliente frecuente", _lblDescuento, false, true);
        FilaTotal(totales, 2, "IVA (13%)", _lblIva, false, true);
        FilaTotal(totales, 3, "Total a pagar", _lblTotal, true, false);

        var guardar = Tema.Boton("Guardar factura", Estilo.Primario);
        guardar.Dock = DockStyle.Bottom;
        guardar.Height = Tema.Px(54);
        guardar.MinimumSize = new Size(0, Tema.Px(54));
        guardar.Click += (s, e) => GuardarFactura();

        // Orden de acople: el último agregado ocupa su borde primero
        lateral.Controls.Add(_carrito);
        lateral.Controls.Add(botones);
        lateral.Controls.Add(filaPago);
        lateral.Controls.Add(totales);
        lateral.Controls.Add(guardar);
        lateral.Controls.Add(_chkFrecuente);
        lateral.Controls.Add(accionesCliente);
        lateral.Controls.Add(_lblCliente);
        lateral.Controls.Add(lblTituloCliente);
        lateral.Controls.Add(tipos);
        lateral.Controls.Add(lblTipo);
        return lateral;
    }

    private void AgregarColumna(string prop, string encabezado, float peso, string formato, bool derecha, int minimo)
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
        _carrito.Columns.Add(c);
    }

    private Label FilaTotal(TableLayoutPanel t, int fila, string texto, Label valor, bool grande, bool opcional)
    {
        t.RowCount = fila + 1;
        t.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        var etiqueta = new Label
        {
            Text = texto,
            AutoSize = true,
            Font = grande ? Tema.FPrecio : Tema.FNormal,
            ForeColor = Tema.Cafe,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(0, Tema.Px(3), 0, Tema.Px(3))
        };
        valor.AutoSize = true;
        valor.Font = grande ? Tema.FPrecio : Tema.FNormal;
        valor.ForeColor = Tema.Cafe;
        valor.Anchor = AnchorStyles.Right;
        valor.Margin = new Padding(0, Tema.Px(3), 0, Tema.Px(3));
        t.Controls.Add(etiqueta, 0, fila);
        t.Controls.Add(valor, 1, fila);
        if (opcional)
        {
            _filasOpcionales.Add(etiqueta);
            _filasOpcionales.Add(valor);
        }
        return etiqueta;
    }

    // ------------------------------------------------------------------
    //  Datos
    // ------------------------------------------------------------------
    private void CargarProductos()
    {
        try
        {
            _productos = Db.Tabla("SELECT Idproducto, Codigo, Descripcion, Categoria, Cantidad, Precio, Imagen FROM Producto ORDER BY Descripcion");

            string actual = Convert.ToString(_categoria.SelectedItem);
            _categoria.Items.Clear();
            _categoria.Items.Add(MarcaCategoria);
            _categoria.Items.Add("Todas");
            foreach (var cat in _productos.AsEnumerable().Select(r => Convert.ToString(r["Categoria"])).Distinct().OrderBy(c => c))
                _categoria.Items.Add(cat);
            int pos = string.IsNullOrEmpty(actual) ? 0 : _categoria.Items.IndexOf(actual);
            _categoria.SelectedIndex = pos < 0 ? 0 : pos;

            ConstruirTarjetas();
        }
        catch (Exception ex)
        {
            MessageBox.Show(FindForm(), "No se pudieron cargar los productos.\n\n" + ex.Message, "FloreHome",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    // ------------------------------------------------------------------
    //  Cliente
    // ------------------------------------------------------------------
    private void BuscarCliente()
    {
        using var d = new BuscarClienteDialog();
        if (d.ShowDialog(FindForm()) == DialogResult.OK)
            SeleccionarCliente(d.IdCliente, d.NombreCliente);
    }

    private void NuevoCliente()
    {
        using var d = new ClienteDialog();
        if (d.ShowDialog(FindForm()) != DialogResult.OK) return;
        try
        {
            int id = Db.Contar("SELECT COALESCE(MAX(Idcliente), 0) FROM Cliente");
            var dt = Db.Tabla("SELECT Nombre FROM Cliente WHERE Idcliente = @id", ("@id", id));
            if (dt.Rows.Count > 0) SeleccionarCliente(id, Convert.ToString(dt.Rows[0]["Nombre"]));
        }
        catch { }
    }

    private void SeleccionarCliente(int id, string nombre)
    {
        _idCliente = id;
        _nombreCliente = nombre;
        _lblCliente.Text = nombre;

        // Cliente frecuente: cada 6 compras de más de $25, la siguiente lleva 10 % de descuento
        bool elegible = false;
        if (id > 0)
        {
            try
            {
                int compras = Db.Contar(
                    "SELECT COUNT(*) FROM Factura f WHERE f.IdCliente = @id AND f.Estado = 'Emitida' " +
                    "AND f.Total > @m AND f.Descuento = 0 " +
                    "AND f.Fecha > COALESCE((SELECT MAX(x.Fecha) FROM Factura x WHERE x.IdCliente = @id " +
                    "AND x.Estado = 'Emitida' AND x.Descuento > 0), '1970-01-01')",
                    ("@id", id), ("@m", Fiscal.MontoMinimoCompra));
                elegible = compras >= Fiscal.ComprasParaDescuento;
            }
            catch { }
        }
        _chkFrecuente.Checked = false;
        _chkFrecuente.Visible = elegible;
        ActualizarTotales();
    }

    // ------------------------------------------------------------------
    //  Tarjetas del menú
    // ------------------------------------------------------------------
    private void ConstruirTarjetas()
    {
        _tarjetas.SuspendLayout();
        foreach (Control c in _tarjetas.Controls.Cast<Control>().ToList()) c.Dispose();
        foreach (var img in _imagenes) img.Dispose();
        _imagenes.Clear();

        string texto = _buscar.Text.Trim().ToLowerInvariant();
        string cat = Convert.ToString(_categoria.SelectedItem);
        bool sinCategoria = string.IsNullOrEmpty(cat) || cat == MarcaCategoria;
        bool todas = sinCategoria || cat == "Todas";

        if (sinCategoria && texto.Length == 0)
        {
            _tarjetas.Controls.Add(new Label
            {
                Text = "Elige una categoría o escribe en el buscador\npara ver los productos.",
                AutoSize = true,
                Font = Tema.FPrecio,
                ForeColor = Tema.CafeClaro,
                Margin = new Padding(Tema.Px(6), Tema.Px(30), 0, 0)
            });
            _tarjetas.ResumeLayout();
            return;
        }

        foreach (DataRow r in _productos.Rows)
        {
            if (!todas && Convert.ToString(r["Categoria"]) != cat) continue;
            if (texto.Length > 0)
            {
                string nombre = Convert.ToString(r["Descripcion"]).ToLowerInvariant();
                string codigo = Convert.ToString(r["Codigo"]).ToLowerInvariant();
                if (!nombre.Contains(texto) && !codigo.Contains(texto)) continue;
            }
            _tarjetas.Controls.Add(CrearTarjeta(r));
        }
        _tarjetas.ResumeLayout();
    }

    private Control CrearTarjeta(DataRow r)
    {
        int id = Convert.ToInt32(r["Idproducto"]);
        string descripcion = Convert.ToString(r["Descripcion"]);
        int stock = Convert.ToInt32(r["Cantidad"]);
        decimal precio = Convert.ToDecimal(r["Precio"]);
        bool agotado = stock <= 0;

        var tarjeta = new Panel
        {
            Width = Tema.Px(176),
            Height = Tema.Px(258),
            BackColor = Color.White,
            Margin = new Padding(0, 0, Tema.Px(14), Tema.Px(14)),
            Padding = new Padding(Tema.Px(8)),
            Cursor = agotado ? Cursors.Default : Cursors.Hand
        };
        tarjeta.Paint += (s, e) =>
        {
            using var pen = new Pen(Tema.Gris);
            e.Graphics.DrawRectangle(pen, 0, 0, tarjeta.Width - 1, tarjeta.Height - 1);
        };

        var t = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4, Margin = new Padding(0) };
        t.RowStyles.Add(new RowStyle(SizeType.Absolute, Tema.Px(138)));
        t.RowStyles.Add(new RowStyle(SizeType.Absolute, Tema.Px(48)));
        t.RowStyles.Add(new RowStyle(SizeType.Absolute, Tema.Px(32)));
        t.RowStyles.Add(new RowStyle(SizeType.Absolute, Tema.Px(28)));

        var foto = new PixelBox { Dock = DockStyle.Fill, MostrarBorde = false, Margin = new Padding(0) };
        if (r["Imagen"] is byte[] datos && datos.Length > 0)
        {
            try
            {
                var img = PixelArt.DesdeBytes(datos);
                _imagenes.Add(img);
                foto.Suavizado = PixelArt.EsFoto(datos);
                foto.Imagen = img;
            }
            catch { }
        }

        var nombre = new Label
        {
            Text = Tema.Recortar(descripcion, 34),
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = Tema.FNegrita,
            ForeColor = Tema.Cafe,
            Margin = new Padding(0)
        };
        var lblPrecio = new Label
        {
            Text = Dinero(precio),
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = Tema.FPrecio,
            ForeColor = Tema.Cafe,
            Margin = new Padding(0)
        };
        var lblStock = new Label
        {
            Text = agotado ? "Agotado" : "Disponibles: " + stock,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = Tema.FNormal,
            ForeColor = agotado ? Tema.Peligro : Tema.CafeClaro,
            Margin = new Padding(0)
        };

        t.Controls.Add(foto, 0, 0);
        t.Controls.Add(nombre, 0, 1);
        t.Controls.Add(lblPrecio, 0, 2);
        t.Controls.Add(lblStock, 0, 3);
        tarjeta.Controls.Add(t);

        if (!agotado)
        {
            EventHandler alClic = (s, e) => AgregarAlCarrito(id, descripcion, precio, stock);
            tarjeta.Click += alClic;
            t.Click += alClic;
            foto.Click += alClic;
            nombre.Click += alClic;
            lblPrecio.Click += alClic;
            lblStock.Click += alClic;
            foto.Cursor = Cursors.Hand;
            nombre.Cursor = Cursors.Hand;
            lblPrecio.Cursor = Cursors.Hand;
            lblStock.Cursor = Cursors.Hand;
        }
        return tarjeta;
    }

    // ------------------------------------------------------------------
    //  Carrito
    // ------------------------------------------------------------------
    private static string Dinero(decimal v) => "$" + v.ToString("N2", CultureInfo.InvariantCulture);

    private void AgregarAlCarrito(int id, string descripcion, decimal precio, int stock)
    {
        foreach (DataRow l in _lineas.Rows)
        {
            if (Convert.ToInt32(l["Id"]) != id) continue;
            int cant = Convert.ToInt32(l["Cant"]);
            if (cant + 1 > stock)
            {
                MessageBox.Show(FindForm(), "Solo hay " + stock + " unidades disponibles de este producto.", "FloreHome",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            l["Cant"] = cant + 1;
            l["Subtotal"] = (cant + 1) * precio;
            ActualizarTotales();
            return;
        }
        _lineas.Rows.Add(id, descripcion, 1, precio, precio, stock);
        ActualizarTotales();
    }

    private DataRow LineaSeleccionada()
    {
        var drv = _carrito.CurrentRow == null ? null : _carrito.CurrentRow.DataBoundItem as DataRowView;
        return drv == null ? null : drv.Row;
    }

    private void CambiarCantidad(int delta)
    {
        _carrito.EndEdit();
        var l = LineaSeleccionada();
        if (l == null) return;
        int nueva = Convert.ToInt32(l["Cant"]) + delta;
        int stock = Convert.ToInt32(l["Stock"]);
        if (nueva <= 0) { _lineas.Rows.Remove(l); ActualizarTotales(); return; }
        if (nueva > stock)
        {
            MessageBox.Show(FindForm(), "Solo hay " + stock + " unidades disponibles de este producto.", "FloreHome",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        l["Cant"] = nueva;
        l["Subtotal"] = nueva * Convert.ToDecimal(l["Precio"]);
        ActualizarTotales();
    }

    private void QuitarLinea()
    {
        _carrito.EndEdit();
        var l = LineaSeleccionada();
        if (l == null) return;
        _lineas.Rows.Remove(l);
        ActualizarTotales();
    }

    private void ValidarCantidad(object sender, DataGridViewCellValidatingEventArgs e)
    {
        if (e.RowIndex < 0 || _carrito.Columns[e.ColumnIndex].Name != "Cant") return;
        var drv = _carrito.Rows[e.RowIndex].DataBoundItem as DataRowView;
        if (drv == null) return;

        string texto = Convert.ToString(e.FormattedValue) ?? "";
        int stock = Convert.ToInt32(drv["Stock"]);
        if (!int.TryParse(texto.Trim(), out int cantidad) || cantidad < 1)
        {
            MessageBox.Show(FindForm(), "Escribe una cantidad válida (1 o más).", "FloreHome",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            e.Cancel = true;
        }
        else if (cantidad > stock)
        {
            MessageBox.Show(FindForm(), "Solo hay " + stock + " unidades disponibles de este producto.", "FloreHome",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            e.Cancel = true;
        }
    }

    private void CantidadEditada(object sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || _carrito.Columns[e.ColumnIndex].Name != "Cant") return;
        var drv = _carrito.Rows[e.RowIndex].DataBoundItem as DataRowView;
        if (drv == null) return;
        drv["Subtotal"] = Convert.ToInt32(drv["Cant"]) * Convert.ToDecimal(drv["Precio"]);
        ActualizarTotales();
    }

    private void PrepararEdicion(object sender, DataGridViewEditingControlShowingEventArgs e)
    {
        if (_carrito.CurrentCell == null || _carrito.CurrentCell.OwningColumn.Name != "Cant") return;
        if (e.Control is TextBox tb)
        {
            tb.MaxLength = 5;
            tb.KeyPress -= SoloDigitos;
            tb.KeyPress += SoloDigitos;
            tb.BeginInvoke(new Action(tb.SelectAll));
        }
    }

    private static void SoloDigitos(object sender, KeyPressEventArgs e)
    {
        if (!char.IsControl(e.KeyChar) && !(e.KeyChar >= '0' && e.KeyChar <= '9')) e.Handled = true;
    }

    private void CalcularTotales(out decimal bruto, out decimal descuento, out decimal subtotal, out decimal iva, out decimal total)
    {
        bruto = 0;
        foreach (DataRow l in _lineas.Rows) bruto += Convert.ToDecimal(l["Subtotal"]);
        descuento = (_chkFrecuente.Visible && _chkFrecuente.Checked) ? Fiscal.Redondear(bruto * Fiscal.DescuentoFrecuente) : 0m;
        Fiscal.Calcular(bruto, descuento, out subtotal, out iva, out total);
    }

    private void ActualizarTotales()
    {
        if (_etiSuma == null) return;     // la pantalla todavía se está armando
        CalcularTotales(out decimal bruto, out decimal descuento, out decimal subtotal, out decimal iva, out decimal total);
        bool credito = EsCreditoFiscal;

        if (credito)
        {
            // Crédito fiscal: productos sin IVA, luego el IVA, y el total es la suma de los dos
            decimal netoBruto = Fiscal.SinIva(bruto);
            _etiSuma.Text = "Productos (sin IVA)";
            _lblSuma.Text = Dinero(netoBruto);
            _lblDescuento.Text = "-" + Dinero(netoBruto - subtotal);
            _lblIva.Text = Dinero(iva);
        }
        else
        {
            // Consumidor final: no se menciona el IVA
            _etiSuma.Text = "Productos";
            _lblSuma.Text = Dinero(bruto);
            _lblDescuento.Text = "-" + Dinero(descuento);
        }
        _lblTotal.Text = Dinero(total);

        bool verDescuento = descuento > 0;
        _filasOpcionales[0].Visible = verDescuento;
        _filasOpcionales[1].Visible = verDescuento;
        _filasOpcionales[2].Visible = credito;
        _filasOpcionales[3].Visible = credito;
    }

    // ------------------------------------------------------------------
    //  Guardar
    // ------------------------------------------------------------------
    private void GuardarFactura()
    {
        _carrito.EndEdit();
        if (_lineas.Rows.Count == 0)
        {
            MessageBox.Show(FindForm(), "Agrega al menos un producto a la factura.", "FloreHome",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (EsCreditoFiscal && _idCliente == 0)
        {
            MessageBox.Show(FindForm(), "El crédito fiscal necesita un cliente registrado.\nUsa \"Buscar cliente\" o \"+ Nuevo\".", "FloreHome",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        CalcularTotales(out decimal bruto, out decimal descuento, out decimal subtotal, out decimal iva, out decimal total);
        string tipo = EsCreditoFiscal ? Fiscal.CreditoFiscal : Fiscal.ConsumidorFinal;
        string metodo = Convert.ToString(_pago.SelectedItem) ?? "Efectivo";
        long idFactura = 0;

        try
        {
            using var cn = Conexion.Crear();
            cn.Open();
            using var tx = cn.BeginTransaction();
            try
            {
                // 1) Verificar existencias
                foreach (DataRow l in _lineas.Rows)
                {
                    using var cmd = new MySqlCommand("SELECT Cantidad FROM Producto WHERE Idproducto = @id FOR UPDATE", cn, tx);
                    cmd.Parameters.AddWithValue("@id", Convert.ToInt32(l["Id"]));
                    object r = cmd.ExecuteScalar();
                    int disponible = r == null || r == DBNull.Value ? 0 : Convert.ToInt32(r);
                    if (disponible < Convert.ToInt32(l["Cant"]))
                        throw new InvalidOperationException("Solo quedan " + disponible + " unidades de \"" + Convert.ToString(l["Producto"]) + "\".");
                }

                // 2) Encabezado
                using (var cmd = new MySqlCommand(
                    "INSERT INTO Factura (IdCliente, ClienteNombre, IdUsuario, TipoDocumento, MetodoPago, Subtotal, Descuento, IVA, Total) " +
                    "VALUES (@c, @n, @u, @td, @mp, @s, @d, @i, @t)", cn, tx))
                {
                    cmd.Parameters.AddWithValue("@c", _idCliente == 0 ? (object)DBNull.Value : _idCliente);
                    cmd.Parameters.AddWithValue("@n", _nombreCliente);
                    cmd.Parameters.AddWithValue("@u", Sesion.IdUsuario == 0 ? (object)DBNull.Value : Sesion.IdUsuario);
                    cmd.Parameters.AddWithValue("@td", tipo);
                    cmd.Parameters.AddWithValue("@mp", metodo);
                    cmd.Parameters.AddWithValue("@s", subtotal);
                    cmd.Parameters.AddWithValue("@d", descuento);
                    cmd.Parameters.AddWithValue("@i", iva);
                    cmd.Parameters.AddWithValue("@t", total);
                    cmd.ExecuteNonQuery();
                    idFactura = cmd.LastInsertedId;
                }

                // 3) Detalle y descuento de inventario
                foreach (DataRow l in _lineas.Rows)
                {
                    using (var cmd = new MySqlCommand(
                        "INSERT INTO Detallefactura (idfactura, idproducto, cantidad, Precio, Total) VALUES (@f, @p, @q, @pr, @t)", cn, tx))
                    {
                        cmd.Parameters.AddWithValue("@f", idFactura);
                        cmd.Parameters.AddWithValue("@p", Convert.ToInt32(l["Id"]));
                        cmd.Parameters.AddWithValue("@q", Convert.ToInt32(l["Cant"]));
                        cmd.Parameters.AddWithValue("@pr", Convert.ToDecimal(l["Precio"]));
                        cmd.Parameters.AddWithValue("@t", Convert.ToDecimal(l["Subtotal"]));
                        cmd.ExecuteNonQuery();
                    }
                    using (var cmd = new MySqlCommand("UPDATE Producto SET Cantidad = Cantidad - @q WHERE Idproducto = @p", cn, tx))
                    {
                        cmd.Parameters.AddWithValue("@q", Convert.ToInt32(l["Cant"]));
                        cmd.Parameters.AddWithValue("@p", Convert.ToInt32(l["Id"]));
                        cmd.ExecuteNonQuery();
                    }
                }
                tx.Commit();
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }
        catch (InvalidOperationException ex)
        {
            MessageBox.Show(FindForm(), ex.Message, "FloreHome", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            CargarProductos();
            return;
        }
        catch (Exception ex)
        {
            MessageBox.Show(FindForm(), "No se pudo guardar la factura.\n\n" + ex.Message, "FloreHome",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        var vendidos = _lineas.AsEnumerable().Select(l => Convert.ToInt32(l["Id"])).ToList();
        _lineas.Rows.Clear();
        SeleccionarCliente(0, ClienteGeneral);
        _rbConsumidor.Checked = true;
        _pago.SelectedIndex = 0;
        CargarProductos();
        Alertas.RevisarProductos(vendidos);       // ¿quedó algún producto con poco stock?
        OfrecerComprobante((int)idFactura);
    }

    private void OfrecerComprobante(int idFactura)
    {
        var r = MessageBox.Show(FindForm(),
            "Factura N° " + idFactura.ToString("D6") + " guardada.\n¿Quieres guardar el comprobante en PDF?",
            "FloreHome", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (r != DialogResult.Yes) return;

        using var dlg = new SaveFileDialog
        {
            Filter = "PDF (*.pdf)|*.pdf",
            FileName = "Comprobante_" + idFactura.ToString("D6") + ".pdf"
        };
        if (dlg.ShowDialog(FindForm()) != DialogResult.OK) return;
        try
        {
            Exportador.Comprobante(idFactura, dlg.FileName);
            try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(dlg.FileName) { UseShellExecute = true }); }
            catch { }
        }
        catch (Exception ex)
        {
            MessageBox.Show(FindForm(), "No se pudo crear el PDF.\n\n" + ex.Message, "FloreHome",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            foreach (var img in _imagenes) img.Dispose();
        base.Dispose(disposing);
    }
}
