using System.Globalization;
using System.Text;

namespace FloreHome.Core;

/// <summary>
/// Códigos de producto automáticos. Se arman con la primera y la última palabra del nombre:
///   Tulipan          → TUL-0001
///   Rosas Blancas    → ROS-BLA-0001
///   Rosas Rojas      → ROS-ROJ-0001
///   Ramo de girasoles→ RAM-GIR-0001
/// El número sigue contando dentro de cada prefijo.
/// </summary>
public static class Codigos
{
    private static readonly HashSet<string> Relleno = new HashSet<string>
    {
        "de", "del", "la", "el", "los", "las", "con", "y", "en", "para", "un", "una", "por"
    };

    private static List<string> Palabras(string nombre)
    {
        var palabras = new List<string>();
        var actual = new StringBuilder();
        string normal = (nombre ?? "").Normalize(NormalizationForm.FormD);
        foreach (char c in normal)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark) continue;   // quita tildes
            if (char.IsLetter(c))
            {
                actual.Append(char.ToUpperInvariant(c));
            }
            else if (actual.Length > 0)
            {
                palabras.Add(actual.ToString());
                actual.Clear();
            }
        }
        if (actual.Length > 0) palabras.Add(actual.ToString());
        return palabras.Where(p => !Relleno.Contains(p.ToLowerInvariant())).ToList();
    }

    private static string TresLetras(string palabra)
    {
        string p = palabra.Length > 3 ? palabra.Substring(0, 3) : palabra;
        return p.PadRight(3, 'X');
    }

    public static string Prefijo(string nombre)
    {
        var palabras = Palabras(nombre);
        if (palabras.Count == 0) return "PRD";
        if (palabras.Count == 1) return TresLetras(palabras[0]);
        return TresLetras(palabras[0]) + "-" + TresLetras(palabras[palabras.Count - 1]);
    }

    public static string Siguiente(string prefijo)
    {
        int ultimo = Db.Contar(
            "SELECT COALESCE(MAX(CAST(SUBSTRING_INDEX(Codigo, '-', -1) AS UNSIGNED)), 0) FROM Producto WHERE Codigo REGEXP @re",
            ("@re", "^" + prefijo + "-[0-9]{4}$"));
        return prefijo + "-" + (ultimo + 1).ToString("D4");
    }

    public static string Generar(string nombre) => Siguiente(Prefijo(nombre));
}
