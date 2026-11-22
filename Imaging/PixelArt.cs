using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace FloreHome.Imaging;

/// <summary>
/// Prepara las imágenes de los productos. Todas terminan en un cuadro de 256x256 px:
///  - Pixel art: cuadrícula fija (64x64 "fino" o 32x32 "grande"), colores más vivos,
///    paleta reducida y, si se pide, fondo transparente con contorno oscuro. Se guarda como PNG.
///  - Foto normal: recorte cuadrado centrado en la zona con más color. Se guarda como JPEG.
/// Así se distingue una de otra solo por el tipo de archivo (ver EsFoto).
/// </summary>
public static class PixelArt
{
    public const int Lado = 256;
    public const int GridFino = 64;
    public const int GridGrande = 32;

    private static readonly Color Contorno = Color.FromArgb(0x3B, 0x2A, 0x26);
    private static readonly Color FondoFoto = Color.FromArgb(0xF9, 0xE7, 0xC7);

    // ------------------------------------------------------------------
    //  Carga y conversión de archivos
    // ------------------------------------------------------------------
    public static Bitmap CargarImagen(string ruta)
    {
        using var fs = new FileStream(ruta, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var img = Image.FromStream(fs);
        var bmp = new Bitmap(img.Width, img.Height, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(bmp))
        {
            g.Clear(Color.White);
            g.DrawImage(img, 0, 0, img.Width, img.Height);
        }

        // Respetar la orientación de fotos tomadas con el celular (EXIF)
        try
        {
            if (img.PropertyIdList.Contains(0x0112))
            {
                int o = img.GetPropertyItem(0x0112).Value[0];
                if (o == 3) bmp.RotateFlip(RotateFlipType.Rotate180FlipNone);
                else if (o == 6) bmp.RotateFlip(RotateFlipType.Rotate90FlipNone);
                else if (o == 8) bmp.RotateFlip(RotateFlipType.Rotate270FlipNone);
            }
        }
        catch { }
        return bmp;
    }

    /// <summary>Pixel art → PNG. Foto normal → JPEG.</summary>
    public static byte[] ABytes(Bitmap bmp, bool comoFoto)
    {
        using var ms = new MemoryStream();
        if (comoFoto)
        {
            var codec = ImageCodecInfo.GetImageEncoders().First(c => c.FormatID == ImageFormat.Jpeg.Guid);
            using var ps = new EncoderParameters(1);
            ps.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 90L);
            bmp.Save(ms, codec, ps);
        }
        else
        {
            bmp.Save(ms, ImageFormat.Png);
        }
        return ms.ToArray();
    }

    public static bool EsFoto(byte[] datos) =>
        datos != null && datos.Length > 2 && datos[0] == 0xFF && datos[1] == 0xD8;

    public static Bitmap DesdeBytes(byte[] datos)
    {
        using var ms = new MemoryStream(datos);
        using var img = Image.FromStream(ms);
        return new Bitmap(img);
    }

    /// <summary>Reduce la imagen. Pixel art: cuadros nítidos. Foto: suavizada.</summary>
    public static Bitmap Miniatura(Image origen, int lado, bool suave)
    {
        var bmp = new Bitmap(lado, lado, PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(bmp);
        g.InterpolationMode = suave ? InterpolationMode.HighQualityBicubic : InterpolationMode.NearestNeighbor;
        g.PixelOffsetMode = suave ? PixelOffsetMode.HighQuality : PixelOffsetMode.Half;
        g.DrawImage(origen, new Rectangle(0, 0, lado, lado), 0, 0, origen.Width, origen.Height, GraphicsUnit.Pixel);
        return bmp;
    }

    // ------------------------------------------------------------------
    //  Recorte cuadrado inteligente: la ventana con más color (donde están las flores)
    // ------------------------------------------------------------------
    private static Rectangle RecorteCuadrado(Bitmap origen)
    {
        int ow = origen.Width, oh = origen.Height;
        int lado = Math.Min(ow, oh);
        if (Math.Abs(ow - oh) < 4) return new Rectangle(0, 0, ow, oh);

        double s = Math.Min(1.0, 160.0 / Math.Max(ow, oh));
        int w = Math.Max(1, (int)Math.Round(ow * s));
        int h = Math.Max(1, (int)Math.Round(oh * s));
        double[] perfil = new double[Math.Max(w, h)];
        using (var chico = new Bitmap(w, h, PixelFormat.Format32bppArgb))
        {
            using (var g = Graphics.FromImage(chico))
            {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.DrawImage(origen, new Rectangle(0, 0, w, h));
            }
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    Color c = chico.GetPixel(x, y);
                    int mx = Math.Max(c.R, Math.Max(c.G, c.B));
                    int mn = Math.Min(c.R, Math.Min(c.G, c.B));
                    double sat = mx == 0 ? 0 : (mx - mn) / (double)mx;
                    double v = sat + (((c.R + c.G + c.B) / 3.0) < 235 ? 0.15 : 0);
                    if (oh > ow) perfil[y] += v; else perfil[x] += v;
                }
        }

        int largo = oh > ow ? h : w;
        int ventana = Math.Max(1, (int)Math.Round(lado * s));
        ventana = Math.Min(ventana, largo);
        double[] acum = new double[largo + 1];
        for (int i = 0; i < largo; i++) acum[i + 1] = acum[i] + perfil[i];
        int mejor = 0;
        double mejorSuma = -1;
        for (int ini = 0; ini + ventana <= largo; ini++)
        {
            double suma = acum[ini + ventana] - acum[ini];
            if (suma > mejorSuma) { mejorSuma = suma; mejor = ini; }
        }

        int offset = (int)Math.Round(mejor / s);
        if (oh > ow)
        {
            offset = Math.Min(offset, oh - lado);
            return new Rectangle(0, offset, ow, lado);
        }
        offset = Math.Min(offset, ow - lado);
        return new Rectangle(offset, 0, lado, oh);
    }

    // ------------------------------------------------------------------
    //  Foto normal 256x256
    // ------------------------------------------------------------------
    public static Bitmap FotoNormal(Bitmap origen)
    {
        var r = RecorteCuadrado(origen);
        var bmp = new Bitmap(Lado, Lado, PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(bmp);
        g.Clear(FondoFoto);
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        g.DrawImage(origen, new Rectangle(0, 0, Lado, Lado), r.X, r.Y, r.Width, r.Height, GraphicsUnit.Pixel);
        return bmp;
    }

    // ------------------------------------------------------------------
    //  Pixel art
    // ------------------------------------------------------------------
    public static Bitmap Convertir(Bitmap origen, int grid, bool quitarFondo, out bool fondoQuitado)
    {
        fondoQuitado = false;
        int colores = grid <= GridGrande ? 16 : 24;

        // 1) Copia de trabajo (máx. 256 px)
        Color[,] px;
        int w, h;
        bool[,] mascara = null;

        if (quitarFondo)
        {
            double s = Math.Min(1.0, 256.0 / Math.Max(origen.Width, origen.Height));
            w = Math.Max(1, (int)Math.Round(origen.Width * s));
            h = Math.Max(1, (int)Math.Round(origen.Height * s));
            px = LeerPixeles(origen, new Rectangle(0, 0, origen.Width, origen.Height), w, h);
            Afilar(px, w, h);
            mascara = DetectarFondo(px, w, h);
        }
        else
        {
            w = 256; h = 256;
            px = LeerPixeles(origen, RecorteCuadrado(origen), w, h);
            Afilar(px, w, h);
        }

        bool conContorno = mascara != null;
        fondoQuitado = conContorno;
        if (mascara == null)
        {
            mascara = new bool[w, h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    mascara[x, y] = true;
        }

        // 2) Caja que contiene el motivo
        int bx0 = int.MaxValue, by0 = int.MaxValue, bx1 = -1, by1 = -1;
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                if (mascara[x, y])
                {
                    if (x < bx0) bx0 = x;
                    if (x > bx1) bx1 = x;
                    if (y < by0) by0 = y;
                    if (y > by1) by1 = y;
                }
        int bw = bx1 - bx0 + 1, bh = by1 - by0 + 1;

        int interior = conContorno ? grid - 2 : grid;
        double esc = Math.Min(interior / (double)bw, interior / (double)bh);
        int tw = Math.Min(interior, Math.Max(1, (int)Math.Round(bw * esc)));
        int th = Math.Min(interior, Math.Max(1, (int)Math.Round(bh * esc)));
        int ox = (grid - tw) / 2, oy = (grid - th) / 2;

        // 3) Promediar cada cuadro de la cuadrícula
        var celdas = new double[grid, grid, 3];
        var opaco = new bool[grid, grid];
        for (int cy = 0; cy < th; cy++)
        {
            for (int cx = 0; cx < tw; cx++)
            {
                int x0 = bx0 + (int)(cx * bw / (double)tw);
                int x1 = Math.Min(bx0 + bw, Math.Max(x0 + 1, bx0 + (int)((cx + 1) * bw / (double)tw)));
                int y0 = by0 + (int)(cy * bh / (double)th);
                int y1 = Math.Min(by0 + bh, Math.Max(y0 + 1, by0 + (int)((cy + 1) * bh / (double)th)));

                double r = 0, g = 0, b = 0;
                int n = 0, total = 0;
                for (int y = y0; y < y1; y++)
                    for (int x = x0; x < x1; x++)
                    {
                        total++;
                        if (!mascara[x, y]) continue;
                        r += px[x, y].R; g += px[x, y].G; b += px[x, y].B; n++;
                    }
                if (n > 0 && n * 2 >= total)
                {
                    celdas[ox + cx, oy + cy, 0] = r / n;
                    celdas[ox + cx, oy + cy, 1] = g / n;
                    celdas[ox + cx, oy + cy, 2] = b / n;
                    opaco[ox + cx, oy + cy] = true;
                }
            }
        }

        // 4) Colores más vivos + paleta reducida
        var posiciones = new List<(int X, int Y)>();
        var muestras = new List<double[]>();
        for (int y = 0; y < grid; y++)
            for (int x = 0; x < grid; x++)
                if (opaco[x, y])
                {
                    posiciones.Add((x, y));
                    muestras.Add(Realzar(celdas[x, y, 0], celdas[x, y, 1], celdas[x, y, 2]));
                }

        var resultado = new Color?[grid, grid];
        if (muestras.Count > 0)
        {
            var paleta = KMeans(muestras, Math.Min(colores, muestras.Count));
            for (int i = 0; i < posiciones.Count; i++)
            {
                double[] m = muestras[i];
                int mejor = 0;
                double mejorDist = double.MaxValue;
                for (int k = 0; k < paleta.Count; k++)
                {
                    double dr = paleta[k][0] - m[0], dg = paleta[k][1] - m[1], db = paleta[k][2] - m[2];
                    double d = dr * dr + dg * dg + db * db;
                    if (d < mejorDist) { mejorDist = d; mejor = k; }
                }
                resultado[posiciones[i].X, posiciones[i].Y] = Color.FromArgb(
                    Limitar(paleta[mejor][0]), Limitar(paleta[mejor][1]), Limitar(paleta[mejor][2]));
            }
        }

        // 5) Contorno oscuro alrededor de la silueta
        if (conContorno)
        {
            var ddx = new[] { 1, -1, 0, 0 };
            var ddy = new[] { 0, 0, 1, -1 };
            var bordes = new List<(int X, int Y)>();
            for (int y = 0; y < grid; y++)
                for (int x = 0; x < grid; x++)
                {
                    if (opaco[x, y]) continue;
                    for (int k = 0; k < 4; k++)
                    {
                        int nx = x + ddx[k], ny = y + ddy[k];
                        if (nx >= 0 && ny >= 0 && nx < grid && ny < grid && opaco[nx, ny])
                        {
                            bordes.Add((x, y));
                            break;
                        }
                    }
                }
            foreach (var bd in bordes) resultado[bd.X, bd.Y] = Contorno;
        }

        // 6) Dibujar a 256x256
        int escala = Lado / grid;
        var salida = new Bitmap(Lado, Lado, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(salida))
        {
            g.SmoothingMode = SmoothingMode.None;
            g.Clear(Color.Transparent);
            for (int y = 0; y < grid; y++)
                for (int x = 0; x < grid; x++)
                    if (resultado[x, y].HasValue)
                        using (var br = new SolidBrush(resultado[x, y].Value))
                            g.FillRectangle(br, x * escala, y * escala, escala, escala);
        }
        return salida;
    }

    // ------------------------------------------------------------------
    //  Auxiliares de imagen
    // ------------------------------------------------------------------
    private static int Limitar(double v) => (int)Math.Max(0, Math.Min(255, Math.Round(v)));

    private static Color[,] LeerPixeles(Bitmap origen, Rectangle region, int w, int h)
    {
        var px = new Color[w, h];
        using var work = new Bitmap(w, h, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(work))
        {
            g.Clear(Color.White);
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.DrawImage(origen, new Rectangle(0, 0, w, h), region.X, region.Y, region.Width, region.Height, GraphicsUnit.Pixel);
        }
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                px[x, y] = work.GetPixel(x, y);
        return px;
    }

    /// <summary>Un poco de nitidez para que los bordes de los pétalos no se pierdan al reducir.</summary>
    private static void Afilar(Color[,] px, int w, int h)
    {
        var copia = (Color[,])px.Clone();
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                double r = 0, g = 0, b = 0;
                int n = 0;
                for (int dy = -1; dy <= 1; dy++)
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        int nx = x + dx, ny = y + dy;
                        if (nx < 0 || ny < 0 || nx >= w || ny >= h) continue;
                        r += copia[nx, ny].R; g += copia[nx, ny].G; b += copia[nx, ny].B; n++;
                    }
                Color c = copia[x, y];
                px[x, y] = Color.FromArgb(
                    Limitar(c.R + 0.9 * (c.R - r / n)),
                    Limitar(c.G + 0.9 * (c.G - g / n)),
                    Limitar(c.B + 0.9 * (c.B - b / n)));
            }
    }

    // ------------------------------------------------------------------
    //  Quitar fondo (sirve para fotos con fondo liso; devuelve null si no logra separarlo)
    // ------------------------------------------------------------------
    private static bool[,] DetectarFondo(Color[,] px, int w, int h)
    {
        // Colores dominantes del borde (3 grupos)
        var borde = new List<Color>();
        for (int x = 0; x < w; x++) { borde.Add(px[x, 0]); borde.Add(px[x, h - 1]); }
        for (int y = 0; y < h; y++) { borde.Add(px[0, y]); borde.Add(px[w - 1, y]); }

        var centros = new double[3][];
        for (int k = 0; k < 3; k++)
        {
            Color c = borde[k * borde.Count / 3];
            centros[k] = new double[] { c.R, c.G, c.B };
        }
        for (int it = 0; it < 8; it++)
        {
            var suma = new double[3, 3];
            var cuenta = new int[3];
            foreach (var c in borde)
            {
                int k = CentroMasCercano(centros, c.R, c.G, c.B);
                suma[k, 0] += c.R; suma[k, 1] += c.G; suma[k, 2] += c.B; cuenta[k]++;
            }
            for (int k = 0; k < 3; k++)
                if (cuenta[k] > 0)
                    centros[k] = new[] { suma[k, 0] / cuenta[k], suma[k, 1] / cuenta[k], suma[k, 2] / cuenta[k] };
        }

        // Distancia de cada pixel al fondo y umbral automático (Otsu)
        var dist = new double[w, h];
        double maxDist = 1;
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                double mejor = double.MaxValue;
                for (int k = 0; k < 3; k++)
                {
                    double dr = px[x, y].R - centros[k][0], dg = px[x, y].G - centros[k][1], db = px[x, y].B - centros[k][2];
                    double d = Math.Sqrt(dr * dr + dg * dg + db * db);
                    if (d < mejor) mejor = d;
                }
                dist[x, y] = mejor;
                if (mejor > maxDist) maxDist = mejor;
            }

        double umbral = Math.Max(UmbralOtsu(dist, w, h, maxDist), 22);
        var mascara = new bool[w, h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                mascara[x, y] = dist[x, y] > umbral;

        // Limpiar: cerrar huequitos, rellenar el interior, quitar manchas sueltas
        mascara = Dilatar(Dilatar(mascara, w, h), w, h);
        mascara = Erosionar(Erosionar(mascara, w, h), w, h);
        RellenarHuecos(mascara, w, h);
        mascara = Dilatar(Erosionar(mascara, w, h), w, h);
        if (!ConservarPiezasGrandes(mascara, w, h)) return null;

        int total = 0;
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                if (mascara[x, y]) total++;
        double fraccion = total / (double)(w * h);
        if (fraccion < 0.06 || fraccion > 0.90) return null;
        return mascara;
    }

    private static int CentroMasCercano(double[][] centros, double r, double g, double b)
    {
        int mejor = 0;
        double mejorD = double.MaxValue;
        for (int k = 0; k < centros.Length; k++)
        {
            double dr = r - centros[k][0], dg = g - centros[k][1], db = b - centros[k][2];
            double d = dr * dr + dg * dg + db * db;
            if (d < mejorD) { mejorD = d; mejor = k; }
        }
        return mejor;
    }

    private static double UmbralOtsu(double[,] dist, int w, int h, double maxDist)
    {
        const int cubos = 64;
        var hist = new double[cubos];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                int i = Math.Min(cubos - 1, (int)(dist[x, y] / maxDist * cubos));
                hist[i]++;
            }
        double totalPx = w * h;
        double sumaTotal = 0;
        for (int i = 0; i < cubos; i++) sumaTotal += i * hist[i];

        double wB = 0, sumaB = 0, mejorVar = -1;
        int mejorI = 0;
        for (int i = 0; i < cubos; i++)
        {
            wB += hist[i];
            if (wB == 0) continue;
            double wF = totalPx - wB;
            if (wF == 0) break;
            sumaB += i * hist[i];
            double mB = sumaB / wB, mF = (sumaTotal - sumaB) / wF;
            double v = wB * wF * (mB - mF) * (mB - mF);
            if (v > mejorVar) { mejorVar = v; mejorI = i; }
        }
        return (mejorI + 1) * maxDist / cubos;
    }

    private static bool[,] Dilatar(bool[,] m, int w, int h)
    {
        var r = new bool[w, h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                if (!m[x, y]) continue;
                for (int dy = -1; dy <= 1; dy++)
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        int nx = x + dx, ny = y + dy;
                        if (nx >= 0 && ny >= 0 && nx < w && ny < h) r[nx, ny] = true;
                    }
            }
        return r;
    }

    private static bool[,] Erosionar(bool[,] m, int w, int h)
    {
        var r = new bool[w, h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                bool todos = true;
                for (int dy = -1; dy <= 1 && todos; dy++)
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        int nx = x + dx, ny = y + dy;
                        if (nx < 0 || ny < 0 || nx >= w || ny >= h) continue;   // fuera de la imagen no cuenta
                        if (!m[nx, ny]) { todos = false; break; }
                    }
                r[x, y] = todos;
            }
        return r;
    }

    private static void RellenarHuecos(bool[,] m, int w, int h)
    {
        var fuera = new bool[w, h];
        var cola = new Queue<int>();
        void Semilla(int x, int y)
        {
            if (!m[x, y] && !fuera[x, y]) { fuera[x, y] = true; cola.Enqueue(y * w + x); }
        }
        for (int x = 0; x < w; x++) { Semilla(x, 0); Semilla(x, h - 1); }
        for (int y = 0; y < h; y++) { Semilla(0, y); Semilla(w - 1, y); }
        var dx4 = new[] { 1, -1, 0, 0 };
        var dy4 = new[] { 0, 0, 1, -1 };
        while (cola.Count > 0)
        {
            int v = cola.Dequeue();
            int x = v % w, y = v / w;
            for (int k = 0; k < 4; k++)
            {
                int nx = x + dx4[k], ny = y + dy4[k];
                if (nx >= 0 && ny >= 0 && nx < w && ny < h && !m[nx, ny] && !fuera[nx, ny])
                {
                    fuera[nx, ny] = true;
                    cola.Enqueue(ny * w + nx);
                }
            }
        }
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                if (!fuera[x, y]) m[x, y] = true;
    }

    /// <summary>Deja solo las piezas grandes (al menos 15 % de la mayor). Falla si no hay ninguna.</summary>
    private static bool ConservarPiezasGrandes(bool[,] m, int w, int h)
    {
        var etiqueta = new int[w, h];
        var tamanos = new List<int> { 0 };
        var cola = new Queue<int>();
        int actual = 0;
        for (int y0 = 0; y0 < h; y0++)
            for (int x0 = 0; x0 < w; x0++)
            {
                if (!m[x0, y0] || etiqueta[x0, y0] != 0) continue;
                actual++;
                int tam = 0;
                etiqueta[x0, y0] = actual;
                cola.Enqueue(y0 * w + x0);
                while (cola.Count > 0)
                {
                    int v = cola.Dequeue();
                    int x = v % w, y = v / w;
                    tam++;
                    for (int dy = -1; dy <= 1; dy++)
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            int nx = x + dx, ny = y + dy;
                            if (nx >= 0 && ny >= 0 && nx < w && ny < h && m[nx, ny] && etiqueta[nx, ny] == 0)
                            {
                                etiqueta[nx, ny] = actual;
                                cola.Enqueue(ny * w + nx);
                            }
                        }
                }
                tamanos.Add(tam);
            }
        if (actual == 0) return false;

        int mayor = tamanos.Max();
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                m[x, y] = etiqueta[x, y] != 0 && tamanos[etiqueta[x, y]] >= 0.15 * mayor;
        return true;
    }

    // ------------------------------------------------------------------
    //  Color
    // ------------------------------------------------------------------
    private static double[] Realzar(double r, double g, double b)
    {
        RgbAHsv(r / 255.0, g / 255.0, b / 255.0, out double hh, out double ss, out double vv);
        ss = Math.Min(1.0, ss * 1.3);
        vv = Math.Min(1.0, Math.Max(0.0, (vv - 0.5) * 1.1 + 0.5));
        HsvARgb(hh, ss, vv, out double rr, out double gg, out double bb);
        return new[] { rr * 255.0, gg * 255.0, bb * 255.0 };
    }

    private static void RgbAHsv(double r, double g, double b, out double h, out double s, out double v)
    {
        double max = Math.Max(r, Math.Max(g, b));
        double min = Math.Min(r, Math.Min(g, b));
        double d = max - min;
        v = max;
        s = max <= 0 ? 0 : d / max;
        if (d <= 0) { h = 0; return; }
        if (max == r) h = ((g - b) / d) % 6.0;
        else if (max == g) h = (b - r) / d + 2.0;
        else h = (r - g) / d + 4.0;
        h /= 6.0;
        if (h < 0) h += 1.0;
    }

    private static void HsvARgb(double h, double s, double v, out double r, out double g, out double b)
    {
        double c = v * s;
        double hh = h * 6.0;
        double x = c * (1 - Math.Abs(hh % 2.0 - 1));
        double m = v - c;
        double r1, g1, b1;
        if (hh < 1) { r1 = c; g1 = x; b1 = 0; }
        else if (hh < 2) { r1 = x; g1 = c; b1 = 0; }
        else if (hh < 3) { r1 = 0; g1 = c; b1 = x; }
        else if (hh < 4) { r1 = 0; g1 = x; b1 = c; }
        else if (hh < 5) { r1 = x; g1 = 0; b1 = c; }
        else { r1 = c; g1 = 0; b1 = x; }
        r = r1 + m; g = g1 + m; b = b1 + m;
    }

    /// <summary>Paleta de n colores (k-means; arranca repartiendo los colores por luminosidad).</summary>
    private static List<double[]> KMeans(List<double[]> colores, int n)
    {
        var orden = colores.OrderBy(c => c[0] + c[1] + c[2]).ToList();
        var centros = new List<double[]>();
        for (int i = 0; i < n; i++)
        {
            int idx = n == 1 ? 0 : (int)Math.Round(i * (orden.Count - 1) / (double)(n - 1));
            centros.Add(new[] { orden[idx][0], orden[idx][1], orden[idx][2] });
        }

        for (int it = 0; it < 6; it++)
        {
            var suma = new double[n, 3];
            var cuenta = new int[n];
            foreach (var c in colores)
            {
                int mejor = 0;
                double mejorD = double.MaxValue;
                for (int k = 0; k < n; k++)
                {
                    double dr = c[0] - centros[k][0], dg = c[1] - centros[k][1], db = c[2] - centros[k][2];
                    double d = dr * dr + dg * dg + db * db;
                    if (d < mejorD) { mejorD = d; mejor = k; }
                }
                suma[mejor, 0] += c[0]; suma[mejor, 1] += c[1]; suma[mejor, 2] += c[2]; cuenta[mejor]++;
            }
            for (int k = 0; k < n; k++)
                if (cuenta[k] > 0)
                    centros[k] = new[] { suma[k, 0] / cuenta[k], suma[k, 1] / cuenta[k], suma[k, 2] / cuenta[k] };
        }
        return centros;
    }
}
