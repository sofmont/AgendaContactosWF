namespace AgendaContactosWF;

/// <summary>
/// Programa 2 — Agenda de Contactos ordenada (Windows Forms).
/// El formulario valida y presenta; el ordenamiento y las búsquedas viven en <see cref="ArbolContactos"/>.
/// </summary>
public partial class FormAgenda : Form
{
    private readonly ArbolContactos agenda = new();
    private readonly ErrorProvider errores = new() { BlinkStyle = ErrorBlinkStyle.NeverBlink };
    private Contacto? seleccionado;

    public FormAgenda()
    {
        ConstruirInterfaz();
        RefrescarVistas();
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        division.SplitterDistance = Math.Max(120, division.Height * 45 / 100);
    }

    // ═══════════════════════ Validación ═══════════════════════

    private bool Validar(TextBox caja, bool valido, string mensaje)
    {
        errores.SetError(caja, valido ? string.Empty : mensaje);
        return valido;
    }

    private bool TryLeerNombres(out string nombre, out string apellidos)
    {
        nombre = txtNombre.Text.Trim();
        apellidos = txtApellidos.Text.Trim();
        bool okNombre = Validar(txtNombre, Texto.EsNombreValido(nombre),
            "Solo letras, espacios, ' - . (sin comas).");
        bool okApellidos = Validar(txtApellidos, Texto.EsNombreValido(apellidos),
            "Solo letras, espacios, ' - . (sin comas).");
        if (!okNombre) txtNombre.Focus();
        else if (!okApellidos) txtApellidos.Focus();
        return okNombre && okApellidos;
    }

    private bool TryLeerTelefono(TextBox caja, out string telefono)
    {
        telefono = caja.Text.Trim();
        bool ok = Validar(caja, Texto.NormalizarTelefono(telefono) is not null,
            "10 dígitos, p. ej. 55 1234 5678 o +52 (81) 1234-5678.");
        if (!ok) caja.Focus();
        return ok;
    }

    /// <summary>Correo opcional: vacío ⇒ null (válido).</summary>
    private bool TryLeerCorreo(out string? correo)
    {
        string texto = txtCorreo.Text.Trim();
        correo = texto.Length == 0 ? null : texto;
        bool ok = Validar(txtCorreo, correo is null || Texto.EsCorreoValido(correo),
            "Correo inválido, p. ej. ana.lopez@correo.com (o déjelo vacío).");
        if (!ok) txtCorreo.Focus();
        return ok;
    }

    // ═══════════════════════ Operaciones del BST ═══════════════════════

    private void BtnAgregar_Click(object? sender, EventArgs e)
    {
        if (!TryLeerNombres(out string nombre, out string apellidos)) return;
        if (!TryLeerTelefono(txtTelefono, out string telefono)) return;
        if (!TryLeerCorreo(out string? correo)) return;

        if (agenda.Buscar(apellidos, nombre) is not null)          // O(h)
        {
            Aviso("Ya existe un contacto con ese nombre y apellidos.", "Duplicado");
            return;
        }

        Contacto? mismoNumero = agenda.BuscarPorTelefono(telefono); // O(n)
        if (mismoNumero is not null &&
            !Confirmar($"Ese número ya pertenece a {mismoNumero.NombreCompleto}. ¿Guardarlo de todos modos?"))
            return;

        Ejecutar(() =>
        {
            var contacto = new Contacto(nombre, apellidos, telefono, correo);
            agenda.Insertar(contacto);                              // O(h)
            Seleccionar(contacto);
            Estado($"✔ {contacto.NombreCompleto} agregado.");
            RefrescarVistas();
        });
    }

    private void BtnBuscar_Click(object? sender, EventArgs e)
    {
        if (!TryLeerNombres(out string nombre, out string apellidos)) return;

        Contacto? c = agenda.Buscar(apellidos, nombre);             // O(h)
        if (c is null)
        {
            Estado($"No existe «{apellidos}, {nombre}». Pruebe escribiendo el apellido en «Consultas».");
            Seleccionar(null);
            RefrescarVistas();
            return;
        }

        CargarEnCampos(c);
        Seleccionar(c);
        Estado($"✔ {c}");
        RefrescarVistas();
    }

    private void BtnEliminar_Click(object? sender, EventArgs e)
    {
        if (!TryLeerNombres(out string nombre, out string apellidos)) return;

        Contacto? c = agenda.Buscar(apellidos, nombre);
        if (c is null)
        {
            Aviso("No existe ese contacto.", "Eliminar");
            return;
        }
        if (!Confirmar($"¿Eliminar a {c.NombreCompleto}?")) return;

        agenda.Eliminar(c.Apellidos, c.Nombre);                     // O(h)
        if (ReferenceEquals(seleccionado, c)) Seleccionar(null);
        LimpiarCampos();
        Estado($"✔ {c.NombreCompleto} eliminado.");
        RefrescarVistas();
    }

    /// <summary>Teléfono y correo no son parte de la clave: se editan en sitio, sin reestructurar el árbol.</summary>
    private void BtnGuardarDatos_Click(object? sender, EventArgs e)
    {
        if (!SeleccionVigente(out Contacto c)) return;
        if (!TryLeerTelefono(txtTelefono, out string telefono)) return;
        if (!TryLeerCorreo(out string? correo)) return;

        Ejecutar(() =>
        {
            c.ActualizarTelefono(telefono);   // ambos ya validados: no puede quedar a medias
            c.ActualizarCorreo(correo);
            Seleccionar(c);
            Estado($"✔ Datos actualizados: {c}");
            RefrescarVistas();
        });
    }

    /// <summary>Cambiar nombre o apellidos cambia la clave ⇒ eliminar + insertar (O(h)).</summary>
    private void BtnRenombrar_Click(object? sender, EventArgs e)
    {
        if (!SeleccionVigente(out Contacto c)) return;
        if (!TryLeerNombres(out string nombre, out string apellidos)) return;

        Ejecutar(() =>
        {
            ResultadoRenombrar r = agenda.Renombrar(c, nombre, apellidos);
            switch (r)
            {
                case ResultadoRenombrar.Renombrado:
                    Seleccionar(agenda.Buscar(apellidos, nombre));
                    Estado("✔ Contacto renombrado y reubicado en el árbol.");
                    break;
                case ResultadoRenombrar.NuevoNombreDuplicado:
                    Aviso("Ya existe otro contacto con ese nombre y apellidos.", "Duplicado");
                    break;
                default:
                    Seleccionar(null);
                    Aviso("El contacto seleccionado ya no existe.", "Renombrar");
                    break;
            }
            RefrescarVistas();
        });
    }

    // ═══════════════════════ Consultas ═══════════════════════

    /// <summary>Autocompletar en vivo: cada pulsación cuesta O(h + k).</summary>
    private void TxtPrefijo_TextChanged(object? sender, EventArgs e)
    {
        string prefijo = txtPrefijo.Text.Trim();
        if (prefijo.Length == 0)
        {
            MostrarResultados(new List<Contacto>());
            Estado("Listo.");
            return;
        }

        List<Contacto> lista = agenda.BuscarPorPrefijo(prefijo);
        MostrarResultados(lista);
        Estado($"{lista.Count} contacto(s) empiezan con «{prefijo}».");
    }

    private void BtnBuscarTelefono_Click(object? sender, EventArgs e)
    {
        if (!TryLeerTelefono(txtTelefonoBuscar, out string telefono)) return;

        Contacto? c = agenda.BuscarPorTelefono(telefono);           // O(n): el teléfono no es la clave
        if (c is null)
        {
            Estado($"Número desconocido: {Texto.FormatearTelefono(Texto.NormalizarTelefono(telefono)!)}");
            MostrarResultados(new List<Contacto>());
            return;
        }

        Seleccionar(c);
        CargarEnCampos(c);
        MostrarResultados(new List<Contacto> { c });
        Estado($"✔ Es de {c.NombreCompleto}.");
        panelArbol.Mostrar(agenda.ObtenerDiagrama(), c.Clave);
    }

    private void BtnCargarEjemplo_Click(object? sender, EventArgs e)
    {
        int agregados = 0;
        Pruebas.ConstruirAgendaEjemplo().Preorden(c =>
        {
            if (agenda.Insertar(c)) agregados++;
        });
        Estado($"✔ {agregados} contacto(s) de ejemplo agregados.");
        RefrescarVistas();
    }

    private void BtnVaciar_Click(object? sender, EventArgs e)
    {
        if (agenda.EstaVacia || !Confirmar("¿Borrar TODOS los contactos?")) return;
        agenda.Vaciar();
        Seleccionar(null);
        LimpiarCampos();
        MostrarResultados(new List<Contacto>());
        Estado("✔ Agenda vacía.");
        RefrescarVistas();
    }

    private void BtnPruebas_Click(object? sender, EventArgs e)
    {
        (bool exito, string reporte) = Pruebas.Ejecutar();
        txtPruebas.Text = reporte;
        lblResultadoPruebas.Text = exito ? "✔ Todas las pruebas pasaron" : "✘ Hay pruebas fallidas";
        lblResultadoPruebas.ForeColor = exito ? Color.ForestGreen : Color.Firebrick;
    }

    private void Lista_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (sender is ListView lv && lv.SelectedItems.Count == 1 && lv.SelectedItems[0].Tag is Contacto c)
        {
            CargarEnCampos(c);
            Seleccionar(c);
            panelArbol.Mostrar(agenda.ObtenerDiagrama(), c.Clave);
        }
    }

    // ═══════════════════════ Presentación ═══════════════════════

    private void RefrescarVistas()
    {
        panelArbol.Mostrar(agenda.ObtenerDiagrama(), seleccionado?.Clave);

        lvDirectorio.BeginUpdate();
        lvDirectorio.Items.Clear();
        ListViewGroup? grupo = null;
        lvDirectorio.Groups.Clear();
        agenda.Inorden(c =>
        {
            // Encabezado por letra inicial, como un directorio impreso.
            string inicial = c.Clave.Substring(0, 1);
            if (grupo is null || grupo.Header != inicial)
            {
                grupo = new ListViewGroup(inicial);
                lvDirectorio.Groups.Add(grupo);
            }
            ListViewItem fila = CrearFila(c);
            fila.Group = grupo;
            lvDirectorio.Items.Add(fila);
        });
        lvDirectorio.EndUpdate();

        txtPreorden.Text = Secuencia(agenda.Preorden);
        txtInorden.Text = Secuencia(agenda.Inorden);
        txtPostorden.Text = Secuencia(agenda.Postorden);

        int n = agenda.Cantidad;
        int h = agenda.Altura();
        int hMin = n == 0 ? 0 : (int)Math.Ceiling(Math.Log2(n + 1));
        lblMetricas.Text = $"n = {n}   |   altura h = {h}   |   altura mínima ⌈log₂(n+1)⌉ = {hMin}";
    }

    private void MostrarResultados(List<Contacto> lista)
    {
        lvResultados.BeginUpdate();
        lvResultados.Items.Clear();
        foreach (Contacto c in lista)
            lvResultados.Items.Add(CrearFila(c));
        lvResultados.EndUpdate();
        if (lista.Count > 0) pestanas.SelectedIndex = 1;
    }

    private static ListViewItem CrearFila(Contacto c) =>
        new(new[] { c.Apellidos, c.Nombre, Texto.FormatearTelefono(c.Telefono), c.Correo ?? "—" }) { Tag = c };

    private static string Secuencia(Action<Action<Contacto>> recorrido)
    {
        var nombres = new List<string>();
        recorrido(c => nombres.Add(c.NombreCompleto));
        return nombres.Count == 0 ? "(árbol vacío)" : string.Join("  →  ", nombres);
    }

    /// <summary>Comprueba que haya un contacto seleccionado y que siga dentro del árbol.</summary>
    private bool SeleccionVigente(out Contacto contacto)
    {
        contacto = seleccionado!;
        if (seleccionado is null || !ReferenceEquals(agenda.Buscar(seleccionado.Apellidos, seleccionado.Nombre), seleccionado))
        {
            Seleccionar(null);
            Aviso("Primero busque o elija un contacto de la lista.", "Sin selección");
            return false;
        }
        return true;
    }

    private void Seleccionar(Contacto? c)
    {
        seleccionado = c;
        lblSeleccion.Text = c is null
            ? "(ninguno)"
            : $"{c.NombreCompleto}\n{Texto.FormatearTelefono(c.Telefono)}  ·  {c.Correo ?? "sin correo"}";
        lblSeleccion.ForeColor = c is null ? Color.DimGray : Color.SteelBlue;
    }

    private void CargarEnCampos(Contacto c)
    {
        txtNombre.Text = c.Nombre;
        txtApellidos.Text = c.Apellidos;
        txtTelefono.Text = Texto.FormatearTelefono(c.Telefono);
        txtCorreo.Text = c.Correo ?? string.Empty;
        errores.Clear();
    }

    private void LimpiarCampos()
    {
        txtNombre.Clear();
        txtApellidos.Clear();
        txtTelefono.Clear();
        txtCorreo.Clear();
        errores.Clear();
        txtNombre.Focus();
    }

    private void Ejecutar(Action accion)
    {
        try
        {
            accion();
        }
        catch (ArgumentException ex)
        {
            Aviso(ex.Message, "Dato inválido");
        }
    }

    private void Estado(string mensaje) => lblEstado.Text = mensaje;

    private static void Aviso(string mensaje, string titulo) =>
        MessageBox.Show(mensaje, titulo, MessageBoxButtons.OK, MessageBoxIcon.Warning);

    private static bool Confirmar(string mensaje) =>
        MessageBox.Show(mensaje, "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;
}
