using Elegibilidad;
using Xunit;

public class MotorTests
{
    private static readonly MotorElegibilidad Motor = CatalogoFondos.Cargar();
    private static Programa P(string id) => Motor.Catalogo.First(p => p.Id == id);

    private static Perfil Base() => new Perfil()
        .Con("iniciado_actividades", true).Con("capital_riesgo", false).Con("al_dia", true)
        .Con("antiguedad_meses", 30).Con("ventas_uf", 500).Con("cursos_sercotec", true).Con("edad", 40).Con("es_mujer", false);

    [Fact]
    public void Catalogo_es_valido_y_trazable()
    {
        Assert.Equal(Motor.Catalogo.Count, Motor.Catalogo.Select(p => p.Id).Distinct().Count());
        foreach (var p in Motor.Catalogo)
        {
            Assert.StartsWith("https://", p.Url);
            Assert.True(DateOnly.TryParse(p.Revisado, out _), $"{p.Id}: fecha de revisión inválida");
            Assert.Equal(p.Reglas.Count, p.Reglas.Select(r => r.Id).Distinct().Count());
            foreach (var r in p.Reglas) Assert.StartsWith("https://", r.Fuente);
            Assert.NotNull(p.Ficha);
            Assert.False(string.IsNullOrWhiteSpace(p.Ficha!.Resumen), $"{p.Id}: la ficha no tiene resumen");
            Assert.False(string.IsNullOrWhiteSpace(p.Ficha.Fuente), $"{p.Id}: la ficha no indica su fuente");
        }
    }

    [Fact]
    public void Perfil_completo_que_cumple()
    {
        var r = Motor.Evaluar(P("sercotec-crece"), Base());
        Assert.Equal(Veredicto.Cumple, r.Veredicto);
        Assert.Equal(100, r.Afinidad);
        Assert.Empty(r.Faltantes);
    }

    [Fact]
    public void Dato_ausente_queda_por_confirmar_no_descartado()
    {
        var perfil = Base().Con("ventas_uf", (double?)null);
        Assert.Equal(Veredicto.PorConfirmar, Motor.Evaluar(P("corfo-credito-mipyme"), perfil).Veredicto);
    }

    [Fact]
    public void Un_incumplimiento_descarta_aunque_falten_otros_datos()
    {
        var perfil = Base().Con("ventas_uf", (double?)null).Con("al_dia", false);
        Assert.Equal(Veredicto.NoCumple, Motor.Evaluar(P("sercotec-crece"), perfil).Veredicto);
    }

    [Theory]
    [InlineData(199, Veredicto.NoCumple)]
    [InlineData(200, Veredicto.Cumple)]
    [InlineData(25000, Veredicto.Cumple)]
    [InlineData(25001, Veredicto.NoCumple)]
    public void Crece_ventas_entre_200_y_25000_uf(int ventas, Veredicto esperado) =>
        Assert.Equal(esperado, Motor.Evaluar(P("sercotec-crece"), Base().Con("ventas_uf", ventas)).Veredicto);

    [Fact]
    public void Semilla_es_para_quien_no_ha_iniciado_actividades()
    {
        var nuevo = Base().Con("iniciado_actividades", false);
        Assert.Equal(Veredicto.Cumple, Motor.Evaluar(P("sercotec-semilla-emprende"), nuevo).Veredicto);
        Assert.Equal(Veredicto.NoCumple, Motor.Evaluar(P("sercotec-semilla-emprende"), Base()).Veredicto);
    }

    [Theory]
    [InlineData(25, false, true)]    // joven de 18 a 29
    [InlineData(40, true, true)]     // mujer de 30 a 54 (el cálculo lo hace quien arma el perfil)
    public void Segmento_modo_empleo_se_usa_como_dato_calculado(int edad, bool mujer, bool segmento)
    {
        var perfil = Base().Con("iniciado_actividades", false).Con("sin_cotizaciones_6m", true).Con("segmento_modo_empleo", segmento).Con("edad", edad).Con("es_mujer", mujer);
        Assert.Equal(Veredicto.Cumple, Motor.Evaluar(P("sercotec-semilla-modo-empleo"), perfil).Veredicto);
    }

    [Fact]
    public void Regla_condicionada_aplica_solo_cuando_se_da_la_condicion()
    {
        const string json = """[{"id":"x","nombre":"x","entidad":"x","descripcion":"x","url":"https://x","montoAprox":"x","comoPostular":"x","vigencia":"x","revisado":"2026-01-01","reglas":[{"id":"r","campo":"ventas","op":"gte","valor":200,"texto":"t","siFalla":"s","fuente":"https://x","cuando":{"campo":"meses","op":"gte","valor":12}}]}]""";
        var m = MotorElegibilidad.DesdeJson(json);
        var p = m.Catalogo[0];
        Assert.Equal(Veredicto.Cumple, m.Evaluar(p, new Perfil().Con("meses", 6).Con("ventas", 10)).Veredicto);      // no aplica
        Assert.Equal(Veredicto.NoCumple, m.Evaluar(p, new Perfil().Con("meses", 12).Con("ventas", 10)).Veredicto);  // aplica y falla
        Assert.Equal(Veredicto.PorConfirmar, m.Evaluar(p, new Perfil().Con("ventas", 10)).Veredicto);               // no se sabe si aplica
    }

    [Theory]
    [InlineData("corfo-credito-mipyme")]
    [InlineData("corfo-garantia-pro-inversion")]
    [InlineData("sercotec-crece")]
    [InlineData("google-cloud-start")]
    public void Sin_inicio_de_actividades_no_califican_los_programas_para_negocios_formalizados(string id) =>
        Assert.Equal(Veredicto.NoCumple, Motor.Evaluar(P(id), Base().Con("iniciado_actividades", false)).Veredicto);

    [Fact]
    public void Radar_ordena_cumple_antes_que_por_confirmar_antes_que_descartado()
    {
        var orden = Motor.Radar(Base()).Select(r => r.Veredicto).ToList();
        Assert.Equal(orden.OrderBy(v => v).ToList(), orden);
    }

    [Fact]
    public void QueDesbloquea_cuenta_solo_programas_con_una_unica_falla()
    {
        var perfil = Base().Con("al_dia", false);
        var d = Motor.QueDesbloquea(perfil);
        Assert.Contains(d, x => x.SiFalla.Contains("al día") && x.Programas == 1);
    }

    [Fact]
    public void Catalogo_con_operador_desconocido_se_rechaza()
    {
        const string json = """[{"id":"x","nombre":"x","entidad":"x","descripcion":"x","url":"https://x","montoAprox":"x","comoPostular":"x","vigencia":"x","revisado":"2026-01-01","reglas":[{"id":"r","campo":"c","op":"cerca","valor":1,"texto":"t","siFalla":"s","fuente":"https://x"}]}]""";
        Assert.Throws<InvalidDataException>(() => MotorElegibilidad.DesdeJson(json));
    }
}
