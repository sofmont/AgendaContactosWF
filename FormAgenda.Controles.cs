namespace AgendaContactosWF;

/// <summary>Construcción de la interfaz por código (sin archivo .Designer).</summary>
public partial class FormAgenda
{
    // ── Captura ──
    private TextBox txtNombre = null!;
    private TextBox txtApellidos = null!;
    private TextBox txtTelefono = null!;
    private TextBox txtCorreo = null!;

    // ── Consultas ──
    private TextBox txtPrefijo = null!;
    private TextBox txtTelefonoBuscar = null!;

    // ── Vistas ──
    private SplitContainer division = null!;
    private PanelArbol panelArbol = null!;
    private TabControl pestanas = null!;
    private ListView lvDirectorio = null!;
    private ListView lvResultados = null!;
    private TextBox txtPreorden = null!;
    private TextBox txtInorden = null!;
    private TextBox txtPostorden = null!;
    private TextBox txtPruebas = null!;
    private Label lblResultadoPruebas = null!;
    private Label lblSeleccion = null!;
    private ToolStripStatusLabel lblEstado = null!;
    private ToolStripStatusLabel lblMetricas = null!;

    private void ConstruirInterfaz()
    {
        SuspendLayout();
        Text = "Agenda de Contactos — Árbol Binario de Búsqueda";
        Font = new Font("Segoe UI", 9.5f);
        ClientSize = new Size(1220, 780);
        MinimumSize = new Size(980, 660);
        StartPosition = FormStartPosition.CenterScreen;

        var raiz = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Padding = new Padding(8) };
        raiz.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 310));
        raiz.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        raiz.Controls.Add(ConstruirPanelIzquierdo(), 0, 0);
        raiz.Controls.Add(ConstruirPanelDerecho(), 1, 0);

        var barra = new StatusStrip();
        lblEstado = new ToolStripStatusLabel("Listo.") { Spring = true, TextAlign = ContentAlignment.MiddleLeft };
        lblMetricas = new ToolStripStatusLabel();
        barra.Items.AddRange(new ToolStripItem[] { lblEstado, lblMetricas });

        Controls.Add(raiz);
        Controls.Add(barra);
        ResumeLayout(true);
    }

    private Control ConstruirPanelIzquierdo()
    {
        var panel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true
        };

        // ── Grupo: contacto ──
        var grupoContacto = Grupo("Contacto");
        var capa = Rejilla(grupoContacto);
        txtNombre = new TextBox { MaxLength = Contacto.LongitudMaxima, Dock = DockStyle.Fill, PlaceholderText = "Ej. Ana Luisa" };
        txtApellidos = new TextBox { MaxLength = Contacto.LongitudMaxima, Dock = DockStyle.Fill, PlaceholderText = "Ej. García López" };
        txtTelefono = new TextBox { MaxLength = 25, Dock = DockStyle.Fill, PlaceholderText = "55 1234 5678 / +52 …" };
        txtCorreo = new TextBox { MaxLength = 254, Dock = DockStyle.Fill, PlaceholderText = "Opcional" };
        Fila(capa, "Nombre(s):", txtNombre);
        Fila(capa, "Apellidos:", txtApellidos);
        Fila(capa, "Teléfono:", txtTelefono);
        Fila(capa, "Correo:", txtCorreo);
        FilaBotones(capa,
            Boton("Agregar", BtnAgregar_Click),
            Boton("Buscar", BtnBuscar_Click));
        FilaBotones(capa,
            Boton("Eliminar", BtnEliminar_Click),
            Boton("Limpiar campos", (_, _) => LimpiarCampos()));
        panel.Controls.Add(grupoContacto);

        // ── Grupo: edición del contacto seleccionado ──
        var grupoEdicion = Grupo("Contacto seleccionado");
        var capaEd = Rejilla(grupoEdicion);
        lblSeleccion = new Label { AutoSize = true, MaximumSize = new Size(260, 0), ForeColor = Color.DimGray, Text = "(ninguno)" };
        int filaSeleccion = capaEd.RowCount++;
        capaEd.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        capaEd.Controls.Add(lblSeleccion, 0, filaSeleccion);
        capaEd.SetColumnSpan(lblSeleccion, 2);
        FilaBotones(capaEd, Boton("Guardar teléfono/correo", BtnGuardarDatos_Click));
        FilaBotones(capaEd, Boton("Corregir nombre/apellidos", BtnRenombrar_Click));
        panel.Controls.Add(grupoEdicion);

        // ── Grupo: consultas ──
        var grupoConsultas = Grupo("Consultas");
        var capa2 = Rejilla(grupoConsultas);
        txtPrefijo = new TextBox { MaxLength = 60, Dock = DockStyle.Fill, PlaceholderText = "Escriba y se filtra solo" };
        txtPrefijo.TextChanged += TxtPrefijo_TextChanged;
        txtTelefonoBuscar = new TextBox { MaxLength = 25, Dock = DockStyle.Fill, PlaceholderText = "Número a identificar" };
        Fila(capa2, "Apellido:", txtPrefijo);
        Fila(capa2, "Número:", txtTelefonoBuscar);
        FilaBotones(capa2, Boton("¿De quién es este número?", BtnBuscarTelefono_Click));
        panel.Controls.Add(grupoConsultas);

        // ── Grupo: árbol ──
        var grupoArbol = Grupo("Árbol");
        var capa3 = Rejilla(grupoArbol);
        FilaBotones(capa3,
            Boton("Cargar ejemplo", BtnCargarEjemplo_Click),
            Boton("Vaciar agenda", BtnVaciar_Click));
        panel.Controls.Add(grupoArbol);

        return panel;
    }

    private Control ConstruirPanelDerecho()
    {
        // SplitterDistance se fija en OnLoad (antes de tener tamaño real lanza excepción).
        division = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal };

        panelArbol = new PanelArbol { Dock = DockStyle.Fill };
        division.Panel1.Controls.Add(panelArbol);

        pestanas = new TabControl { Dock = DockStyle.Fill };

        lvDirectorio = CrearLista();
        lvDirectorio.SelectedIndexChanged += Lista_SelectedIndexChanged;
        pestanas.TabPages.Add(Pestana("Directorio A→Z (Inorden)", lvDirectorio));

        lvResultados = CrearLista();
        lvResultados.SelectedIndexChanged += Lista_SelectedIndexChanged;
        pestanas.TabPages.Add(Pestana("Resultados de consulta", lvResultados));

        var recorridos = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 3, Padding = new Padding(6) };
        recorridos.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
        recorridos.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (int i = 0; i < 3; i++) recorridos.RowStyles.Add(new RowStyle(SizeType.Percent, 33.3f));
        txtPreorden = TextoSoloLectura();
        txtInorden = TextoSoloLectura();
        txtPostorden = TextoSoloLectura();
        recorridos.Controls.Add(Etiqueta("Preorden"), 0, 0); recorridos.Controls.Add(txtPreorden, 1, 0);
        recorridos.Controls.Add(Etiqueta("Inorden"), 0, 1); recorridos.Controls.Add(txtInorden, 1, 1);
        recorridos.Controls.Add(Etiqueta("Postorden"), 0, 2); recorridos.Controls.Add(txtPostorden, 1, 2);
        pestanas.TabPages.Add(Pestana("Recorridos (iterativos)", recorridos));

        var pruebas = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2, Padding = new Padding(6) };
        pruebas.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170));
        pruebas.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        pruebas.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        pruebas.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var btnPruebas = Boton("Ejecutar pruebas", BtnPruebas_Click);
        lblResultadoPruebas = new Label { AutoSize = true, Anchor = AnchorStyles.Left, Font = new Font(Font, FontStyle.Bold) };
        txtPruebas = TextoSoloLectura();
        txtPruebas.Font = new Font("Consolas", 9.5f);
        pruebas.Controls.Add(btnPruebas, 0, 0);
        pruebas.Controls.Add(lblResultadoPruebas, 1, 0);
        pruebas.Controls.Add(txtPruebas, 0, 1);
        pruebas.SetColumnSpan(txtPruebas, 2);
        pestanas.TabPages.Add(Pestana("Pruebas", pruebas));

        division.Panel2.Controls.Add(pestanas);
        return division;
    }

    // ───────────── Fábricas de controles ─────────────

    private static GroupBox Grupo(string titulo) =>
        new() { Text = titulo, Width = 288, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Padding = new Padding(8) };

    private static TableLayoutPanel Rejilla(GroupBox grupo)
    {
        // Sin Dock: el GroupBox (AutoSize) se ajusta al tamaño preferido de la rejilla.
        var t = new TableLayoutPanel
        {
            Location = new Point(8, 22), ColumnCount = 2, AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink
        };
        t.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80));
        t.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 186));
        grupo.Controls.Add(t);
        return t;
    }

    private static void Fila(TableLayoutPanel t, string etiqueta, Control control)
    {
        int fila = t.RowCount++;
        t.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        t.Controls.Add(Etiqueta(etiqueta), 0, fila);
        t.Controls.Add(control, 1, fila);
    }

    private static void FilaBotones(TableLayoutPanel t, params Button[] botones)
    {
        int fila = t.RowCount++;
        t.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        var flujo = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, Margin = new Padding(0) };
        foreach (Button b in botones)
        {
            b.Width = botones.Length == 1 ? 262 : 128;
            flujo.Controls.Add(b);
        }
        t.Controls.Add(flujo, 0, fila);
        t.SetColumnSpan(flujo, 2);
    }

    private static Label Etiqueta(string texto) =>
        new() { Text = texto, AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(3, 7, 3, 3) };

    private static Button Boton(string texto, EventHandler alPulsar)
    {
        var b = new Button { Text = texto, Height = 30, UseVisualStyleBackColor = true };
        b.Click += alPulsar;
        return b;
    }

    private static TextBox TextoSoloLectura() =>
        new() { Dock = DockStyle.Fill, Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Both, WordWrap = true, BackColor = SystemColors.Window };

    private static TabPage Pestana(string titulo, Control contenido)
    {
        var p = new TabPage(titulo);
        p.Controls.Add(contenido);
        return p;
    }

    private static ListView CrearLista()
    {
        var lv = new ListView
        {
            Dock = DockStyle.Fill, View = View.Details, FullRowSelect = true,
            GridLines = true, MultiSelect = false, HideSelection = false
        };
        lv.Columns.Add("Apellidos", 200);
        lv.Columns.Add("Nombre(s)", 160);
        lv.Columns.Add("Teléfono", 130);
        lv.Columns.Add("Correo", 240);
        return lv;
    }
}
