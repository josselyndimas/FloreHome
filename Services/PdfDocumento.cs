using System.Drawing.Imaging;
using System.Globalization;
using System.IO.Compression;
using System.Text;

namespace FloreHome.Services;

/// <summary>Una página del PDF. Se dibuja con coordenadas desde la esquina superior izquierda (en puntos).</summary>
public sealed class PdfPagina
{
    public double Ancho { get; }
    public double Alto { get; }
    internal StringBuilder Contenido { get; } = new StringBuilder();

    public PdfPagina(double ancho, double alto)
    {
        Ancho = ancho;
        Alto = alto;
    }

    private static string N(double v) => v.ToString("0.###", CultureInfo.InvariantCulture);

    private static string Rgb(Color c) =>
        N(c.R / 255.0) + " " + N(c.G / 255.0) + " " + N(c.B / 255.0);

    public void Rectangulo(double x, double y, double w, double h, Color relleno)
    {
        Contenido.Append(Rgb(relleno)).Append(" rg ")
                 .Append(N(x)).Append(' ').Append(N(Alto - y - h)).Append(' ')
                 .Append(N(w)).Append(' ').Append(N(h)).Append(" re f\n");
    }

    public void Linea(double x1, double y1, double x2, double y2, Color color, double grosor = 0.5)
    {
        Contenido.Append(Rgb(color)).Append(" RG ").Append(N(grosor)).Append(" w ")
                 .Append(N(x1)).Append(' ').Append(N(Alto - y1)).Append(" m ")
                 .Append(N(x2)).Append(' ').Append(N(Alto - y2)).Append(" l S\n");
    }

    /// <summary>Escribe texto. y = parte de arriba del texto.</summary>
    public void Texto(double x, double y, string texto, double tamano, bool negrita, Color color)
    {
        if (string.IsNullOrEmpty(texto)) return;
        string seguro = texto.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)")
                             .Replace("\r", " ").Replace("\n", " ");
        Contenido.Append("BT ").Append(negrita ? "/F2 " : "/F1 ").Append(N(tamano)).Append(" Tf ")
                 .Append(Rgb(color)).Append(" rg ")
                 .Append(N(x)).Append(' ').Append(N(Alto - (y + tamano * 0.8))).Append(" Td (")
                 .Append(seguro).Append(") Tj ET\n");
    }

    public void Logo(double x, double y, double w, double h)
    {
        Contenido.Append("q ").Append(N(w)).Append(" 0 0 ").Append(N(h)).Append(' ')
                 .Append(N(x)).Append(' ').Append(N(Alto - y - h)).Append(" cm /Im1 Do Q\n");
    }
}

/// <summary>
/// Generador de PDF sencillo y propio (sin librerías externas ni archivos nativos).
/// Usa las fuentes estándar Helvetica / Helvetica-Bold y admite un logo con transparencia.
/// </summary>
public sealed class PdfDocumento
{
    private readonly List<PdfPagina> _paginas = new List<PdfPagina>();
    private byte[] _logoRgb;
    private byte[] _logoAlfa;
    private int _logoAncho, _logoAlto;

    public IReadOnlyList<PdfPagina> Paginas => _paginas;

    public PdfPagina NuevaPagina(double ancho, double alto)
    {
        var p = new PdfPagina(ancho, alto);
        _paginas.Add(p);
        return p;
    }

    public void EstablecerLogo(Bitmap bmp)
    {
        _logoAncho = bmp.Width;
        _logoAlto = bmp.Height;
        _logoRgb = new byte[_logoAncho * _logoAlto * 3];
        _logoAlfa = new byte[_logoAncho * _logoAlto];
        int i = 0, j = 0;
        for (int y = 0; y < _logoAlto; y++)
            for (int x = 0; x < _logoAncho; x++)
            {
                Color c = bmp.GetPixel(x, y);
                _logoRgb[i++] = c.R;
                _logoRgb[i++] = c.G;
                _logoRgb[i++] = c.B;
                _logoAlfa[j++] = c.A;
            }
    }

    // ---------- Medidas de texto (Arial = mismas medidas que Helvetica) ----------
    private static readonly Bitmap _bmMedida = new Bitmap(1, 1);
    private static readonly Graphics _gMedida = CrearMedidor();

    private static Graphics CrearMedidor()
    {
        var g = Graphics.FromImage(_bmMedida);
        g.PageUnit = GraphicsUnit.Point;
        return g;
    }

    public static double Ancho(string texto, double tamano, bool negrita)
    {
        if (string.IsNullOrEmpty(texto)) return 0;
        using var f = new Font("Arial", (float)tamano, negrita ? FontStyle.Bold : FontStyle.Regular, GraphicsUnit.Point);
        return _gMedida.MeasureString(texto, f, 100000, StringFormat.GenericTypographic).Width;
    }

    /// <summary>Corta el texto con "..." para que quepa en el ancho indicado.</summary>
    public static string Ajustar(string texto, double tamano, bool negrita, double anchoMax)
    {
        if (string.IsNullOrEmpty(texto)) return "";
        if (Ancho(texto, tamano, negrita) <= anchoMax) return texto;
        string t = texto;
        while (t.Length > 1 && Ancho(t + "...", tamano, negrita) > anchoMax)
            t = t.Substring(0, t.Length - 1);
        return t.TrimEnd() + "...";
    }

    // ---------- Guardado ----------
    public void Guardar(string ruta)
    {
        using var ms = new MemoryStream();
        var offsets = new List<long>();

        void Escribir(string s)
        {
            byte[] b = Encoding.Latin1.GetBytes(s);
            ms.Write(b, 0, b.Length);
        }
        void Objeto(int id, Action cuerpo)
        {
            while (offsets.Count < id) offsets.Add(0);
            offsets[id - 1] = ms.Position;
            Escribir(id.ToString(CultureInfo.InvariantCulture) + " 0 obj\n");
            cuerpo();
            Escribir("\nendobj\n");
        }
        void Flujo(string diccionario, byte[] datos)
        {
            Escribir("<< " + diccionario + " /Length " + datos.Length.ToString(CultureInfo.InvariantCulture) + " >>\nstream\n");
            ms.Write(datos, 0, datos.Length);
            Escribir("\nendstream");
        }
        byte[] Comprimir(byte[] datos)
        {
            using var salida = new MemoryStream();
            using (var z = new ZLibStream(salida, CompressionLevel.Optimal, true))
                z.Write(datos, 0, datos.Length);
            return salida.ToArray();
        }

        bool hayLogo = _logoRgb != null;
        int idPrimeraPagina = hayLogo ? 7 : 5;     // 1 catálogo, 2 páginas, 3-4 fuentes, (5-6 imágenes)
        var idsPagina = new List<int>();
        for (int i = 0; i < _paginas.Count; i++) idsPagina.Add(idPrimeraPagina + i * 2);
        int totalObjetos = idPrimeraPagina - 1 + _paginas.Count * 2;

        Escribir("%PDF-1.4\n%\u00E2\u00E3\u00CF\u00D3\n");

        Objeto(1, () => Escribir("<< /Type /Catalog /Pages 2 0 R >>"));
        Objeto(2, () =>
        {
            var kids = string.Join(" ", idsPagina.Select(id => id.ToString(CultureInfo.InvariantCulture) + " 0 R"));
            Escribir("<< /Type /Pages /Kids [" + kids + "] /Count " + _paginas.Count.ToString(CultureInfo.InvariantCulture) + " >>");
        });
        Objeto(3, () => Escribir("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>"));
        Objeto(4, () => Escribir("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold /Encoding /WinAnsiEncoding >>"));

        if (hayLogo)
        {
            string dim = "/Type /XObject /Subtype /Image /Width " + _logoAncho.ToString(CultureInfo.InvariantCulture) +
                         " /Height " + _logoAlto.ToString(CultureInfo.InvariantCulture) + " /BitsPerComponent 8 /Filter /FlateDecode";
            Objeto(5, () => Flujo(dim + " /ColorSpace /DeviceGray", Comprimir(_logoAlfa)));
            Objeto(6, () => Flujo(dim + " /ColorSpace /DeviceRGB /SMask 5 0 R", Comprimir(_logoRgb)));
        }

        for (int i = 0; i < _paginas.Count; i++)
        {
            var p = _paginas[i];
            int idPagina = idPrimeraPagina + i * 2;
            int idContenido = idPagina + 1;
            string recursos = "/Resources << /Font << /F1 3 0 R /F2 4 0 R >>" +
                              (hayLogo ? " /XObject << /Im1 6 0 R >>" : "") + " >>";
            Objeto(idPagina, () => Escribir(
                "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 " +
                p.Ancho.ToString("0.##", CultureInfo.InvariantCulture) + " " +
                p.Alto.ToString("0.##", CultureInfo.InvariantCulture) + "] " + recursos +
                " /Contents " + idContenido.ToString(CultureInfo.InvariantCulture) + " 0 R >>"));
            Objeto(idContenido, () => Flujo("", Encoding.Latin1.GetBytes(p.Contenido.ToString())));
        }

        long posXref = ms.Position;
        Escribir("xref\n0 " + (totalObjetos + 1).ToString(CultureInfo.InvariantCulture) + "\n");
        Escribir("0000000000 65535 f \n");
        for (int i = 0; i < totalObjetos; i++)
            Escribir(offsets[i].ToString("D10", CultureInfo.InvariantCulture) + " 00000 n \n");
        Escribir("trailer\n<< /Size " + (totalObjetos + 1).ToString(CultureInfo.InvariantCulture) + " /Root 1 0 R >>\n");
        Escribir("startxref\n" + posXref.ToString(CultureInfo.InvariantCulture) + "\n%%EOF\n");

        File.WriteAllBytes(ruta, ms.ToArray());
    }
}
