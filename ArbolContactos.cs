namespace AgendaContactosWF;

/// <summary>Nodo del BST, reservado dinámicamente en el heap.</summary>
internal sealed class NodoContacto
{
    public Contacto Dato { get; }
    public NodoContacto? Izquierdo { get; set; }
    public NodoContacto? Derecho { get; set; }

    public NodoContacto(Contacto dato)
    {
        Dato = dato ?? throw new ArgumentNullException(nameof(dato));
    }
}

/// <summary>
/// Pila enlazada implementada desde cero (también con memoria dinámica).
/// Sustituye a la pila de llamadas en los recorridos iterativos, evitando
/// desbordamiento de pila en árboles degenerados.
/// </summary>
internal sealed class PilaNodos
{
    private sealed class Elemento
    {
        public NodoContacto Nodo { get; }
        public Elemento? Siguiente { get; }

        public Elemento(NodoContacto nodo, Elemento? siguiente)
        {
            Nodo = nodo;
            Siguiente = siguiente;
        }
    }

    private Elemento? tope;

    public bool EstaVacia => tope is null;

    public void Apilar(NodoContacto nodo)
    {
        ArgumentNullException.ThrowIfNull(nodo);
        tope = new Elemento(nodo, tope);
    }

    public NodoContacto Desapilar()
    {
        if (tope is null)
            throw new InvalidOperationException("La pila está vacía.");
        NodoContacto nodo = tope.Nodo;
        tope = tope.Siguiente;
        return nodo;
    }

    public NodoContacto Cima =>
        tope?.Nodo ?? throw new InvalidOperationException("La pila está vacía.");
}

/// <summary>Resultado de renombrar un contacto.</summary>
public enum ResultadoRenombrar
{
    Renombrado,
    NoExiste,
    NuevoNombreDuplicado
}

/// <summary>
/// Agenda ordenada alfabéticamente por "APELLIDOS, NOMBRE".
/// Invariante BST: claves(subárbol izq.) &lt; clave(nodo) &lt; claves(subárbol der.).
/// Todas las operaciones son iterativas (sin recursión).
/// Nota: el visitante de un recorrido no debe insertar ni eliminar contactos.
/// </summary>
public sealed class ArbolContactos
{
    private NodoContacto? raiz;

    public int Cantidad { get; private set; }
    public bool EstaVacia => raiz is null;

    // ═══════════════════════ INSERCIÓN  O(h) ═══════════════════════

    public bool Insertar(Contacto contacto)
    {
        ArgumentNullException.ThrowIfNull(contacto);

        NodoContacto? padre = null;
        NodoContacto? actual = raiz;
        int cmp = 0;

        while (actual is not null)
        {
            cmp = Texto.Comparar(contacto.Clave, actual.Dato.Clave);
            if (cmp == 0)
                return false;                       // duplicado: no se reserva memoria
            padre = actual;
            actual = cmp < 0 ? actual.Izquierdo : actual.Derecho;
        }

        var nuevo = new NodoContacto(contacto);     // reserva dinámica
        if (padre is null)
            raiz = nuevo;
        else if (cmp < 0)
            padre.Izquierdo = nuevo;
        else
            padre.Derecho = nuevo;

        Cantidad++;
        return true;
    }

    // ═══════════════════════ BÚSQUEDA  O(h) ═══════════════════════

    public Contacto? Buscar(string? apellidos, string? nombre)
    {
        if (string.IsNullOrWhiteSpace(apellidos) || string.IsNullOrWhiteSpace(nombre))
            return null;
        return BuscarNodo(Contacto.CrearClave(apellidos, nombre))?.Dato;
    }

    private NodoContacto? BuscarNodo(string clave)
    {
        NodoContacto? actual = raiz;
        while (actual is not null)
        {
            int cmp = Texto.Comparar(clave, actual.Dato.Clave);
            if (cmp == 0)
                return actual;
            actual = cmp < 0 ? actual.Izquierdo : actual.Derecho;
        }
        return null;
    }

    // ═══════════════════════ ELIMINACIÓN  O(h) ═══════════════════════

    public bool Eliminar(string? apellidos, string? nombre)
    {
        if (string.IsNullOrWhiteSpace(apellidos) || string.IsNullOrWhiteSpace(nombre))
            return false;
        return EliminarClave(Contacto.CrearClave(apellidos, nombre));
    }

    private bool EliminarClave(string clave)
    {
        NodoContacto? padre = null;
        NodoContacto? actual = raiz;
        while (actual is not null)
        {
            int cmp = Texto.Comparar(clave, actual.Dato.Clave);
            if (cmp == 0) break;
            padre = actual;
            actual = cmp < 0 ? actual.Izquierdo : actual.Derecho;
        }
        if (actual is null)
            return false;

        NodoContacto? reemplazo;
        if (actual.Izquierdo is null)
        {
            reemplazo = actual.Derecho;             // hoja o solo hijo derecho
        }
        else if (actual.Derecho is null)
        {
            reemplazo = actual.Izquierdo;           // solo hijo izquierdo
        }
        else
        {
            // Dos hijos: sucesor inorden = mínimo del subárbol derecho.
            NodoContacto padreSucesor = actual;
            NodoContacto sucesor = actual.Derecho;
            while (sucesor.Izquierdo is not null)
            {
                padreSucesor = sucesor;
                sucesor = sucesor.Izquierdo;
            }
            if (padreSucesor != actual)
            {
                padreSucesor.Izquierdo = sucesor.Derecho;
                sucesor.Derecho = actual.Derecho;
            }
            sucesor.Izquierdo = actual.Izquierdo;
            reemplazo = sucesor;
        }

        if (padre is null)
            raiz = reemplazo;
        else if (padre.Izquierdo == actual)
            padre.Izquierdo = reemplazo;
        else
            padre.Derecho = reemplazo;

        actual.Izquierdo = null;                    // nodo inalcanzable → lo libera el GC
        actual.Derecho = null;
        Cantidad--;
        return true;
    }

    /// <summary>
    /// Cambia nombre/apellidos conservando teléfono y correo. Como cambia la clave,
    /// se elimina el nodo y se inserta uno nuevo: O(h) + O(h) = O(h).
    /// </summary>
    public ResultadoRenombrar Renombrar(Contacto actual, string nuevoNombre, string nuevosApellidos)
    {
        ArgumentNullException.ThrowIfNull(actual);
        if (BuscarNodo(actual.Clave)?.Dato != actual)
            return ResultadoRenombrar.NoExiste;

        // Puede lanzar ArgumentException si el nombre nuevo es inválido (antes de tocar el árbol).
        var renombrado = new Contacto(nuevoNombre, nuevosApellidos, actual.Telefono, actual.Correo);

        if (Texto.Comparar(renombrado.Clave, actual.Clave) != 0 && BuscarNodo(renombrado.Clave) is not null)
            return ResultadoRenombrar.NuevoNombreDuplicado;

        EliminarClave(actual.Clave);
        Insertar(renombrado);
        return ResultadoRenombrar.Renombrado;
    }

    // ═══════════════════════ RECORRIDOS ITERATIVOS ═══════════════════════

    /// <summary>Preorden (R-I-D): se apila primero el hijo derecho para procesar antes el izquierdo.</summary>
    public void Preorden(Action<Contacto> visitar)
    {
        ArgumentNullException.ThrowIfNull(visitar);
        if (raiz is null) return;

        var pila = new PilaNodos();
        pila.Apilar(raiz);
        while (!pila.EstaVacia)
        {
            NodoContacto nodo = pila.Desapilar();
            visitar(nodo.Dato);
            if (nodo.Derecho is not null) pila.Apilar(nodo.Derecho);
            if (nodo.Izquierdo is not null) pila.Apilar(nodo.Izquierdo);
        }
    }

    /// <summary>Inorden (I-R-D): baja por la izquierda apilando; al desapilar visita y gira a la derecha.</summary>
    public void Inorden(Action<Contacto> visitar)
    {
        ArgumentNullException.ThrowIfNull(visitar);

        var pila = new PilaNodos();
        NodoContacto? actual = raiz;
        while (actual is not null || !pila.EstaVacia)
        {
            while (actual is not null)
            {
                pila.Apilar(actual);
                actual = actual.Izquierdo;
            }
            NodoContacto nodo = pila.Desapilar();
            visitar(nodo.Dato);
            actual = nodo.Derecho;
        }
    }

    /// <summary>
    /// Postorden (I-D-R) con una sola pila: un nodo se visita cuando no tiene hijo derecho
    /// o cuando su hijo derecho fue el último nodo visitado.
    /// </summary>
    public void Postorden(Action<Contacto> visitar)
    {
        ArgumentNullException.ThrowIfNull(visitar);

        var pila = new PilaNodos();
        NodoContacto? actual = raiz;
        NodoContacto? ultimoVisitado = null;
        while (actual is not null || !pila.EstaVacia)
        {
            if (actual is not null)
            {
                pila.Apilar(actual);
                actual = actual.Izquierdo;
            }
            else
            {
                NodoContacto cima = pila.Cima;
                if (cima.Derecho is not null && cima.Derecho != ultimoVisitado)
                {
                    actual = cima.Derecho;
                }
                else
                {
                    visitar(cima.Dato);
                    ultimoVisitado = pila.Desapilar();
                }
            }
        }
    }

    // ═══════════════════════ CONSULTAS DEL DOMINIO ═══════════════════════

    /// <summary>
    /// Autocompletar: contactos cuya clave "APELLIDOS, NOMBRE" empieza con el prefijo.
    /// Inorden iterativo con poda → O(h + k).
    /// </summary>
    public List<Contacto> BuscarPorPrefijo(string? prefijo)
    {
        var resultado = new List<Contacto>();
        if (string.IsNullOrWhiteSpace(prefijo))
            return resultado;

        string p = Texto.Normalizar(prefijo);
        var pila = new PilaNodos();
        NodoContacto? actual = raiz;
        while (actual is not null || !pila.EstaVacia)
        {
            // Descenso: solo se apilan nodos que podrían coincidir o tener coincidencias a su derecha.
            while (actual is not null)
            {
                string clave = actual.Dato.Clave;
                bool coincide = clave.StartsWith(p, StringComparison.Ordinal);
                if (coincide || Texto.Comparar(clave, p) > 0)
                {
                    pila.Apilar(actual);              // puede haber coincidencias a la izquierda
                    actual = actual.Izquierdo;
                }
                else
                {
                    actual = actual.Derecho;          // clave < prefijo: la izquierda es aún menor
                }
            }
            if (pila.EstaVacia)
                break;

            NodoContacto nodo = pila.Desapilar();
            if (!nodo.Dato.Clave.StartsWith(p, StringComparison.Ordinal))
                break;                                // primera clave > todo el rango: terminamos
            resultado.Add(nodo.Dato);
            actual = nodo.Derecho;
        }
        return resultado;
    }

    /// <summary>
    /// Búsqueda inversa por teléfono. El árbol está ordenado por nombre, no por teléfono,
    /// así que no hay poda posible: recorrido inorden completo O(n) con salida temprana.
    /// </summary>
    public Contacto? BuscarPorTelefono(string? telefono)
    {
        string? digitos = Texto.NormalizarTelefono(telefono);
        if (digitos is null) return null;

        var pila = new PilaNodos();
        NodoContacto? actual = raiz;
        while (actual is not null || !pila.EstaVacia)
        {
            while (actual is not null)
            {
                pila.Apilar(actual);
                actual = actual.Izquierdo;
            }
            NodoContacto nodo = pila.Desapilar();
            if (nodo.Dato.Telefono == digitos)
                return nodo.Dato;
            actual = nodo.Derecho;
        }
        return null;
    }

    /// <summary>
    /// Coordenadas para dibujar: columna = posición inorden, nivel = profundidad.
    /// Inorden iterativo con la pila propia, guardando el nivel de cada nodo. O(n).
    /// </summary>
    public Diagrama ObtenerDiagrama()
    {
        var diagrama = new Diagrama();
        var columnas = new Dictionary<NodoContacto, (int Columna, int Nivel)>(ReferenceEqualityComparer.Instance);
        var niveles = new Dictionary<NodoContacto, int>(ReferenceEqualityComparer.Instance);
        if (raiz is null) return diagrama;

        niveles[raiz] = 0;
        var pila = new PilaNodos();
        NodoContacto? actual = raiz;
        int columna = 0;
        while (actual is not null || !pila.EstaVacia)
        {
            while (actual is not null)
            {
                pila.Apilar(actual);
                if (actual.Izquierdo is not null) niveles[actual.Izquierdo] = niveles[actual] + 1;
                actual = actual.Izquierdo;
            }
            NodoContacto nodo = pila.Desapilar();
            int nivel = niveles[nodo];
            columnas[nodo] = (columna, nivel);
            diagrama.Nodos.Add(new NodoDibujo(nodo.Dato.NombreCompleto, nodo.Dato.Clave, columna, nivel));
            columna++;
            if (nodo.Derecho is not null) niveles[nodo.Derecho] = nivel + 1;
            actual = nodo.Derecho;
        }

        foreach (KeyValuePair<NodoContacto, (int Columna, int Nivel)> par in columnas)
        {
            (int c, int n) = par.Value;
            if (par.Key.Izquierdo is not null)
            {
                (int ci, int ni) = columnas[par.Key.Izquierdo];
                diagrama.Aristas.Add(new AristaDibujo(c, n, ci, ni));
            }
            if (par.Key.Derecho is not null)
            {
                (int cd, int nd) = columnas[par.Key.Derecho];
                diagrama.Aristas.Add(new AristaDibujo(c, n, cd, nd));
            }
        }
        return diagrama;
    }

    /// <summary>Altura en niveles (vacío = 0), calculada por niveles con dos listas: O(n).</summary>
    public int Altura()
    {
        if (raiz is null) return 0;

        int altura = 0;
        var nivel = new List<NodoContacto> { raiz };
        while (nivel.Count > 0)
        {
            altura++;
            var siguiente = new List<NodoContacto>();
            foreach (NodoContacto n in nivel)
            {
                if (n.Izquierdo is not null) siguiente.Add(n.Izquierdo);
                if (n.Derecho is not null) siguiente.Add(n.Derecho);
            }
            nivel = siguiente;
        }
        return altura;
    }

    /// <summary>
    /// Vacía la agenda cortando los enlaces de cada nodo (recorrido con la pila propia),
    /// de modo que todos quedan inalcanzables para el recolector de basura. O(n).
    /// </summary>
    public void Vaciar()
    {
        if (raiz is not null)
        {
            var pila = new PilaNodos();
            pila.Apilar(raiz);
            while (!pila.EstaVacia)
            {
                NodoContacto nodo = pila.Desapilar();
                if (nodo.Izquierdo is not null) pila.Apilar(nodo.Izquierdo);
                if (nodo.Derecho is not null) pila.Apilar(nodo.Derecho);
                nodo.Izquierdo = null;
                nodo.Derecho = null;
            }
        }
        raiz = null;
        Cantidad = 0;
    }

    /// <summary>Invariante: inorden estrictamente creciente y conteo consistente.</summary>
    public bool VerificarInvariante()
    {
        string? anterior = null;
        int contados = 0;
        bool valido = true;
        Inorden(c =>
        {
            if (anterior is not null && Texto.Comparar(anterior, c.Clave) >= 0)
                valido = false;
            anterior = c.Clave;
            contados++;
        });
        return valido && contados == Cantidad;
    }
}
