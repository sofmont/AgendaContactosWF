# Agenda de Contactos ordenada — Árbol Binario de Búsqueda (Windows Forms, .NET 10)

Proyecto autónomo: no depende de ningún otro proyecto ni archivo externo.

- **Clave del BST:** APELLIDOS, NOMBRE (orden español, Ñ entre N y O)
- **Implementación:** Todo iterativo, con pila enlazada implementada desde cero

## Ejecutar

- Visual Studio: abrir `AgendaContactosWF.slnx` (o `AgendaContactosWF.csproj`) y presionar F5.
- Consola (Windows): `dotnet run` dentro de esta carpeta.

Para .NET 8, cambie `net10.0-windows` por `net8.0-windows` en `AgendaContactosWF.csproj`.

## Archivos

- `Program.cs` — punto de entrada (`[STAThread]`).
- `Form*.cs` — eventos y validación; `Form*.Controles.cs` — interfaz construida por código (si VS abre el diseñador, use *Ver código*).
- `Arbol*.cs` — nodo con memoria dinámica + BST: inserción, búsqueda, eliminación, Preorden/Inorden/Postorden.
- `PanelArbol.cs` — dibujo del árbol con GDI+.
- `Pruebas.cs` — pruebas automáticas, pestaña *Pruebas* del formulario.
