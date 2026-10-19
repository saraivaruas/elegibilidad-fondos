namespace Elegibilidad;

/// <summary>Catálogo de programas que viene dentro de la biblioteca (Catalogo/fondos.json, incrustado en el ensamblado).</summary>
public static class CatalogoFondos
{
    public static MotorElegibilidad Cargar()
    {
        using var s = typeof(CatalogoFondos).Assembly.GetManifestResourceStream("fondos.json")
            ?? throw new InvalidOperationException("No se encontró el catálogo incrustado fondos.json.");
        using var r = new StreamReader(s);
        return MotorElegibilidad.DesdeJson(r.ReadToEnd());
    }
}
