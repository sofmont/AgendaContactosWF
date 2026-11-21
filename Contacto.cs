namespace AgendaContactosWF;

/// <summary>
/// Contacto de la agenda. La clave del BST es "APELLIDOS, NOMBRE" normalizada.
/// Nombre y apellidos son inmutables a propósito: si la clave de un nodo cambiara
/// mientras está dentro del árbol, se rompería la invariante de orden. Para renombrar
/// se elimina y se vuelve a insertar (ver <see cref="ArbolContactos.Renombrar"/>).
/// </summary>
public sealed class Contacto
{
    public const int LongitudMaxima = 40;

    public string Nombre { get; }
    public string Apellidos { get; }
    public string Clave { get; }
    public string Telefono { get; private set; }   // 10 dígitos sin separadores
    public string? Correo { get; private set; }

    public Contacto(string nombre, string apellidos, string telefono, string? correo = null)
    {
        if (!Texto.EsNombreValido(nombre))
            throw new ArgumentException("Nombre inválido.", nameof(nombre));
        if (!Texto.EsNombreValido(apellidos))
            throw new ArgumentException("Apellidos inválidos.", nameof(apellidos));

        Nombre = Texto.LimpiarEspacios(nombre);
        Apellidos = Texto.LimpiarEspacios(apellidos);
        Clave = CrearClave(apellidos, nombre);
        Telefono = Texto.NormalizarTelefono(telefono)
                   ?? throw new ArgumentException("Teléfono inválido (se esperan 10 dígitos).", nameof(telefono));
        ActualizarCorreo(correo);
    }

    public static string CrearClave(string apellidos, string nombre) =>
        Texto.Normalizar(apellidos) + ", " + Texto.Normalizar(nombre);

    public void ActualizarTelefono(string telefono)
    {
        Telefono = Texto.NormalizarTelefono(telefono)
                   ?? throw new ArgumentException("Teléfono inválido.", nameof(telefono));
    }

    /// <summary>Cadena vacía o null elimina el correo.</summary>
    public void ActualizarCorreo(string? correo)
    {
        if (string.IsNullOrWhiteSpace(correo))
        {
            Correo = null;
            return;
        }
        if (!Texto.EsCorreoValido(correo))
            throw new ArgumentException("Correo electrónico inválido.", nameof(correo));
        Correo = correo.Trim().ToLowerInvariant();
    }

    public string NombreCompleto => $"{Apellidos}, {Nombre}";

    public override string ToString() =>
        $"{NombreCompleto,-34} {Texto.FormatearTelefono(Telefono),-14} {Correo ?? "—"}";
}
