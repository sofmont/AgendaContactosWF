namespace AgendaContactosWF;

internal static class Program
{
    /// <summary>Punto de entrada de la aplicación Windows Forms.</summary>
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new FormAgenda());
    }
}
