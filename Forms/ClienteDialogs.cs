using System.Data;
using FloreHome.Core;
using FloreHome.Ui;

namespace FloreHome.Forms;

/// <summary>Buscador de clientes para facturar: se escribe parte del nombre, teléfono o correo y la lista se filtra sola.</summary>
public sealed class BuscarClienteDialog : DialogoBase
{
    private readonly TextBox _buscar = Tema.Caja();
    private readonly DataGridView _grid = new DataGridView();
    private DataView _vista;

    public int IdCliente { get; private set; }
    public string NombreCliente { get; private set; } = "";

    public BuscarClienteDialog() : base("Buscar cliente", "Buscar cliente")
    {
        BtnGuardar.Text = "Seleccionar";
        Cuerpo.Controls.Remove(Campos);

        var panel = new Panel { Size = new Size(Tema.Px(620), Tema.Px(380)) };

        Tema.EstilizarGrid(_grid);
        _grid.Dock = DockStyle.Fill;
        _grid.ColumnHeadersHeight = Tema.Px(38);
        _grid.RowTemplate.Height = Tema.Px(34);
        AgregarColumna("Nombre", "Nombre", 50, 160);
        AgregarColumna("Telefono", "Teléfono", 25, 110);
        AgregarColumna("Email", "Correo", 25, 140);
        _grid.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0) BtnGuardar.PerformClick(); };

        var fila = new Panel { Dock = DockStyle.Top, Height = Tema.Px(48), Padding = new Padding(0, 0, 0, Tema.Px(10)) };
        _buscar.Dock = DockStyle.Fill;
        var lbl = Tema.Etiqueta("Nombre, teléfono o correo");
        lbl.Dock = DockStyle.Left;
        lbl.Padding = new Padding(0, Tema.Px(8), Tema.Px(12), 0);
        fila.Controls.Add(_buscar);
        fila.Controls.Add(lbl);

        panel.Controls.Add(_grid);
        panel.Controls.Add(fila);
        Cuerpo.Controls.Add(panel);

        try
        {
            var dt = Db.Tabla("SELECT Idcliente, Nombre, Telefono, Email FROM Cliente ORDER BY Nombre");
            _vista = dt.DefaultView;
            _grid.DataSource = _vista;
        }
        catch (Exception ex)
        {
            ErrorBD("No se pudieron cargar los clientes.", ex);
        }

        _buscar.TextChanged += (s, e) => Filtrar();
        Shown += (s, e) => _buscar.Focus();
    }

    private void AgregarColumna(string prop, string encabezado, float peso, int minimo)
    {
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = prop,
            Name = prop,
            HeaderText = encabezado,
            FillWeight = peso,
            MinimumWidth = Tema.Px(minimo)
        });
    }

    private void Filtrar()
    {
        if (_vista == null) return;
        string t = _buscar.Text.Trim();
        if (t.Length == 0) { _vista.RowFilter = ""; return; }
        string like = t.Replace("[", "[[]").Replace("%", "[%]").Replace("*", "[*]").Replace("'", "''");
        _vista.RowFilter = "Nombre LIKE '%" + like + "%' OR Telefono LIKE '%" + like + "%' OR Convert(Email, 'System.String') LIKE '%" + like + "%'";
    }

    protected override bool Guardar()
    {
        var drv = _grid.CurrentRow == null ? null : _grid.CurrentRow.DataBoundItem as DataRowView;
        if (drv == null)
        {
            Aviso("Selecciona un cliente de la lista.");
            return false;
        }
        IdCliente = Convert.ToInt32(drv["Idcliente"]);
        NombreCliente = Convert.ToString(drv["Nombre"]);
        return true;
    }
}

/// <summary>Pide el motivo para anular una factura.</summary>
public sealed class AnularFacturaDialog : DialogoBase
{
    private readonly TextBox _motivo = Tema.Caja(200);

    public string Motivo => _motivo.Text.Trim();

    public AnularFacturaDialog(int idFactura) : base("Anular factura", "Anular factura N° " + idFactura.ToString("D6"), 380)
    {
        BtnGuardar.Text = "Anular";
        AgregarCampo("Motivo", _motivo);
        Regla(_motivo, () => _motivo.Text.Trim().Length >= 5 ? null : "Escribe el motivo de la anulación (mínimo 5 letras).");
    }

    protected override bool Guardar() => ValidarTodo();
}
