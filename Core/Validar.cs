using System.Text.RegularExpressions;

namespace FloreHome.Core;

/// <summary>Validaciones y filtros de escritura para los campos de texto.</summary>
public static class Validar
{
    private static readonly Regex RxNombre = new Regex(@"^\p{L}[\p{L} .'\-]*$");
    private static readonly Regex RxUsuario = new Regex(@"^[A-Za-z0-9_.]{3,30}$");
    private static readonly Regex RxTelefono = new Regex(@"^\d{4}-\d{4}$");
    private static readonly Regex RxEmail = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");

    // ---------- Comprobaciones al guardar ----------
    public static bool NombreValido(string t) => !string.IsNullOrWhiteSpace(t) && RxNombre.IsMatch(t.Trim());
    public static bool UsuarioValido(string t) => RxUsuario.IsMatch(t ?? "");
    public static bool TelefonoValido(string t) => RxTelefono.IsMatch(t ?? "");
    public static bool EmailValido(string t) => RxEmail.IsMatch(t ?? "");

    // ---------- Filtros mientras se escribe ----------
    private static bool LetraPermitida(char c) =>
        char.IsLetter(c) || c == ' ' || c == '\'' || c == '-' || c == '.';

    private static bool UsuarioPermitido(char c) =>
        (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == '_' || c == '.';

    private static void Limpiar(TextBox tb, Func<char, bool> permitido)
    {
        string limpio = new string(tb.Text.Where(permitido).ToArray());
        if (limpio != tb.Text)
        {
            int pos = tb.SelectionStart;
            tb.Text = limpio;
            tb.SelectionStart = Math.Min(pos, tb.Text.Length);
        }
    }

    /// <summary>Solo letras, espacios, punto, apóstrofe y guion (nada de números).</summary>
    public static void SoloLetras(TextBox tb)
    {
        tb.KeyPress += (s, e) =>
        {
            if (!char.IsControl(e.KeyChar) && !LetraPermitida(e.KeyChar)) e.Handled = true;
        };
        tb.TextChanged += (s, e) => Limpiar(tb, LetraPermitida);
    }

    /// <summary>Letras, números, guion bajo y punto (para el nombre de usuario).</summary>
    public static void SoloUsuario(TextBox tb)
    {
        tb.KeyPress += (s, e) =>
        {
            if (!char.IsControl(e.KeyChar) && !UsuarioPermitido(e.KeyChar)) e.Handled = true;
        };
        tb.TextChanged += (s, e) => Limpiar(tb, UsuarioPermitido);
    }

    /// <summary>Teléfono de El Salvador: solo 8 números; el guion se pone solo (1234-5678).</summary>
    public static void Telefono(TextBox tb)
    {
        tb.MaxLength = 9;
        bool ocupado = false;
        tb.KeyPress += (s, e) =>
        {
            if (!char.IsControl(e.KeyChar) && !(e.KeyChar >= '0' && e.KeyChar <= '9')) e.Handled = true;
        };
        tb.TextChanged += (s, e) =>
        {
            if (ocupado) return;
            ocupado = true;
            string digitos = new string(tb.Text.Where(c => c >= '0' && c <= '9').Take(8).ToArray());
            string formato = digitos.Length > 4 ? digitos.Substring(0, 4) + "-" + digitos.Substring(4) : digitos;
            if (formato != tb.Text) tb.Text = formato;
            tb.SelectionStart = tb.Text.Length;
            ocupado = false;
        };
    }
}
