using FloreHome.Core;
using FloreHome.Imaging;
using FloreHome.Ui;
using MySqlConnector;

namespace FloreHome.Forms;

/// <summary>
/// Crear o editar un producto. La foto se puede dejar normal o convertir a pixel art;
/// en los dos casos queda en un cuadro de 256x256 para que el menú de facturación se vea parejo.
/// </summary>
public sealed class ProductoDialog : DialogoBase
{
    public static readonly string[] Categorias = { "Flores", "Ramos", "Plantas", "Arreglos", "Regalos", "Extras", "Otros" };

    private readonly int? _id;
    private readonly TextBox _codigo = Tema.Caja(20);
    private readonly TextBox _descripcion = Tema.Caja(150);
    private readonly ComboBox _categoria = Tema.Combo(Categorias);
    private readonly NumericUpDown _cantidad = new NumericUpDown
    {
        Minimum = 0, Maximum = 100000, Font = Tema.FNormal, ForeColor = Tema.Cafe
    };
    private readonly NumericUpDown _precio = new NumericUpDown
    {
        Minimum = 0.01m, Maximum = 100000m, DecimalPlaces = 2, Increment = 0.25m, Value = 1m,
        Font = Tema.FNormal, ForeColor = Tema.Cafe
    };

    private readonly PixelBox _vista = new PixelBox();
    private readonly CheckBox _convertir = new CheckBox();
    private readonly CheckBox _quitarFondo = new CheckBox();
    private readonly Button _btnSubir = Tema.Boton("Subir foto", Estilo.Secundario);
    private readonly Button _btnQuitar = Tema.Boton("Quitar imagen", Estilo.Secundario);

    private Bitmap _origen;          // foto original elegida (para volver a convertir)
    private Bitmap _resultado;       // lo que se muestra
    private byte[] _bytes;           // lo que se guardará
    private bool _cambioImagen;
    private bool _preparando = true;

    public ProductoDialog(int? id = null)
        : base(id == null ? "Nuevo producto" : "Editar producto", id == null ? "Nuevo producto" : "Editar producto")
    {
        _id = id;

        _codigo.ReadOnly = true;
        _codigo.TabStop = false;
        _codigo.BackColor = Tema.CremaSuave;
        _codigo.Text = _id == null ? "Se genera solo" : "";
        AgregarCampo("Código", _codigo);
        AgregarCampo("Descripción", _descripcion);
        AgregarCampo("Categoría", _categoria);
        AgregarCampo("Cantidad en stock", _cantidad);
        AgregarCampo("Precio ($)", _precio);

        Regla(_descripcion, () => _descripcion.Text.Trim().Length >= 2 ? null : "Escribe la descripción del producto.");

        // Panel derecho: vista previa fija + opciones de la foto
        var derecha = new TableLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            Margin = new Padding(Tema.Px(32), 0, 0, 0)
        };
        _vista.Size = new Size(Tema.Px(192) + 2, Tema.Px(192) + 2);
        _vista.Margin = new Padding(0, Tema.Px(6), 0, Tema.Px(10));

        _convertir.Text = "Convertir a pixel art";
        _convertir.AutoSize = true;
        _convertir.Checked = true;
        _convertir.Font = Tema.FNormal;
        _convertir.ForeColor = Tema.Cafe;
        _convertir.Margin = new Padding(0, 0, 0, Tema.Px(6));

        _quitarFondo.Text = "Quitar el fondo (fotos con fondo liso)";
        _quitarFondo.AutoSize = true;
        _quitarFondo.Checked = false;
        _quitarFondo.Font = Tema.FNormal;
        _quitarFondo.ForeColor = Tema.Cafe;
        _quitarFondo.Margin = new Padding(0, 0, 0, Tema.Px(10));

        var botones = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            WrapContents = true,
            Margin = new Padding(0)
        };
        botones.Controls.Add(_btnSubir);
        botones.Controls.Add(_btnQuitar);

        derecha.Controls.Add(_vista, 0, 0);
        derecha.Controls.Add(botones, 0, 1);
        derecha.Controls.Add(_convertir, 0, 2);
        derecha.Controls.Add(_quitarFondo, 0, 3);
        Cuerpo.Controls.Add(derecha);

        ActualizarOpciones();

        _btnSubir.Click += (s, e) => SubirFoto();
        _btnQuitar.Click += (s, e) => QuitarImagen();
        _convertir.CheckedChanged += (s, e) => { ActualizarOpciones(); if (_origen != null) Convertir(); };
        _quitarFondo.CheckedChanged += (s, e) => { if (_origen != null) Convertir(); };

        if (_id != null) Cargar(_id.Value);
        _preparando = false;
    }

    private void ActualizarOpciones()
    {
        bool hayFoto = _origen != null;
        _convertir.Enabled = hayFoto;
        _quitarFondo.Enabled = hayFoto && _convertir.Checked;
    }

    private void Cargar(int id)
    {
        var dt = Db.Tabla("SELECT Codigo, Descripcion, Categoria, Cantidad, Precio, Imagen FROM Producto WHERE Idproducto = @id", ("@id", id));
        if (dt.Rows.Count == 0) return;
        var f = dt.Rows[0];
        _codigo.Text = f["Codigo"] == DBNull.Value ? "" : Convert.ToString(f["Codigo"]);
        _descripcion.Text = Convert.ToString(f["Descripcion"]);
        string cat = Convert.ToString(f["Categoria"]);
        if (!_categoria.Items.Contains(cat)) _categoria.Items.Add(cat);
        _categoria.SelectedItem = cat;
        _cantidad.Value = Convert.ToDecimal(f["Cantidad"]);
        _precio.Value = Math.Max(_precio.Minimum, Convert.ToDecimal(f["Precio"]));
        if (f["Imagen"] is byte[] datos && datos.Length > 0)
        {
            _bytes = datos;
            _resultado = PixelArt.DesdeBytes(datos);
            _vista.Suavizado = PixelArt.EsFoto(datos);
            _vista.Imagen = _resultado;
        }
    }

    private void SubirFoto()
    {
        using var dlg = new OpenFileDialog
        {
            Title = "Elige la foto del producto",
            Filter = "Imágenes (*.jpg;*.jpeg;*.png;*.bmp;*.gif;*.webp)|*.jpg;*.jpeg;*.png;*.bmp;*.gif;*.webp"
        };
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        try
        {
            var nueva = PixelArt.CargarImagen(dlg.FileName);
            _origen?.Dispose();
            _origen = nueva;
            ActualizarOpciones();
            Convertir();
        }
        catch (Exception ex)
        {
            ErrorBD("No se pudo abrir esa imagen. Prueba con un archivo JPG o PNG.", ex);
        }
    }

    private void Convertir()
    {
        if (_origen == null || _preparando) return;
        Cursor = Cursors.WaitCursor;
        try
        {
            bool comoFoto = !_convertir.Checked;
            Bitmap nuevo;
            bool pedirFondo = _quitarFondo.Checked && _convertir.Checked;
            bool fondoQuitado = false;
            if (comoFoto)
            {
                nuevo = PixelArt.FotoNormal(_origen);
            }
            else
            {
                nuevo = PixelArt.Convertir(_origen, PixelArt.GridFino, pedirFondo, out fondoQuitado);
            }

            _vista.Suavizado = comoFoto;
            _vista.Imagen = nuevo;
            _resultado?.Dispose();
            _resultado = nuevo;
            _bytes = PixelArt.ABytes(nuevo, comoFoto);
            _cambioImagen = true;

            if (pedirFondo && !fondoQuitado)
                MessageBox.Show(this,
                    "No se pudo separar el fondo de esta foto, así que se usó la imagen completa.\nFunciona mejor con fotos de fondo liso.",
                    "FloreHome", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            ErrorBD("No se pudo convertir la imagen.", ex);
        }
        finally
        {
            Cursor = Cursors.Default;
        }
    }

    private void QuitarImagen()
    {
        _origen?.Dispose();
        _origen = null;
        _bytes = null;
        _vista.Imagen = null;
        _vista.Suavizado = false;
        _resultado?.Dispose();
        _resultado = null;
        _cambioImagen = true;
        ActualizarOpciones();
    }

    protected override bool Guardar()
    {
        if (!ValidarTodo()) return false;

        string desc = _descripcion.Text.Trim();
        string cat = Convert.ToString(_categoria.SelectedItem) ?? "Otros";
        int cantidad = (int)_cantidad.Value;
        decimal precio = _precio.Value;

        try
        {
            if (_id == null)
            {
                // El código se genera solo con las primeras letras del nombre (TUL-0001); si dos personas
                // guardan a la vez y el número se repite, se vuelve a intentar con el siguiente.
                for (int intento = 0; ; intento++)
                {
                    string codigo = Codigos.Generar(desc);
                    try
                    {
                        Db.Ejecutar(
                            "INSERT INTO Producto (Codigo, Descripcion, Categoria, Cantidad, Precio, Imagen) VALUES (@co, @d, @c, @q, @p, @i)",
                            ("@co", codigo), ("@d", desc), ("@c", cat), ("@q", cantidad), ("@p", precio), ("@i", _bytes));
                        break;
                    }
                    catch (MySqlException ex) when (ex.Number == 1062 && intento < 3)
                    {
                        // código repetido: probar de nuevo
                    }
                }
            }
            else if (_cambioImagen)
            {
                Db.Ejecutar(
                    "UPDATE Producto SET Descripcion=@d, Categoria=@c, Cantidad=@q, Precio=@p, Imagen=@i WHERE Idproducto=@id",
                    ("@d", desc), ("@c", cat), ("@q", cantidad), ("@p", precio), ("@i", _bytes), ("@id", _id.Value));
            }
            else
            {
                Db.Ejecutar(
                    "UPDATE Producto SET Descripcion=@d, Categoria=@c, Cantidad=@q, Precio=@p WHERE Idproducto=@id",
                    ("@d", desc), ("@c", cat), ("@q", cantidad), ("@p", precio), ("@id", _id.Value));
            }
            Alertas.RevisarProductos(null);           // actualiza las alertas de stock
            return true;
        }
        catch (Exception ex)
        {
            ErrorBD("No se pudo guardar el producto.", ex);
            return false;
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _origen?.Dispose();
            _resultado?.Dispose();
        }
        base.Dispose(disposing);
    }
}
