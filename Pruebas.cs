namespace AgendaContactosWF;

/// <summary>Pruebas automáticas: botón «Ejecutar pruebas» del formulario</summary>
internal static class Pruebas
{
    private static int fallos;
    private static System.Text.StringBuilder salida = new();

    // Un TextBox multilínea de WinForms necesita \r\n: se normalizan los saltos.
    private static void Linea(string texto) => salida.AppendLine(texto.Replace("\n", Environment.NewLine));

    /// <summary>Ejecuta la batería completa y devuelve (todas pasaron, reporte en texto).</summary>
    public static (bool Exito, string Reporte) Ejecutar()
    {
        fallos = 0;
        salida = new System.Text.StringBuilder();
        Linea("═══ Pruebas de la Agenda de Contactos ═══\n");

        ArbolContactos agenda = ConstruirAgendaEjemplo();

        Linea("── Recorridos iterativos (pila enlazada propia) ──");
        Secuencia("Preorden", Apellidos(agenda.Preorden), "Martínez García Díaz Hernández Ortiz Núñez Ruiz");
        Secuencia("Inorden", Apellidos(agenda.Inorden), "Díaz García Hernández Martínez Núñez Ortiz Ruiz");
        Secuencia("Postorden", Apellidos(agenda.Postorden), "Díaz Hernández García Núñez Ruiz Ortiz Martínez");
        Verificar("Altura = 3", agenda.Altura() == 3);
        Verificar("Invariante BST", agenda.VerificarInvariante());

        Linea("\n── Orden alfabético en español ──");
        Verificar("N < Ñ < O  (NUNEZ < NUÑEZ < NUOZ)",
            Texto.Comparar("NUNEZ", "NUÑEZ") < 0 && Texto.Comparar("NUÑEZ", "NUOZ") < 0);
        Verificar("Apellido corto primero: 'PEREZ, ANA' < 'PEREZ GARCIA, ANA'",
            Texto.Comparar("PEREZ, ANA", "PEREZ GARCIA, ANA") < 0);

        Linea("\n── Búsqueda ──");
        Verificar("Buscar ('hernandez', 'ELENA') sin acentos ni mayúsculas",
            agenda.Buscar("hernandez", "ELENA") is not null);
        Verificar("Buscar inexistente devuelve null", agenda.Buscar("López", "Ana") is null);
        Verificar("Buscar con null devuelve null", agenda.Buscar(null, "Ana") is null);
        Verificar("Búsqueda inversa por teléfono '+52 (81) 2222-3333'",
            agenda.BuscarPorTelefono("+52 (81) 2222-3333")?.Apellidos == "García");

        Linea("\n── Duplicados ──");
        Verificar("('  martinez ', 'luis') rechazado",
            !agenda.Insertar(new Contacto("luis", "  martinez ", "5599998888")));
        Verificar("Cantidad sigue en 7", agenda.Cantidad == 7);

        Linea("\n── Validación de datos ──");
        Verificar("Teléfono '+52 (55) 1234-5678' → 5512345678",
            Texto.NormalizarTelefono("+52 (55) 1234-5678") == "5512345678");
        Verificar("Teléfono '12345' inválido", Texto.NormalizarTelefono("12345") is null);
        Verificar("Teléfono con letras inválido", Texto.NormalizarTelefono("55-ABCD-5678") is null);
        Verificar("Correo 'ana@correo.com' válido", Texto.EsCorreoValido("ana@correo.com"));
        Verificar("Correo 'ana@@correo.com' inválido", !Texto.EsCorreoValido("ana@@correo.com"));
        Verificar("Correo 'ana@correo' inválido", !Texto.EsCorreoValido("ana@correo"));
        Verificar("Nombre con coma inválido", !Texto.EsNombreValido("Ana, Luisa"));
        Verificar("Contacto con teléfono inválido lanza excepción",
            Lanza<ArgumentException>(() => new Contacto("Ana", "López", "123")));
        Verificar("Insertar null lanza ArgumentNullException",
            Lanza<ArgumentNullException>(() => agenda.Insertar(null!)));

        Linea("\n── Autocompletar por prefijo O(h+k) ──");
        agenda.Insertar(new Contacto("Rosa", "Martín", "5511112222"));
        agenda.Insertar(new Contacto("Jorge", "Marín", "5533334444"));
        Secuencia("Prefijo 'mar'", Apellidos(agenda.BuscarPorPrefijo("mar")), "Marín Martín Martínez");
        Secuencia("Prefijo 'mart'", Apellidos(agenda.BuscarPorPrefijo("mart")), "Martín Martínez");
        Secuencia("Prefijo 'Martínez, L'", Apellidos(agenda.BuscarPorPrefijo("Martínez, L")), "Martínez");
        Secuencia("Prefijo 'xyz'", Apellidos(agenda.BuscarPorPrefijo("xyz")), "");
        Verificar("Eliminar Martín y Marín",
            agenda.Eliminar("Martín", "Rosa") && agenda.Eliminar("Marín", "Jorge"));

        Linea("\n── Renombrar (cambia la clave ⇒ eliminar + insertar) ──");
        Contacto ruiz = agenda.Buscar("Ruiz", "Teresa")!;
        Verificar("Renombrar a nombre ya existente se rechaza",
            agenda.Renombrar(ruiz, "Luis", "Martínez") == ResultadoRenombrar.NuevoNombreDuplicado);
        Verificar("Renombrar Ruiz → Ruiz Soto conserva teléfono",
            agenda.Renombrar(ruiz, "Teresa", "Ruiz Soto") == ResultadoRenombrar.Renombrado
            && agenda.Buscar("Ruiz Soto", "Teresa")?.Telefono == ruiz.Telefono
            && agenda.Buscar("Ruiz", "Teresa") is null);
        Verificar("Renombrar Ruiz Soto → Ruiz (restaurar)",
            agenda.Renombrar(agenda.Buscar("Ruiz Soto", "Teresa")!, "Teresa", "Ruiz") == ResultadoRenombrar.Renombrado);
        Verificar("Invariante BST", agenda.VerificarInvariante());

        Linea("\n── Eliminación: hoja (Díaz) ──");
        Verificar("Eliminar Díaz", agenda.Eliminar("Díaz", "Carlos"));
        Secuencia("Inorden", Apellidos(agenda.Inorden), "García Hernández Martínez Núñez Ortiz Ruiz");

        Linea("\n── Eliminación: un hijo (García) ──");
        Verificar("Eliminar García", agenda.Eliminar("garcia", "ana"));
        Secuencia("Preorden", Apellidos(agenda.Preorden), "Martínez Hernández Ortiz Núñez Ruiz");
        Verificar("Invariante BST", agenda.VerificarInvariante());

        Linea("\n── Eliminación: dos hijos, raíz (Martínez; sucesor Núñez no es hijo directo) ──");
        Verificar("Eliminar Martínez", agenda.Eliminar("Martínez", "Luis"));
        Secuencia("Preorden", Apellidos(agenda.Preorden), "Núñez Hernández Ortiz Ruiz");
        Verificar("Invariante BST", agenda.VerificarInvariante());

        Linea("\n── Eliminación: dos hijos, raíz (Núñez; sucesor Ortiz es hijo directo) ──");
        Verificar("Eliminar Núñez", agenda.Eliminar("Nuñez", "Pedro"));
        Secuencia("Preorden", Apellidos(agenda.Preorden), "Ortiz Hernández Ruiz");
        Secuencia("Postorden", Apellidos(agenda.Postorden), "Hernández Ruiz Ortiz");
        Verificar("Eliminar inexistente devuelve false", !agenda.Eliminar("Núñez", "Pedro"));
        Verificar("Cantidad = 3", agenda.Cantidad == 3);

        Linea("\n── Árbol degenerado (1 000 inserciones ordenadas, sin recursión) ──");
        var lista = new ArbolContactos();
        for (int i = 0; i < 1000; i++)
            lista.Insertar(new Contacto("Ana", "Apellido " + NumeroALetras(i), "55" + (10_000_000 + i)));
        int visitados = 0;
        lista.Postorden(_ => visitados++);
        Verificar("Altura = 1000 (lista enlazada) y Postorden visita los 1000 nodos",
            lista.Altura() == 1000 && visitados == 1000);

        Linea(fallos == 0 ? "\n✔ Todas las pruebas pasaron." : $"\n✘ {fallos} prueba(s) fallaron.");
        return (fallos == 0, salida.ToString());
    }

    public static ArbolContactos ConstruirAgendaEjemplo()
    {
        var agenda = new ArbolContactos();
        agenda.Insertar(new Contacto("Luis", "Martínez", "55 1111 2222", "luis.mtz@correo.com"));
        agenda.Insertar(new Contacto("Ana", "García", "81 2222 3333", "ana.garcia@correo.com"));
        agenda.Insertar(new Contacto("Sofía", "Ortiz", "33 4444 5555"));
        agenda.Insertar(new Contacto("Carlos", "Díaz", "55 6666 7777", "carlos@correo.mx"));
        agenda.Insertar(new Contacto("Elena", "Hernández", "866 123 4567"));
        agenda.Insertar(new Contacto("Pedro", "Núñez", "222 987 6543", "pnunez@correo.com"));
        agenda.Insertar(new Contacto("Teresa", "Ruiz", "999 111 2233"));
        return agenda;
    }

    // Genera "AAA", "AAB", ... para obtener claves en orden alfabético estricto.
    private static string NumeroALetras(int n)
    {
        char[] letras = new char[3];
        for (int i = 2; i >= 0; i--)
        {
            letras[i] = (char)('A' + n % 26);
            n /= 26;
        }
        return new string(letras);
    }

    private static string Apellidos(Action<Action<Contacto>> recorrido)
    {
        var lista = new List<string>();
        recorrido(c => lista.Add(c.Apellidos));
        return string.Join(" ", lista);
    }

    private static string Apellidos(List<Contacto> contactos)
    {
        var lista = new List<string>();
        foreach (Contacto c in contactos)
            lista.Add(c.Apellidos);
        return string.Join(" ", lista);
    }

    private static void Secuencia(string etiqueta, string obtenido, string esperado)
    {
        bool ok = obtenido == esperado;
        if (!ok) fallos++;
        Linea($"{(ok ? "[OK]   " : "[FALLO]")} {etiqueta}: {obtenido}"
                          + (ok ? "" : $"   (esperado: {esperado})"));
    }

    private static void Verificar(string descripcion, bool condicion)
    {
        if (!condicion) fallos++;
        Linea($"{(condicion ? "[OK]   " : "[FALLO]")} {descripcion}");
    }

    private static bool Lanza<TExcepcion>(Action accion) where TExcepcion : Exception
    {
        try { accion(); return false; }
        catch (TExcepcion) { return true; }
    }
}
