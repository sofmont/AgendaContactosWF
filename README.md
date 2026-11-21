# Agenda de Contactos ordenada — Árbol Binario de Búsqueda (Windows Forms, .NET 10)

Proyecto autónomo: no depende de ningún otro proyecto ni archivo externo.

**Unidad 4 · Tarea 4.2.**

## Datos de la entrega

| | |
|---|---|
| Alumna | Barbara Sofia Montalvo Ontiveros |
| Matrícula | I25050373 |
| Carrera | Ingeniería en Informática |
| Asignatura | Estructuras de Datos |
| Docente | Ruben Riojas |
| Institución | Instituto Tecnológico Superior de Monclova |
| Video explicativo | _(pegar aquí el enlace público de YouTube)_ |

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

## Resultados de las pruebas

Árbol de ejemplo (inserción: Martínez, García, Ortiz, Díaz, Hernández, Núñez, Ruiz; altura 3):

| Recorrido | Resultado |
|---|---|
| Preorden | Martínez García Díaz Hernández Ortiz Núñez Ruiz |
| Inorden | Díaz García Hernández Martínez Núñez Ortiz Ruiz (orden alfabético) |
| Postorden | Díaz Hernández García Núñez Ruiz Ortiz Martínez |

La pestaña *Pruebas* ejecuta 45 verificaciones (recorridos, búsqueda, duplicados, validación, autocompletar, renombrar, eliminación de hoja / un hijo / dos hijos y árbol degenerado de 1 000 nodos): **45 correctas, 0 fallas**.
