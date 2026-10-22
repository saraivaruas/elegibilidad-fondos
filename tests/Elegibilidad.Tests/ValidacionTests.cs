using Elegibilidad;
using Xunit;

/// <summary>Validación del catálogo al cargarlo, lógica de tres valores de las reglas condicionadas, versión del catálogo y vigencia de las revisiones.</summary>
public class ValidacionTests
{
    // Un catálogo mínimo válido, con una regla ajustable, para probar cada validación por separado.
    private static string Prog(string id = "p1", string reglas = "", string revisado = "2026-10-20") =>
        $$$"""
        {"id":"{{{id}}}","nombre":"Programa","entidad":"Entidad","descripcion":"d","url":"https://x.cl","montoAprox":"m","comoPostular":"c","vigencia":"v","revisado":"{{{revisado}}}","reglas":[{{{reglas}}}]}
        """;

    private static string Json(string reglas = "", string id = "p1", string revisado = "2026-10-20") => "[" + Prog(id, reglas, revisado) + "]";

    private static string Regla(string id = "r1", string campo = "es_mujer", string op = "eq", string valor = "1", string cuando = "") =>
        $$$"""{"id":"{{{id}}}","campo":"{{{campo}}}","op":"{{{op}}}","valor":{{{valor}}},"texto":"t","siFalla":"s","fuente":"https://x.cl"{{{cuando}}}}""";

    [Fact]
    public void Un_campo_de_perfil_mal_escrito_se_rechaza_al_cargar()
    {
        var ex = Assert.Throws<InvalidDataException>(() => MotorElegibilidad.DesdeJson(Json(Regla(campo: "es_mujr"))));
        Assert.Contains("es_mujr", ex.Message);
        Assert.Contains("p1/r1", ex.Message);
    }

    [Fact]
    public void Un_campo_mal_escrito_en_cuando_tambien_se_rechaza()
    {
        var cuando = ""","cuando":{"campo":"edadd","op":"gte","valor":18}""";
        var ex = Assert.Throws<InvalidDataException>(() => MotorElegibilidad.DesdeJson(Json(Regla(cuando: cuando))));
        Assert.Contains("edadd", ex.Message);
    }

    [Fact]
    public void Se_rechazan_programas_y_reglas_repetidos_y_fechas_invalidas()
    {
        Assert.Throws<InvalidDataException>(() => MotorElegibilidad.DesdeJson("[" + Prog("p1") + "," + Prog("p1") + "]"));
        Assert.Throws<InvalidDataException>(() => MotorElegibilidad.DesdeJson(Json(Regla("r1") + "," + Regla("r1"))));
        Assert.Throws<InvalidDataException>(() => MotorElegibilidad.DesdeJson(Json(Regla(), revisado: "mañana")));
    }

    [Fact]
    public void Todos_los_campos_del_catalogo_real_son_conocidos()
    {
        var motor = CatalogoFondos.Cargar(); // si hubiera un campo desconocido, Cargar() ya habría fallado
        var usados = motor.Catalogo.SelectMany(p => p.Reglas).SelectMany(r => new[] { r.Campo }.Concat(r.Cuando is { } c ? [c.Campo] : [])).Distinct();
        Assert.All(usados, c => Assert.Contains(c, Perfil.CamposConocidos));
    }

    // «Si (edad >= 30) entonces ventas_uf >= 100»: la condición es la edad.
    private static Regla Condicionada() => new("r", "ventas_uf", "gte", 100, "t", "s", "f", new Regla.Condicion("edad", "gte", 30));

    [Fact]
    public void Condicion_desconocida_pero_regla_ya_cumplida_es_cumple()
    {
        var perfil = new Perfil().Con("ventas_uf", 500.0); // la edad no se sabe
        Assert.Equal(Veredicto.Cumple, MotorElegibilidad.EvaluarRegla(Condicionada(), perfil));
    }

    [Fact]
    public void Condicion_desconocida_y_regla_incumplida_o_desconocida_queda_por_confirmar()
    {
        Assert.Equal(Veredicto.PorConfirmar, MotorElegibilidad.EvaluarRegla(Condicionada(), new Perfil().Con("ventas_uf", 10.0)));
        Assert.Equal(Veredicto.PorConfirmar, MotorElegibilidad.EvaluarRegla(Condicionada(), new Perfil()));
    }

    [Fact]
    public void Condicion_conocida_se_comporta_como_antes()
    {
        Assert.Equal(Veredicto.Cumple, MotorElegibilidad.EvaluarRegla(Condicionada(), new Perfil().Con("edad", 20.0).Con("ventas_uf", 1.0))); // no aplica
        Assert.Equal(Veredicto.NoCumple, MotorElegibilidad.EvaluarRegla(Condicionada(), new Perfil().Con("edad", 40.0).Con("ventas_uf", 1.0)));
        Assert.Equal(Veredicto.Cumple, MotorElegibilidad.EvaluarRegla(Condicionada(), new Perfil().Con("edad", 40.0).Con("ventas_uf", 200.0)));
    }

    [Fact]
    public void La_version_del_catalogo_es_estable_y_cambia_con_el_contenido()
    {
        var a = MotorElegibilidad.DesdeJson(Json(Regla()));
        Assert.Equal(12, a.VersionCatalogo.Length);
        Assert.Equal(a.VersionCatalogo, MotorElegibilidad.DesdeJson(Json(Regla())).VersionCatalogo);
        Assert.Equal(a.VersionCatalogo, MotorElegibilidad.DesdeJson(Json(Regla()).Replace("\n", "\r\n")).VersionCatalogo); // Windows y Linux dan lo mismo
        Assert.NotEqual(a.VersionCatalogo, MotorElegibilidad.DesdeJson(Json(Regla(valor: "0"))).VersionCatalogo);
        Assert.Equal(12, CatalogoFondos.Cargar().VersionCatalogo.Length);
    }

    [Fact]
    public void Las_revisiones_vencen_al_ano_y_se_avisa_con_anticipacion()
    {
        var motor = MotorElegibilidad.DesdeJson(Json(Regla(), revisado: "2026-01-10"));
        var vence = new DateOnly(2027, 1, 10);
        Assert.Empty(motor.RevisionesPendientes(new DateOnly(2026, 12, 1)));
        Assert.Empty(motor.RevisionesPendientes(vence.AddDays(-31), avisarDiasAntes: 30));
        var aviso = Assert.Single(motor.RevisionesPendientes(vence.AddDays(-30), avisarDiasAntes: 30));
        Assert.Equal(30, aviso.DiasRestantes);
        Assert.Equal(vence, aviso.Vence);
        var vencido = Assert.Single(motor.RevisionesPendientes(vence.AddDays(5)));
        Assert.Equal(-5, vencido.DiasRestantes);
    }

    [Fact]
    public void Un_programa_sin_reglas_se_marca_como_sin_requisitos_medibles()
    {
        var sin = MotorElegibilidad.DesdeJson(Json());
        var r = sin.Evaluar(sin.Catalogo[0], new Perfil());
        Assert.True(r.SinRequisitosMedibles);
        Assert.Equal(Veredicto.Cumple, r.Veredicto);
        var con = MotorElegibilidad.DesdeJson(Json(Regla()));
        Assert.False(con.Evaluar(con.Catalogo[0], new Perfil()).SinRequisitosMedibles);
    }

    /// <summary>
    /// Alerta de vigencia: se ejecuta cada semana desde GitHub (workflow «Vigencia»), no en cada cambio, porque depende de la fecha de hoy y no del código.
    /// Falla 30 días antes de que venza la revisión de algún programa, para volver a compararlo con sus bases oficiales.
    /// </summary>
    [Fact]
    [Trait("Category", "Vigencia")]
    public void Ninguna_revision_esta_vencida_ni_vence_en_30_dias()
    {
        var pendientes = CatalogoFondos.Cargar().RevisionesPendientes(DateOnly.FromDateTime(DateTime.UtcNow), avisarDiasAntes: 30);
        Assert.True(pendientes.Count == 0,
            "Revisar contra las bases oficiales: " + string.Join("; ", pendientes.Select(x => $"{x.Programa.Id} (revisado {x.Programa.Revisado}, " + (x.DiasRestantes < 0 ? $"venció hace {-x.DiasRestantes} días" : $"vence en {x.DiasRestantes} días") + ")")));
    }
}
