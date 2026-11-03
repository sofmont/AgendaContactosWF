using System.Drawing.Drawing2D;

namespace AgendaContactosWF;

/// <summary>Nodo listo para dibujar: columna = posición inorden, nivel = profundidad.</summary>
public sealed record NodoDibujo(string Texto, string Clave, int Columna, int Nivel);

/// <summary>Arista padre → hijo expresada en coordenadas de cuadrícula.</summary>
public sealed record AristaDibujo(int ColumnaPadre, int NivelPadre, int ColumnaHijo, int NivelHijo);

/// <summary>
/// Disposición del árbol. Usar la posición inorden como coordenada X garantiza que
/// ningún par de nodos se encime y que la vista respete el orden del BST
/// (todo lo de la izquierda es menor). Se calcula en O(n).
/// </summary>
public sealed class Diagrama
{
    public List<NodoDibujo> Nodos { get; } = new();
    public List<AristaDibujo> Aristas { get; } = new();
}

/// <summary>
/// Panel con doble búfer y desplazamiento que dibuja el BST con GDI+.
/// El nodo cuya clave coincide con <see cref="Mostrar"/> se resalta en dorado.
/// </summary>
internal sealed class PanelArbol : Panel
{
    private const int AnchoCelda = 96;
    private const int AltoNivel = 72;
    private const int Margen = 24;
    private const int AnchoNodo = 88;
    private const int AltoNodo = 34;

    private Diagrama diagrama = new();
    private string? claveResaltada;

    public PanelArbol()
    {
        DoubleBuffered = true;
        ResizeRedraw = true;
        AutoScroll = true;
        BackColor = Color.White;
        BorderStyle = BorderStyle.FixedSingle;
    }

    public void Mostrar(Diagrama nuevo, string? resaltar)
    {
        diagrama = nuevo ?? throw new ArgumentNullException(nameof(nuevo));
        claveResaltada = resaltar;

        int niveles = 0;
        foreach (NodoDibujo n in diagrama.Nodos)
            niveles = Math.Max(niveles, n.Nivel + 1);

        AutoScrollMinSize = new Size(
            Margen * 2 + diagrama.Nodos.Count * AnchoCelda,
            Margen * 2 + niveles * AltoNivel);
        Invalidate();
    }

    protected override void OnScroll(ScrollEventArgs se)
    {
        base.OnScroll(se);
        Invalidate();
    }

    protected override void OnMouseWheel(MouseEventArgs e)
    {
        base.OnMouseWheel(e);
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        Graphics g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TranslateTransform(AutoScrollPosition.X, AutoScrollPosition.Y);

        if (diagrama.Nodos.Count == 0)
        {
            g.DrawString("Árbol vacío", Font, Brushes.Gray, Margen, Margen);
            return;
        }

        using (var lapiz = new Pen(Color.SlateGray, 1.6f))
        {
            foreach (AristaDibujo a in diagrama.Aristas)
                g.DrawLine(lapiz, Centro(a.ColumnaPadre, a.NivelPadre), Centro(a.ColumnaHijo, a.NivelHijo));
        }

        using var formato = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center,
            Trimming = StringTrimming.EllipsisCharacter,
            FormatFlags = StringFormatFlags.NoWrap
        };
        using var fondoNormal = new SolidBrush(Color.AliceBlue);
        using var fondoResaltado = new SolidBrush(Color.Gold);
        using var borde = new Pen(Color.SteelBlue, 1.4f);

        foreach (NodoDibujo n in diagrama.Nodos)
        {
            PointF c = Centro(n.Columna, n.Nivel);
            var rect = new RectangleF(c.X - AnchoNodo / 2f, c.Y - AltoNodo / 2f, AnchoNodo, AltoNodo);
            bool resaltado = claveResaltada is not null && n.Clave == claveResaltada;
            g.FillEllipse(resaltado ? fondoResaltado : fondoNormal, rect);
            g.DrawEllipse(borde, rect);
            g.DrawString(n.Texto, Font, Brushes.Black, rect, formato);
        }
    }

    private static PointF Centro(int columna, int nivel) =>
        new(Margen + columna * AnchoCelda + AnchoCelda / 2f,
            Margen + nivel * AltoNivel + AltoNodo / 2f);
}
