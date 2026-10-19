using System.Net;
using System.Text.RegularExpressions;

namespace Elegibilidad;

/// <summary>Una convocatoria del calendario oficial de Sercotec («Disponibles en tu región»).</summary>
public record ConvocatoriaSercotec(string Titulo, string Instrumento, string Region, string Inicio, string UrlPostular, bool Abierta);

/// <summary>Resultado de comprobar el plazo de postulación de un programa.</summary>
public record EstadoPlazo(EstadoPostulacion Estado, string Texto, IReadOnlyList<Enlace> Enlaces);

/// <summary>
/// Lee el calendario público de Sercotec (la respuesta HTML de <c>widgetsercotec.sercotec.cl/Calendario/TraerInformacion/?region=0&amp;tipoInstrumento=0</c>)
/// y decide si un programa tiene convocatorias abiertas. No hace llamadas de red: quien usa la biblioteca descarga el HTML y se lo entrega.
/// Cada convocatoria abierta trae el enlace exacto de «Postular».
/// </summary>
public static partial class CalendarioSercotec
{
    [GeneratedRegex(@"<li>(.*?)</li>", RegexOptions.Singleline)] private static partial Regex ItemRx();
    [GeneratedRegex(@"class=""inicio-btn""[^>]*>\s*Inicio\s+([^<]+?)\s*<")] private static partial Regex InicioRx();
    [GeneratedRegex(@"<a href=""[^""]*""[^>]*>([^<]+)</a>")] private static partial Regex TituloRx();
    [GeneratedRegex(@"class=""postular""")] private static partial Regex PostularRx();
    [GeneratedRegex(@"window\.open\('([^']+)'\)")] private static partial Regex UrlPostularRx();
    [GeneratedRegex(@"class=""instrumento"">([^<]*)<")] private static partial Regex InstrumentoRx();
    [GeneratedRegex(@"class=""region"">([^<]*)<")] private static partial Regex RegionRx();

    /// <summary>Solo se leen los bloques «en postulación» y «próximas»; los de actividades (charlas, talleres) no son convocatorias. Un enlace de postulación que no sea https de sercotec.cl se descarta.</summary>
    public static IReadOnlyList<ConvocatoriaSercotec> Parsear(string html)
    {
        var res = new List<ConvocatoriaSercotec>();
        int iAbiertas = html.IndexOf("CONVOCATORIAS EN POSTULACI", StringComparison.Ordinal);
        int iProximas = html.IndexOf("CONVOCATORIAS</b>", Math.Max(iAbiertas, 0) + 1, StringComparison.Ordinal);
        int iActividades = html.IndexOf("ACTIVIDADES CENTROS", StringComparison.Ordinal);

        void Leer(string tramo, bool abierta)
        {
            foreach (Match m in ItemRx().Matches(tramo))
            {
                var li = m.Groups[1].Value;
                var titulo = TituloRx().Match(li);
                if (!titulo.Success) continue;
                var url = abierta && PostularRx().IsMatch(li) ? UrlPostularRx().Match(li).Groups[1].Value : "";
                if (abierta && !(Uri.TryCreate(url, UriKind.Absolute, out var u) && u.Scheme == Uri.UriSchemeHttps && u.Host.EndsWith("sercotec.cl", StringComparison.OrdinalIgnoreCase))) continue;
                var inicio = InicioRx().Match(li);
                res.Add(new ConvocatoriaSercotec(WebUtility.HtmlDecode(titulo.Groups[1].Value).Trim(), WebUtility.HtmlDecode(InstrumentoRx().Match(li).Groups[1].Value).Trim(),
                    WebUtility.HtmlDecode(RegionRx().Match(li).Groups[1].Value).Trim(), inicio.Success ? inicio.Groups[1].Value.Trim() : "", url, abierta));
            }
        }

        if (iAbiertas >= 0)
        {
            var topes = new[] { iProximas, iActividades }.Where(i => i > iAbiertas).DefaultIfEmpty(html.Length);
            Leer(html[iAbiertas..topes.Min()], true);
        }
        if (iProximas >= 0) Leer(html[iProximas..(iActividades > iProximas ? iActividades : html.Length)], false);
        return res;
    }

    private static string Normalizar(string s)
    {
        var sb = new System.Text.StringBuilder();
        foreach (var ch in s.Normalize(System.Text.NormalizationForm.FormD))
            if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(ch) != System.Globalization.UnicodeCategory.NonSpacingMark) sb.Append(ch);
        return sb.ToString().ToLowerInvariant();
    }

    /// <summary>¿Esta convocatoria es del programa descrito por la ventana? Se compara el título y el instrumento con las claves (y exclusiones) del catálogo.</summary>
    public static bool Coincide(ConvocatoriaSercotec c, Ventana v)
    {
        var t = Normalizar(c.Titulo + " " + c.Instrumento);
        return (v.Claves ?? []).Any(k => t.Contains(Normalizar(k))) && !(v.Excluir ?? []).Any(k => t.Contains(Normalizar(k)));
    }

    /// <summary>
    /// Estado de un programa de tipo «externo». Con <paramref name="calendario"/> nulo (no se pudo consultar) el estado es «Desconocida»: nunca se marca como cerrado por no haber podido mirar.
    /// </summary>
    public static EstadoPlazo Resolver(Ventana v, IReadOnlyList<ConvocatoriaSercotec>? calendario)
    {
        if (calendario is null)
            return new(EstadoPostulacion.Desconocida, "No pudimos consultar hoy el calendario de Sercotec. Confirma las fechas en su sitio.", []);
        var mias = calendario.Where(c => Coincide(c, v)).ToList();
        var abiertas = mias.Where(c => c.Abierta).ToList();
        if (abiertas.Count > 0)
            return new(EstadoPostulacion.Abierta, $"Hay {abiertas.Count} convocatoria(s) abierta(s) en el calendario oficial de Sercotec.",
                abiertas.Select(c => new Enlace($"Postular · {c.Region}" + (c.Titulo.Length > 0 ? $" · {c.Titulo}" : ""), c.UrlPostular)).ToList());
        var proxima = mias.FirstOrDefault(c => !c.Abierta);
        if (proxima is not null)
            return new(EstadoPostulacion.Proxima, $"Próxima convocatoria: abre el {proxima.Inicio} ({proxima.Region}).", []);
        return new(EstadoPostulacion.Cerrada, "Fuera de plazo: no hay convocatorias abiertas de este programa en el calendario oficial de Sercotec.", []);
    }
}
