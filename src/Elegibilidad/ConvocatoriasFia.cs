using System.Globalization;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Elegibilidad;

/// <summary>Una convocatoria publicada por FIA (Fundación para la Innovación Agraria) en su API pública de WordPress.</summary>
public record ConvocatoriaFia(string Titulo, string Slug, string Link, DateOnly? Inicio, DateOnly? Cierre, string? UrlPostulacion, IReadOnlyList<SeccionOficial>? Secciones = null);

/// <summary>
/// Lee las convocatorias de FIA desde su API pública (<c>https://www.fia.cl/wp-json/wp/v2/convocatorias</c>), que trae el texto oficial con las fechas de INICIO y CIERRE
/// (si hubo ampliación de plazo, hay más de un CIERRE y vale el último) y el enlace de «FORMA DE POSTULACIÓN». No hace llamadas de red: quien usa la biblioteca descarga el JSON y se lo entrega.
/// </summary>
public static partial class ConvocatoriasFia
{
    private static readonly string[] Meses = ["enero", "febrero", "marzo", "abril", "mayo", "junio", "julio", "agosto", "septiembre", "octubre", "noviembre", "diciembre"];

    [GeneratedRegex(@"INICIO:\s*(\d{1,2})\s*de\s+([A-Za-zñÑáéíóú]+)\s+de\s+(\d{4})", RegexOptions.IgnoreCase)] private static partial Regex InicioRx();
    [GeneratedRegex(@"CIERRE:\s*(\d{1,2})\s*de\s+([A-Za-zñÑáéíóú]+)\s+de\s+(\d{4})", RegexOptions.IgnoreCase)] private static partial Regex CierreRx();
    [GeneratedRegex(@"FORMA DE POSTULACI[ÓO]N.*?(https?://[^\s<>""']+)", RegexOptions.IgnoreCase | RegexOptions.Singleline)] private static partial Regex UrlRx();
    [GeneratedRegex(@"<(script|style)[^>]*>.*?</\1>", RegexOptions.Singleline | RegexOptions.IgnoreCase)] private static partial Regex ScriptRx();
    [GeneratedRegex(@"<[^>]+>")] private static partial Regex TagRx();
    [GeneratedRegex(@"\s+")] private static partial Regex EspaciosRx();
    [GeneratedRegex(@"<h[1-6][^>]*>(.*?)</h[1-6]>", RegexOptions.Singleline | RegexOptions.IgnoreCase)] private static partial Regex EncabezadoRx();

    private static string Limpio(string html) => EspaciosRx().Replace(WebUtility.HtmlDecode(TagRx().Replace(html, " ")), " ").Trim();

    /// <summary>Divide el texto de la convocatoria por sus encabezados. Cada apartado se recorta a 1.500 caracteres y se devuelven como máximo 10, para mostrarlos tal cual dentro de la aplicación.</summary>
    private static IReadOnlyList<SeccionOficial> Secciones(string html)
    {
        var res = new List<SeccionOficial>();
        var marcas = EncabezadoRx().Matches(html);
        for (int i = 0; i < marcas.Count && res.Count < 10; i++)
        {
            var ini = marcas[i].Index + marcas[i].Length;
            var fin = i + 1 < marcas.Count ? marcas[i + 1].Index : html.Length;
            var titulo = Limpio(marcas[i].Groups[1].Value);
            var texto = Limpio(html[ini..fin]);
            if (titulo.Length == 0 || texto.Length == 0) continue;
            res.Add(new SeccionOficial(titulo, texto.Length > 1500 ? texto[..1500] + "…" : texto));
        }
        return res;
    }

    private static DateOnly? Fecha(Match m)
    {
        if (!m.Success) return null;
        var mes = Array.IndexOf(Meses, m.Groups[2].Value.ToLowerInvariant());
        return mes >= 0 && int.TryParse(m.Groups[1].Value, out var d) && int.TryParse(m.Groups[3].Value, out var y) ? new DateOnly(y, mes + 1, d) : null;
    }

    /// <summary>Interpreta la respuesta JSON (una lista de convocatorias con <c>title.rendered</c>, <c>slug</c>, <c>link</c> y <c>content.rendered</c>).</summary>
    public static IReadOnlyList<ConvocatoriaFia> Parsear(string json)
    {
        var res = new List<ConvocatoriaFia>();
        using var doc = JsonDocument.Parse(json);
        if (doc.RootElement.ValueKind != JsonValueKind.Array) return res;
        foreach (var e in doc.RootElement.EnumerateArray())
        {
            string Texto(string a, string b) => e.TryGetProperty(a, out var x) && x.TryGetProperty(b, out var y) ? y.GetString() ?? "" : "";
            var titulo = WebUtility.HtmlDecode(TagRx().Replace(Texto("title", "rendered"), "")).Trim();
            var html = ScriptRx().Replace(Texto("content", "rendered"), " ");
            var texto = EspaciosRx().Replace(WebUtility.HtmlDecode(TagRx().Replace(html, " ")), " ");
            var cierres = CierreRx().Matches(texto).Select(Fecha).Where(f => f is not null).Select(f => f!.Value).ToList();
            var url = UrlRx().Match(texto) is { Success: true } m ? m.Groups[1].Value.TrimEnd('.', ',', ')') : null;
            if (url is not null && !(Uri.TryCreate(url, UriKind.Absolute, out var u) && u.Scheme == Uri.UriSchemeHttps && u.Host.EndsWith("fia.cl", StringComparison.OrdinalIgnoreCase))) url = null;
            res.Add(new ConvocatoriaFia(titulo, e.TryGetProperty("slug", out var s) ? s.GetString() ?? "" : "", e.TryGetProperty("link", out var l) ? l.GetString() ?? "" : "",
                Fecha(InicioRx().Match(texto)), cierres.Count > 0 ? cierres.Max() : null, url, Secciones(html)));
        }
        return res;
    }

    /// <summary>
    /// Estado de un programa de tipo «fia»: se busca la convocatoria cuyo título o dirección contiene alguna de las claves y se compara con <paramref name="hoy"/> (hora de Chile).
    /// El cierre vale hasta el final de ese día. Sin lista (no se pudo consultar) o sin convocatoria encontrada el estado es «Desconocida»: no se marca como cerrado por no haber podido mirar.
    /// </summary>
    public static EstadoPlazo Resolver(Ventana v, IReadOnlyList<ConvocatoriaFia>? lista, DateOnly hoy)
    {
        if (lista is null) return new(EstadoPostulacion.Desconocida, "No pudimos consultar hoy las convocatorias de FIA. Confirma las fechas en su sitio.", []);
        var claves = (v.Claves ?? []).Select(k => k.ToLowerInvariant()).ToList();
        var c = lista.Where(x => x.Cierre is not null && claves.Any(k => (x.Slug + " " + x.Titulo).ToLowerInvariant().Contains(k)))
                     .OrderByDescending(x => x.Cierre).FirstOrDefault();
        if (c is null) return new(EstadoPostulacion.Desconocida, "No encontramos esta convocatoria en el sitio de FIA.", []);
        var enlaces = new List<Enlace>();
        if (c.UrlPostulacion is not null) enlaces.Add(new Enlace("Ir a postular en la plataforma de FIA", c.UrlPostulacion));
        if (c.Link.StartsWith("https://", StringComparison.Ordinal)) enlaces.Add(new Enlace("Bases y detalle de la convocatoria (FIA)", c.Link));
        if (c.Inicio is { } i && hoy < i) return new(EstadoPostulacion.Proxima, $"La postulación abre el {i:dd-MM-yyyy}.", enlaces, c.Secciones);
        if (hoy <= c.Cierre!.Value) return new(EstadoPostulacion.Abierta, $"Postulación abierta hasta el {c.Cierre:dd-MM-yyyy} a las 15:00 (hora de Chile).", enlaces, c.Secciones);
        return new(EstadoPostulacion.Cerrada, $"Fuera de plazo: la postulación cerró el {c.Cierre:dd-MM-yyyy}.", enlaces, c.Secciones);
    }
}
