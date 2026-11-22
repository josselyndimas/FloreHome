namespace FloreHome.Ui;

public static class Recursos
{
    /// <summary>Flor en pixel art del logo (256x256, fondo transparente).</summary>
    public static Bitmap Logo()
    {
        using var s = typeof(Recursos).Assembly.GetManifestResourceStream("flor.png");
        if (s == null) return null;
        using var img = new Bitmap(s);
        return new Bitmap(img);
    }
}
