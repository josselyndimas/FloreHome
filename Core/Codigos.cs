using System.Globalization;
using System.Text;

namespace FloreHome.Core;

/// <summary>Códigos de producto automáticos: primeras 3 letras del nombre + número (TUL-0001).</summary>
public static class Codigos
{
    public static string Prefijo(string nombre)
    {
        var letras = new StringBuilder();
        string normal = (nombre ?? "").Normalize(NormalizationForm.FormD);
        foreach (char c in normal)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark) continue;   // quita tildes
            if (char.IsLetter(c)) letras.Append(char.ToUpperInvariant(c));
            if (letras.Length == 3) break;
        }
        if (letras.Length == 0) return "PRD";
        while (letras.Length < 3) letras.Append('X');
        return letras.ToString();
    }

    public static string Siguiente(string prefijo)
    {
        int ultimo = Db.Contar(
            "SELECT COALESCE(MAX(CAST(SUBSTRING(Codigo, 5) AS UNSIGNED)), 0) FROM Producto WHERE Codigo LIKE @p",
            ("@p", prefijo + "-%"));
        return prefijo + "-" + (ultimo + 1).ToString("D4");
    }

    public static string Generar(string nombre) => Siguiente(Prefijo(nombre));
}
