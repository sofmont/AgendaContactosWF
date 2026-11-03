using System.Net.Mail;
using System.Text;

namespace AgendaContactosWF;

/// <summary>Normalización, comparación y validación de los datos de un contacto.</summary>
internal static class Texto
{
    public static string LimpiarEspacios(string texto)
    {
        ArgumentNullException.ThrowIfNull(texto);
        return string.Join(' ', texto.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
    }

    /// <summary>Mayúsculas, sin acentos, espacios limpios. Conserva la Ñ.</summary>
    public static string Normalizar(string texto)
    {
        string limpio = LimpiarEspacios(texto).ToUpperInvariant();
        var sb = new StringBuilder(limpio.Length);
        foreach (char c in limpio)
        {
            sb.Append(c switch
            {
                'Á' or 'À' or 'Ä' or 'Â' => 'A',
                'É' or 'È' or 'Ë' or 'Ê' => 'E',
                'Í' or 'Ì' or 'Ï' or 'Î' => 'I',
                'Ó' or 'Ò' or 'Ö' or 'Ô' => 'O',
                'Ú' or 'Ù' or 'Ü' or 'Û' => 'U',
                _ => c
            });
        }
        return sb.ToString();
    }

    /// <summary>
    /// Orden de directorio telefónico en español:
    ///   • la coma separadora pesa menos que cualquier letra, así
    ///     "PÉREZ, ANA" &lt; "PÉREZ GARCÍA, ANA" (el apellido corto va primero);
    ///   • la Ñ va entre la N y la O.
    /// Es un orden lexicográfico sobre un alfabeto totalmente ordenado ⇒ orden total estricto.
    /// </summary>
    public static int Comparar(string a, string b)
    {
        int n = Math.Min(a.Length, b.Length);
        for (int i = 0; i < n; i++)
        {
            int pa = Peso(a[i]);
            int pb = Peso(b[i]);
            if (pa != pb)
                return pa < pb ? -1 : 1;
        }
        return a.Length.CompareTo(b.Length);
    }

    private static int Peso(char c) => c switch
    {
        ',' => 0,
        'Ñ' => 'N' * 2 + 3,
        _ => c * 2 + 2
    };

    // ───────────── Validaciones ─────────────

    /// <summary>Letras, espacios, apóstrofo, guion y punto; al menos una letra; sin comas.</summary>
    public static bool EsNombreValido(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto) || texto.Trim().Length > Contacto.LongitudMaxima)
            return false;

        bool tieneLetra = false;
        foreach (char c in texto)
        {
            if (char.IsLetter(c))
                tieneLetra = true;
            else if (c is not (' ' or '\'' or '-' or '.'))
                return false;
        }
        return tieneLetra;
    }

    /// <summary>
    /// Devuelve los 10 dígitos de un teléfono mexicano o null si no es válido.
    /// Acepta separadores ( ) - . y espacios, y el prefijo de país +52.
    /// </summary>
    public static string? NormalizarTelefono(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
            return null;

        var digitos = new StringBuilder();
        foreach (char c in texto.Trim())
        {
            if (c >= '0' && c <= '9')
                digitos.Append(c);
            else if (c is not (' ' or '-' or '(' or ')' or '.' or '+'))
                return null;
        }

        string d = digitos.ToString();
        if (d.Length == 12 && d.StartsWith("52", StringComparison.Ordinal))
            d = d.Substring(2);

        return d.Length == 10 && d[0] != '0' ? d : null;
    }

    public static string FormatearTelefono(string diezDigitos) =>
        $"{diezDigitos.Substring(0, 2)} {diezDigitos.Substring(2, 4)} {diezDigitos.Substring(6, 4)}";

    public static bool EsCorreoValido(string? correo)
    {
        if (string.IsNullOrWhiteSpace(correo))
            return false;

        correo = correo.Trim();
        if (correo.Length > 254 || correo.Contains(' ') || correo.Contains(".."))
            return false;

        int arroba = correo.IndexOf('@');
        if (arroba <= 0 || arroba != correo.LastIndexOf('@'))
            return false;

        string dominio = correo.Substring(arroba + 1);
        int punto = dominio.LastIndexOf('.');
        if (punto <= 0 || punto == dominio.Length - 1)
            return false;

        return MailAddress.TryCreate(correo, out MailAddress? direccion)
               && direccion.Address == correo;
    }
}
