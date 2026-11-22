using System.Data;
using System.Globalization;
using ClosedXML.Excel;
using FloreHome.Core;
using FloreHome.Imaging;
using FloreHome.Ui;

namespace FloreHome.Services;

/// <summary>Exporta tablas a Excel y a PDF con la paleta de FloreHome.</summary>
public static class Exportador
{
    private const string HexCafe = "#684F48";
    private const string HexMenta = "#D6E9ED";
    private const string HexCrema = "#F9E7C7";
    private const string HexCremaSuave = "#FDF6E6";

    // ------------------------------------------------------------------
    //  Tomar lo que se ve en pantalla (columnas visibles, filtro y orden)
    // ------------------------------------------------------------------
    public static DataTable TablaDeGrid(DataGridView grid)
    {
        var dt = new DataTable();
        var columnas = new List<DataGridViewColumn>();
        foreach (DataGridViewColumn c in grid.Columns.Cast<DataGridViewColumn>().OrderBy(col => col.DisplayIndex))
        {
            if (!c.Visible || c is DataGridViewImageColumn) continue;
            string nombre = string.IsNullOrWhiteSpace(c.HeaderText) ? c.Name : c.HeaderText;
            string unico = nombre;
            int n = 2;
            while (dt.Columns.Contains(unico)) unico = nombre + " " + (n++);
            dt.Columns.Add(unico, typeof(object));
            columnas.Add(c);
        }
        foreach (DataGridViewRow fila in grid.Rows)
        {
            var nueva = dt.NewRow();
            for (int i = 0; i < columnas.Count; i++)
                nueva[i] = fila.Cells[columnas[i].Index].Value ?? DBNull.Value;
            dt.Rows.Add(nueva);
        }
        return dt;
    }

    private static string Formato(object v)
    {
        if (v == null || v == DBNull.Value) return "";
        if (v is decimal d) return "$" + d.ToString("N2", CultureInfo.InvariantCulture);
        if (v is double db) return "$" + db.ToString("N2", CultureInfo.InvariantCulture);
        if (v is DateTime f) return f.TimeOfDay == TimeSpan.Zero ? f.ToString("dd/MM/yyyy") : f.ToString("dd/MM/yyyy HH:mm");
        if (v is bool b) return b ? "Sí" : "No";
        return Convert.ToString(v, CultureInfo.InvariantCulture) ?? "";
    }

    private static bool EsNumero(object v) => v is decimal || v is double || v is int || v is long;

    // ------------------------------------------------------------------
    //  EXCEL
    // ------------------------------------------------------------------
    public static void Excel(DataTable tabla, string titulo, string ruta)
    {
        int nCols = Math.Max(1, tabla.Columns.Count);
        using var wb = new XLWorkbook();
        string hoja = new string(titulo.Where(c => !"[]:*?/\\".Contains(c)).ToArray());
        if (hoja.Length == 0) hoja = "Datos";
        if (hoja.Length > 31) hoja = hoja.Substring(0, 31);
        var ws = wb.Worksheets.Add(hoja);

        // Encabezado del reporte
        ws.Cell(1, 1).Value = "FloreHome · " + titulo;
        ws.Range(1, 1, 1, nCols).Merge();
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontSize = 18;
        ws.Cell(1, 1).Style.Font.FontColor = XLColor.FromHtml(HexCafe);
        ws.Row(1).Height = 32;

        string quien = string.IsNullOrEmpty(Sesion.NombreCompleto) ? "" : "  ·  " + Sesion.NombreCompleto;
        ws.Cell(2, 1).Value = "Generado el " + DateTime.Now.ToString("dd/MM/yyyy HH:mm") + quien;
        ws.Range(2, 1, 2, nCols).Merge();
        ws.Cell(2, 1).Style.Font.FontColor = XLColor.FromHtml("#85685F");

        // Encabezados de la tabla
        const int filaEnc = 4;
        for (int c = 0; c < tabla.Columns.Count; c++)
            ws.Cell(filaEnc, c + 1).Value = tabla.Columns[c].ColumnName;
        var enc = ws.Range(filaEnc, 1, filaEnc, nCols);
        enc.Style.Fill.BackgroundColor = XLColor.FromHtml(HexCafe);
        enc.Style.Font.FontColor = XLColor.FromHtml(HexCrema);
        enc.Style.Font.Bold = true;
        enc.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        ws.Row(filaEnc).Height = 24;

        // Datos
        for (int r = 0; r < tabla.Rows.Count; r++)
        {
            int fila = filaEnc + 1 + r;
            for (int c = 0; c < tabla.Columns.Count; c++)
            {
                object v = tabla.Rows[r][c];
                var celda = ws.Cell(fila, c + 1);
                if (v == null || v == DBNull.Value) continue;

                if (v is decimal d) { celda.Value = (double)d; celda.Style.NumberFormat.Format = "$#,##0.00"; }
                else if (v is double db) { celda.Value = db; celda.Style.NumberFormat.Format = "$#,##0.00"; }
                else if (v is int i) { celda.Value = i; }
                else if (v is long l) { celda.Value = (double)l; }
                else if (v is DateTime f) { celda.Value = f; celda.Style.NumberFormat.Format = f.TimeOfDay == TimeSpan.Zero ? "dd/mm/yyyy" : "dd/mm/yyyy hh:mm"; }
                else celda.Value = Convert.ToString(v, CultureInfo.InvariantCulture);
            }
            if (r % 2 == 1)
                ws.Range(fila, 1, fila, nCols).Style.Fill.BackgroundColor = XLColor.FromHtml(HexCremaSuave);
        }

        int ultima = filaEnc + Math.Max(1, tabla.Rows.Count);
        var cuerpo = ws.Range(filaEnc, 1, ultima, nCols);
        cuerpo.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        cuerpo.Style.Border.OutsideBorderColor = XLColor.FromHtml(HexMenta);
        cuerpo.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        cuerpo.Style.Border.InsideBorderColor = XLColor.FromHtml(HexMenta);

        ws.Columns(1, nCols).AdjustToContents(filaEnc, ultima);
        for (int c = 1; c <= nCols; c++)
            if (ws.Column(c).Width < 12) ws.Column(c).Width = 12;
        ws.SheetView.FreezeRows(filaEnc);

        wb.SaveAs(ruta);
    }

    // ------------------------------------------------------------------
    //  PDF · listados
    // ------------------------------------------------------------------
    private static readonly Color CCafe = ColorTranslator.FromHtml(HexCafe);
    private static readonly Color CMenta = ColorTranslator.FromHtml(HexMenta);
    private static readonly Color CCrema = ColorTranslator.FromHtml(HexCrema);
    private static readonly Color CSuave = ColorTranslator.FromHtml(HexCremaSuave);

    private static PdfDocumento NuevoDocumento()
    {
        var doc = new PdfDocumento();
        try
        {
            using var s = typeof(Exportador).Assembly.GetManifestResourceStream("flor.png");
            if (s != null)
            {
                using var logo = new Bitmap(s);
                doc.EstablecerLogo(logo);
            }
        }
        catch { }
        return doc;
    }

    private static void Encabezado(PdfPagina p, string subtitulo)
    {
        p.Rectangulo(0, 0, p.Ancho, 64, CCafe);
        p.Logo(26, 8, 48, 48);
        p.Texto(84, 12, "FloreHome", 20, true, CCrema);
        p.Texto(84, 38, subtitulo, 11, false, CMenta);
        string fecha = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
        double w = PdfDocumento.Ancho(fecha, 9, false);
        p.Texto(p.Ancho - 30 - w, 26, fecha, 9, false, CCrema);
    }

    private static void PiesDePagina(PdfDocumento doc)
    {
        int total = doc.Paginas.Count;
        for (int i = 0; i < total; i++)
        {
            var p = doc.Paginas[i];
            p.Linea(30, p.Alto - 34, p.Ancho - 30, p.Alto - 34, CMenta, 0.8);
            p.Texto(30, p.Alto - 26, "FloreHome", 8, true, CCafe);
            string t = "Página " + (i + 1) + " de " + total;
            p.Texto(p.Ancho - 30 - PdfDocumento.Ancho(t, 8, false), p.Alto - 26, t, 8, false, CCafe);
        }
    }

    public static void Pdf(DataTable tabla, string titulo, string ruta)
    {
        const double margen = 30, tam = 9, altoFila = 22, altoEnc = 26;
        bool horizontal = tabla.Columns.Count > 6;
        double anchoPag = horizontal ? 841.89 : 595.28;
        double altoPag = horizontal ? 595.28 : 841.89;
        double util = anchoPag - margen * 2;

        var doc = NuevoDocumento();
        int nCols = tabla.Columns.Count;

        // Ancho natural de cada columna según su contenido
        var natural = new double[nCols];
        for (int c = 0; c < nCols; c++)
        {
            double mayor = PdfDocumento.Ancho(tabla.Columns[c].ColumnName, tam, true);
            for (int r = 0; r < tabla.Rows.Count; r++)
            {
                double w = PdfDocumento.Ancho(Formato(tabla.Rows[r][c]), tam, false);
                if (w > mayor) mayor = w;
            }
            natural[c] = Math.Min(mayor, 240) + 16;
        }
        double suma = natural.Sum();
        var anchos = natural.Select(a => a * util / (suma <= 0 ? 1 : suma)).ToArray();

        var pagina = doc.NuevaPagina(anchoPag, altoPag);
        Encabezado(pagina, titulo);
        double y = 84;

        void EncabezadoTabla()
        {
            pagina.Rectangulo(margen, y, util, altoEnc, CCafe);
            double x = margen;
            for (int c = 0; c < nCols; c++)
            {
                string t = PdfDocumento.Ajustar(tabla.Columns[c].ColumnName, tam, true, anchos[c] - 12);
                pagina.Texto(x + 6, y + 8, t, tam, true, CCrema);
                x += anchos[c];
            }
            y += altoEnc;
        }
        EncabezadoTabla();

        for (int r = 0; r < tabla.Rows.Count; r++)
        {
            if (y + altoFila > altoPag - 50)
            {
                pagina = doc.NuevaPagina(anchoPag, altoPag);
                Encabezado(pagina, titulo);
                y = 84;
                EncabezadoTabla();
            }
            if (r % 2 == 1) pagina.Rectangulo(margen, y, util, altoFila, CSuave);

            double x = margen;
            for (int c = 0; c < nCols; c++)
            {
                object v = tabla.Rows[r][c];
                string t = PdfDocumento.Ajustar(Formato(v), tam, false, anchos[c] - 12);
                double tx = EsNumero(v) ? x + anchos[c] - 6 - PdfDocumento.Ancho(t, tam, false) : x + 6;
                pagina.Texto(tx, y + 7, t, tam, false, CCafe);
                x += anchos[c];
            }
            pagina.Linea(margen, y + altoFila, margen + util, y + altoFila, CMenta, 0.4);
            y += altoFila;
        }

        y += 12;
        if (y > altoPag - 50)
        {
            pagina = doc.NuevaPagina(anchoPag, altoPag);
            Encabezado(pagina, titulo);
            y = 90;
        }
        pagina.Texto(margen, y, "Registros: " + tabla.Rows.Count, 9, true, CCafe);

        PiesDePagina(doc);
        doc.Guardar(ruta);
    }

    // ------------------------------------------------------------------
    //  PDF · comprobante de una factura
    // ------------------------------------------------------------------
    public static void Comprobante(int idFactura, string ruta)
    {
        var enc = Db.Tabla(
            "SELECT f.Idfactura, f.Fecha, f.ClienteNombre, f.TipoDocumento, f.MetodoPago, f.Subtotal, f.Descuento, f.IVA, f.Total, " +
            "f.Estado, f.FechaAnulacion, f.MotivoAnulacion, COALESCE(u.NombreCompleto, '') AS Vendedor " +
            "FROM Factura f LEFT JOIN Usuario u ON u.IdUsuario = f.IdUsuario WHERE f.Idfactura = @id",
            ("@id", idFactura));
        if (enc.Rows.Count == 0) throw new InvalidOperationException("La factura no existe.");
        var fila = enc.Rows[0];

        var det = Db.Tabla(
            "SELECT p.Descripcion, d.cantidad, d.Precio, d.Total FROM Detallefactura d " +
            "JOIN Producto p ON p.Idproducto = d.idproducto WHERE d.idfactura = @id ORDER BY d.Iddetalle",
            ("@id", idFactura));

        bool anulada = Convert.ToString(fila["Estado"]) == "Anulada";
        bool credito = Convert.ToString(fila["TipoDocumento"]) == Fiscal.CreditoFiscal;
        string titulo = credito ? "Comprobante de crédito fiscal" : "Factura · Consumidor final";
        decimal descuento = Convert.ToDecimal(fila["Descuento"]);
        decimal total = Convert.ToDecimal(fila["Total"]);
        decimal bruto = 0;
        foreach (DataRow d in det.Rows) bruto += Convert.ToDecimal(d["Total"]);

        const double ancho = 419.53, alto = 595.28, margen = 30, util = ancho - margen * 2;
        var doc = NuevoDocumento();
        var p = doc.NuevaPagina(ancho, alto);
        Encabezado(p, titulo);

        double y = 82;
        if (anulada)
        {
            p.Rectangulo(margen, y, util, 24, ColorTranslator.FromHtml("#A65D57"));
            p.Texto(margen + 8, y + 6, "FACTURA ANULADA", 11, true, Color.White);
            y += 32;
        }
        p.Texto(margen, y, "Factura N° " + Convert.ToInt32(fila["Idfactura"]).ToString("D6"), 14, true, CCafe);
        y += 26;
        p.Texto(margen, y, "Fecha: " + Convert.ToDateTime(fila["Fecha"]).ToString("dd/MM/yyyy HH:mm"), 10, false, CCafe);
        y += 16;
        p.Texto(margen, y, "Cliente: " + PdfDocumento.Ajustar(Convert.ToString(fila["ClienteNombre"]), 10, false, util - 50), 10, false, CCafe);
        y += 16;
        p.Texto(margen, y, "Pago: " + Convert.ToString(fila["MetodoPago"]), 10, false, CCafe);
        y += 16;
        string vendedor = Convert.ToString(fila["Vendedor"]);
        if (vendedor.Length > 0)
        {
            p.Texto(margen, y, "Atendió: " + PdfDocumento.Ajustar(vendedor, 10, false, util - 55), 10, false, CCafe);
            y += 16;
        }
        if (anulada)
        {
            string motivo = fila["MotivoAnulacion"] == DBNull.Value ? "" : Convert.ToString(fila["MotivoAnulacion"]);
            p.Texto(margen, y, "Motivo: " + PdfDocumento.Ajustar(motivo, 10, false, util - 50), 10, false, ColorTranslator.FromHtml("#A65D57"));
            y += 16;
        }
        y += 10;

        double[] col = { util * 0.50, util * 0.12, util * 0.19, util * 0.19 };
        string[] cab = { "Producto", "Cant.", "Precio", "Subtotal" };

        void EncTabla()
        {
            p.Rectangulo(margen, y, util, 26, CCafe);
            double x = margen;
            for (int c = 0; c < 4; c++)
            {
                double w = PdfDocumento.Ancho(cab[c], 9, true);
                double tx = c == 0 ? x + 6 : x + col[c] - 6 - w;
                p.Texto(tx, y + 8, cab[c], 9, true, CCrema);
                x += col[c];
            }
            y += 26;
        }
        EncTabla();

        for (int r = 0; r < det.Rows.Count; r++)
        {
            if (y + 24 > alto - 150)
            {
                p = doc.NuevaPagina(ancho, alto);
                Encabezado(p, titulo);
                y = 86;
                EncTabla();
            }
            if (r % 2 == 1) p.Rectangulo(margen, y, util, 24, CSuave);
            string[] valores =
            {
                PdfDocumento.Ajustar(Convert.ToString(det.Rows[r]["Descripcion"]), 9, false, col[0] - 12),
                Convert.ToString(det.Rows[r]["cantidad"]),
                credito ? Formato(Fiscal.SinIva(Convert.ToDecimal(det.Rows[r]["Precio"]))) : Formato(det.Rows[r]["Precio"]),
                credito ? Formato(Fiscal.SinIva(Convert.ToDecimal(det.Rows[r]["Total"]))) : Formato(det.Rows[r]["Total"])
            };
            double x = margen;
            for (int c = 0; c < 4; c++)
            {
                double w = PdfDocumento.Ancho(valores[c], 9, false);
                double tx = c == 0 ? x + 6 : x + col[c] - 6 - w;
                p.Texto(tx, y + 8, valores[c], 9, false, CCafe);
                x += col[c];
            }
            p.Linea(margen, y + 24, margen + util, y + 24, CMenta, 0.4);
            y += 24;
        }

        // Totales
        if (y + 130 > alto - 40)
        {
            p = doc.NuevaPagina(ancho, alto);
            Encabezado(p, titulo);
            y = 86;
        }
        y += 14;
        void Linea(string etiqueta, string valor, bool grande)
        {
            double tam = grande ? 13 : 10;
            double wv = PdfDocumento.Ancho(valor, tam, grande);
            p.Texto(margen + util - 6 - wv, y, valor, tam, grande, CCafe);
            double we = PdfDocumento.Ancho(etiqueta, tam, grande);
            p.Texto(margen + util - 150 - we, y, etiqueta, tam, grande, CCafe);
            y += grande ? 24 : 18;
        }
        if (credito)
        {
            // Crédito fiscal: productos sin IVA, monto del IVA y total (suma de los dos)
            decimal netoBruto = Fiscal.SinIva(bruto);
            decimal subtotal = Convert.ToDecimal(fila["Subtotal"]);
            Linea("Productos (sin IVA)", Formato(netoBruto), false);
            if (descuento > 0) Linea("Descuento cliente frecuente", "-" + Formato(netoBruto - subtotal), false);
            Linea("IVA (13%)", Formato(fila["IVA"]), false);
        }
        else
        {
            // Consumidor final: no se menciona el IVA
            Linea("Productos", Formato(bruto), false);
            if (descuento > 0) Linea("Descuento cliente frecuente", "-" + Formato(descuento), false);
            y -= 6;     // la línea queda pegada a "Productos": no se deja el hueco donde iría el IVA
        }
        p.Linea(margen + util - 220, y, margen + util, y, CCafe, 1);
        y += 8;
        Linea("TOTAL", Formato(total), true);

        y += 20;
        string gracias = "¡Gracias por su compra!";
        p.Texto((ancho - PdfDocumento.Ancho(gracias, 12, true)) / 2, y, gracias, 12, true, CCafe);

        PiesDePagina(doc);
        doc.Guardar(ruta);
    }
}
