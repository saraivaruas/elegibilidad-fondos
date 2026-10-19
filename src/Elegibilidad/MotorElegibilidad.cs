using System.Text.Json;
using System.Text.Json.Serialization;

namespace Elegibilidad;

/// <summary>Resultado de una regla o de un programa completo. «PorConfirmar» es lo que distingue este motor de un simple sí/no: falta un dato, no es un descarte.</summary>
public enum Veredicto { Cumple, NoCumple, PorConfirmar }

/// <summary>
/// Una condición de un programa, escrita como datos (fondos.json) y no como código, para que la revise y la cambie quien conoce las bases.
/// <paramref name="Campo"/> es una clave del perfil (ver <see cref="Perfil"/>). <paramref name="Op"/>: eq, gte, lte, gt, lt.
/// <paramref name="Cuando"/> hace que la regla solo aplique si se cumple esa otra condición (por ejemplo, las ventas mínimas de Crece dependen de la antigüedad).
/// <paramref name="Texto"/> es lo que ve la empresa cuando la regla se cumple o hace falta; <paramref name="SiFalla"/> explica qué hacer.
/// </summary>
public record Regla(
    string Id, string Campo, string Op, double Valor, string Texto, string SiFalla, string Fuente,
    Regla.Condicion? Cuando = null)
{
    public record Condicion(string Campo, string Op, double Valor);
}

/// <summary>
/// La ficha de un programa para mostrarla completa dentro de la plataforma, sin mandar a la persona a otro sitio. Todo es texto resumido de las bases oficiales;
/// un apartado que las fuentes no permiten confirmar se deja vacío (null) y la ficha no lo muestra: no se rellena con suposiciones.
/// <c>TextoOficialUrl</c> es la página pública del programa de la que la aplicación puede leer, al abrir la ficha, el texto oficial actualizado (ver <see cref="PaginaOficial"/>).
/// </summary>
public record Ficha(
    string? Resumen = null,
    List<string>? Proceso = null,
    List<string>? Plazos = null,
    List<string>? CriteriosEvaluacion = null,
    List<string>? Documentos = null,
    List<string>? Gastos = null,
    List<string>? Notas = null,
    string? Fuente = null,
    Postulacion? Postulacion = null,
    string? TextoOficialUrl = null);

/// <summary>Un enlace oficial para postular. El primero de la lista es el principal («Ir a postular»); los demás son de apoyo.</summary>
public record Enlace(string Texto, string Url);

/// <summary>Un apartado del texto oficial de una convocatoria (por ejemplo «OBJETIVO» o «CALENDARIZACIÓN»), ya sin formato, para mostrarlo dentro de la aplicación.</summary>
public record SeccionOficial(string Titulo, string Texto);

/// <summary>Dónde se postula de verdad. Las convocatorias regionales (Sercotec, Súmate a Innovar) cambian de dirección en cada llamado: se enlaza la página oficial que las lista.</summary>
public record Postulacion(List<Enlace> Enlaces, string? Nota = null);

/// <summary>Un programa del catálogo. <paramref name="Revisado"/> es la fecha en que alguien comparó estas reglas con las bases oficiales: pasado un año hay que volver a revisarlas.</summary>
public record Programa(
    string Id, string Nombre, string Entidad, string Descripcion, string Url, string MontoAprox, string ComoPostular, string Vigencia,
    string Revisado, List<Regla> Reglas, Ficha? Ficha = null, Ventana? Ventana = null);

public record ResultadoRegla(Regla Regla, Veredicto Veredicto);

public record ResultadoPrograma(Programa Programa, Veredicto Veredicto, int Afinidad, IReadOnlyList<ResultadoRegla> Reglas)
{
    /// <summary>Lo que le falta a la empresa para calificar (reglas que no cumple o que no se pudieron comprobar).</summary>
    public IEnumerable<ResultadoRegla> Faltantes => Reglas.Where(r => r.Veredicto != Veredicto.Cumple);
}

/// <summary>Datos de la empresa contra los que se evalúa. Un valor ausente (null) significa «no se sabe»: la regla queda «por confirmar», nunca se supone.</summary>
public class Perfil
{
    private readonly Dictionary<string, double?> _valores = new(StringComparer.Ordinal);
    public Perfil Con(string campo, double? valor) { _valores[campo] = valor; return this; }
    public Perfil Con(string campo, bool? valor) { _valores[campo] = valor is null ? null : valor.Value ? 1 : 0; return this; }
    public double? Valor(string campo) => _valores.TryGetValue(campo, out var v) ? v : null;
}

/// <summary>
/// Motor determinista: sin IA ni llamadas externas. Evalúa cada programa del catálogo contra un <see cref="Perfil"/> y entrega, por programa,
/// un veredicto de tres estados, un puntaje de afinidad (porcentaje de reglas cumplidas) y la lista de lo que falta.
/// </summary>
public class MotorElegibilidad(IReadOnlyList<Programa> catalogo)
{
    public IReadOnlyList<Programa> Catalogo { get; } = catalogo;

    public static MotorElegibilidad DesdeJson(string json)
    {
        var opciones = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, Converters = { new JsonStringEnumConverter() } };
        var lista = JsonSerializer.Deserialize<List<Programa>>(json, opciones) ?? throw new InvalidDataException("El catálogo de fondos está vacío.");
        foreach (var p in lista)
        {
            if (p.Reglas.Count == 0) throw new InvalidDataException($"El programa {p.Id} no tiene reglas.");
            foreach (var r in p.Reglas)
                if (!Operadores.Contains(r.Op) || (r.Cuando is { } c && !Operadores.Contains(c.Op)))
                    throw new InvalidDataException($"Operador desconocido en {p.Id}/{r.Id}.");
        }
        return new MotorElegibilidad(lista);
    }

    private static readonly HashSet<string> Operadores = ["eq", "gte", "lte", "gt", "lt"];

    private static bool Compara(double a, string op, double b) => op switch
    {
        "eq" => a == b, "gte" => a >= b, "lte" => a <= b, "gt" => a > b, "lt" => a < b, _ => false,
    };

    public static Veredicto EvaluarRegla(Regla r, Perfil perfil)
    {
        if (r.Cuando is { } c)
        {
            var dato = perfil.Valor(c.Campo);
            if (dato is null) return Veredicto.PorConfirmar;
            if (!Compara(dato.Value, c.Op, c.Valor)) return Veredicto.Cumple; // la regla no aplica a este caso
        }
        var v = perfil.Valor(r.Campo);
        if (v is null) return Veredicto.PorConfirmar;
        return Compara(v.Value, r.Op, r.Valor) ? Veredicto.Cumple : Veredicto.NoCumple;
    }

    public ResultadoPrograma Evaluar(Programa p, Perfil perfil)
    {
        var rs = p.Reglas.Select(r => new ResultadoRegla(r, EvaluarRegla(r, perfil))).ToList();
        var veredicto = rs.Any(x => x.Veredicto == Veredicto.NoCumple) ? Veredicto.NoCumple
            : rs.Any(x => x.Veredicto == Veredicto.PorConfirmar) ? Veredicto.PorConfirmar : Veredicto.Cumple;
        var afinidad = (int)Math.Round(100.0 * rs.Count(x => x.Veredicto == Veredicto.Cumple) / rs.Count);
        return new ResultadoPrograma(p, veredicto, afinidad, rs);
    }

    /// <summary>Todos los programas, de mejor a peor: primero los que califican, luego los que faltan por confirmar, luego los descartados; dentro de cada grupo, por afinidad.</summary>
    public IReadOnlyList<ResultadoPrograma> Radar(Perfil perfil) =>
        Catalogo.Select(p => Evaluar(p, perfil)).OrderBy(r => r.Veredicto).ThenByDescending(r => r.Afinidad).ThenBy(r => r.Programa.Nombre).ToList();

    /// <summary>
    /// «¿Qué arreglo desbloquea más fondos?»: para cada regla que hoy falla, cuántos programas pasarían a no estar descartados si solo esa regla fuera el problema.
    /// Sirve para decirle a la empresa por dónde empezar.
    /// </summary>
    public IReadOnlyList<(string SiFalla, int Programas)> QueDesbloquea(Perfil perfil)
    {
        var cuenta = new Dictionary<string, (string SiFalla, int N)>();
        foreach (var res in Catalogo.Select(p => Evaluar(p, perfil)).Where(r => r.Veredicto == Veredicto.NoCumple))
        {
            var fallas = res.Reglas.Where(x => x.Veredicto == Veredicto.NoCumple).ToList();
            if (fallas.Count != 1) continue;
            var f = fallas[0].Regla;
            cuenta[f.SiFalla] = (f.SiFalla, cuenta.GetValueOrDefault(f.SiFalla).N + 1);
        }
        return cuenta.Values.OrderByDescending(x => x.N).Select(x => (x.SiFalla, x.N)).ToList();
    }
}


/// <summary>Estado de la postulación de un programa en una fecha dada.</summary>
public enum EstadoPostulacion { Abierta, Proxima, Cerrada, Permanente, Desconocida }

/// <summary>
/// Cuándo se puede postular. <paramref name="Tipo"/>: «permanente» (todo el año), «fechas» (una convocatoria con apertura y cierre conocidos, o la última cerrada)
/// o «externo» (la convocatoria cambia por región y se consulta a la fuente oficial, por ejemplo el calendario de Sercotec) o «fia» (se consulta la API de FIA y, si no responde, se usan las fechas del catálogo).
/// <paramref name="Claves"/> y <paramref name="Excluir"/> sirven para reconocer el programa en ese calendario. <paramref name="Verificado"/> es la fecha en que se comprobó.
/// </summary>
public record Ventana(string Tipo, string? Abre = null, string? Cierra = null, string? Verificado = null, string? Nota = null, List<string>? Claves = null, List<string>? Excluir = null)
{
    /// <summary>Estado según las fechas del catálogo. Para el tipo «externo» devuelve <see cref="EstadoPostulacion.Desconocida"/>: lo resuelve quien consulta la fuente.</summary>
    public EstadoPostulacion Estado(DateOnly hoy)
    {
        if (Tipo == "permanente") return EstadoPostulacion.Permanente;
        // «fia» usa estas mismas fechas solo como respaldo cuando no se puede consultar la API de FIA.
        if (Tipo != "fechas" && Tipo != "fia") return EstadoPostulacion.Desconocida;
        if (DateOnly.TryParse(Abre, out var a) && hoy < a) return EstadoPostulacion.Proxima;
        if (DateOnly.TryParse(Cierra, out var c)) return hoy <= c ? EstadoPostulacion.Abierta : EstadoPostulacion.Cerrada;
        return EstadoPostulacion.Desconocida;
    }
}