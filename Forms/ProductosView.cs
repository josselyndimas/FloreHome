using System.Data;
using FloreHome.Core;
using FloreHome.Imaging;
using FloreHome.Ui;
using MySqlConnector;

namespace FloreHome.Forms;

public sealed class ProductosView : VistaLista
{
    private readonly Dictionary<int, Image> _miniaturas = new Dictionary<int, Image>();
    private readonly int _ladoMiniatura = Math.Max(32, (int)Math.Round(Tema.Px(64) / 32.0) * 32);

    protected override string Titulo => "Productos";
    protected override string[] ColumnasBusqueda => new[] { "Codigo", "Descripcion", "Categoria" };

    public ProductosView()
    {
        if (Sesion.EsAdmin)
        {
            AgregarBoton("Nuevo producto", Estilo.Primario, (s, e) => Nuevo());
            AgregarBoton("Editar", Estilo.Secundario, (s, e) => Editar());
            AgregarBoton("Eliminar", Estilo.Peligro, (s, e) => Eliminar());
            Grid.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0) Editar(); };
        }
        AgregarExportaciones();
        Grid.CellFormatting += PintarFoto;
    }

    protected override DataTable Consultar()
    {
        LimpiarMiniaturas();
        return Db.Tabla("SELECT Idproducto, Codigo, Descripcion, Categoria, Cantidad, Precio, Imagen FROM Producto ORDER BY Descripcion");
    }

    protected override void CrearColumnas()
    {
        Grid.RowTemplate.Height = _ladoMiniatura + Tema.Px(12);

        var foto = new DataGridViewImageColumn
        {
            Name = "Foto",
            HeaderText = "Foto",
            ImageLayout = DataGridViewImageCellLayout.Normal,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
            Width = _ladoMiniatura + Tema.Px(24),
            Resizable = DataGridViewTriState.False
        };
        foto.DefaultCellStyle.NullValue = null;
        foto.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        Grid.Columns.Add(foto);

        Col("Codigo", "Código", 13, minimo: 100);
        Col("Descripcion", "Producto", 34, minimo: 180);
        Col("Categoria", "Categoría", 15, minimo: 110);
        Col("Cantidad", "Stock", 10, derecha: true, minimo: 80);
        Col("Precio", "Precio", 14, "$#,##0.00", true, 100);
    }

    private void PintarFoto(object sender, DataGridViewCellFormattingEventArgs e)
    {
        if (e.RowIndex < 0 || Grid.Columns[e.ColumnIndex].Name != "Foto") return;
        var drv = Grid.Rows[e.RowIndex].DataBoundItem as DataRowView;
        if (drv == null) return;
        var datos = drv["Imagen"] as byte[];
        if (datos == null || datos.Length == 0) return;

        int id = Convert.ToInt32(drv["Idproducto"]);
        if (!_miniaturas.TryGetValue(id, out var img))
        {
            using var completa = PixelArt.DesdeBytes(datos);
            img = PixelArt.Miniatura(completa, _ladoMiniatura, PixelArt.EsFoto(datos));
            _miniaturas[id] = img;
        }
        e.Value = img;
        e.FormattingApplied = true;
    }

    private void LimpiarMiniaturas()
    {
        foreach (var img in _miniaturas.Values) img.Dispose();
        _miniaturas.Clear();
    }

    private void Nuevo()
    {
        using var d = new ProductoDialog();
        if (d.ShowDialog(FindForm()) == DialogResult.OK) Recargar();
    }

    private void Editar()
    {
        var f = Seleccion();
        if (f == null) { Avisar("Selecciona un producto de la lista."); return; }
        using var d = new ProductoDialog(Convert.ToInt32(f["Idproducto"]));
        if (d.ShowDialog(FindForm()) == DialogResult.OK) Recargar();
    }

    private void Eliminar()
    {
        var f = Seleccion();
        if (f == null) { Avisar("Selecciona un producto de la lista."); return; }
        if (!Confirmar("¿Eliminar el producto \"" + f["Descripcion"] + "\"?")) return;
        try
        {
            Db.Ejecutar("DELETE FROM Producto WHERE Idproducto = @id", ("@id", Convert.ToInt32(f["Idproducto"])));
            Recargar();
        }
        catch (MySqlException ex) when (ex.Number == 1451)
        {
            Avisar("Este producto ya aparece en facturas y no se puede eliminar.\nPuedes dejar su stock en 0.");
        }
        catch (Exception ex)
        {
            ErrorMsg("No se pudo eliminar el producto.", ex);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) LimpiarMiniaturas();
        base.Dispose(disposing);
    }
}
