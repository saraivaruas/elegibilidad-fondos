namespace Elegibilidad;

/// <summary>Catálogo de programas que viene dentro de la biblioteca (Catalogo/fondos.json, incrustado en el ensamblado).</summary>
public static class CatalogoFondos
{
    private static readonly Lazy<MotorElegibilidad> _compartido = new(Cargar);

    /// <summary>El catálogo ya cargado, una sola vez por proceso: es de solo lectura, así que todas las pantallas y herramientas pueden compartirlo.</summary>
    public static MotorElegibilidad Compartido => _compartido.Value;

    public static MotorElegibilidad Cargar()
    {
        using var s = typeof(CatalogoFondos).Assembly.GetManifestResourceStream("fondos.json")
            ?? throw new InvalidOperationException("No se encontró el catálogo incrustado fondos.json.");
        using var r = new StreamReader(s);
        return MotorElegibilidad.DesdeJson(r.ReadToEnd());
    }
}
