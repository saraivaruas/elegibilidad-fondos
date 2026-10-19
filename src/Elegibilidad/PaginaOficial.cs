using System.Net;
using System.Text.RegularExpressions;

namespace Elegibilidad;

/// <summary>
/// Lee el texto oficial de la página pública de un programa de Sercotec o de Corfo (los apartados «¿Qué es?», «¿Qué apoyo entrega?», «¿Quiénes pueden postular?»,
/// «Pasos de Postulación», «IMPORTANTE»…) para mostrarlo dentro de la aplicación. No hace llamadas de red: quien usa la biblioteca descarga el HTML y se lo entrega.
/// Solo se conservan los apartados reconocidos, sin duplicados (las páginas de Sercotec repiten el contenido para celular), recortados a 1.500 caracteres y como texto plano.
/// </summary>
public static partial class PaginaOficial
{
    private static readonly string[] Reconocidos =
    [
        "que es", "que apoyo entrega", "que beneficio entrega", "quienes pueden acceder", "quienes pueden postular", "pasos de postulacion", "importante", "requisitos",
    ];

    [GeneratedRegex(@"<(script|style|nav|header|footer|noscript)[^>]*>.*?</\1>", RegexOptions.Singleline | RegexOptions.IgnoreCase)] private static partial Regex RuidoRx();
    [GeneratedRegex(@"<h([1-3])[^>]*>(.*?)</h\1>", RegexOptions.Singleline | RegexOptions.IgnoreCase)] private static partial Regex EncabezadoRx();
    [GeneratedRegex(@"<[^>]+>")] private static partial Regex TagRx();
    [GeneratedRegex(@"\s+")] private static partial Regex EspaciosRx();

    private static string Limpio(string html) => EspaciosRx().Replace(WebUtility.HtmlDecode(TagRx().Replace(html, " ")), " ").Trim();

    private static string Clave(string t)
    {
        var sb = new System.Text.StringBuilder();
        foreach (var ch in t.Normalize(System.Text.NormalizationForm.FormD))
            if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(ch) != System.Globalization.UnicodeCategory.NonSpacingMark && (char.IsLetterOrDigit(ch) || ch == ' ')) sb.Append(ch);
        return sb.ToString().Trim().ToLowerInvariant();
    }

    public static IReadOnlyList<SeccionOficial> Parsear(string html)
    {
        var limpio = RuidoRx().Replace(html, " ");
        var marcas = EncabezadoRx().Matches(limpio);
        var res = new List<SeccionOficial>();
        var vistos = new HashSet<string>();
        for (int i = 0; i < marcas.Count; i++)
        {
            var titulo = Limpio(marcas[i].Groups[2].Value);
            var clave = Clave(titulo);
            if (!Reconocidos.Contains(clave) || !vistos.Add(clave)) continue;
            var ini = marcas[i].Index + marcas[i].Length;
            var fin = i + 1 < marcas.Count ? marcas[i + 1].Index : limpio.Length;
            var texto = Limpio(limpio[ini..fin]);
            if (texto.Length == 0) continue;
            res.Add(new SeccionOficial(titulo, texto.Length > 1500 ? texto[..1500] + "…" : texto));
        }
        return res;
    }
}
