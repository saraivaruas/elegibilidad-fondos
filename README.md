# ElegibilidadFondos

Proyecto de **Jorge Luis Saraiva Ruas** ([@saraivaruas](https://github.com/saraivaruas)) · MIT

Motor de elegibilidad para fondos y programas de financiamiento en Chile (Corfo, Sercotec, ANID, FIA, FOSIS, ProChile, INDAP, GORE, Municipalidad de Providencia y otros).
Responde a una empresa tres cosas: **a qué programas puede postular, cuáles le faltan por confirmar y qué le falta para calificar**.

Se usa dentro de DataHub Providencia (herramienta «Diagnóstico») y también sirve por separado.

## Alcance real del proyecto
**Software libre bajo licencia [MIT](LICENSE)**: puedes usarlo, copiarlo, modificarlo y distribuirlo, también con fines comerciales, conservando el aviso de copyright. Es un proyecto independiente y comunitario de una persona; no es un producto oficial.

**Qué es**
- Una biblioteca .NET con un motor de reglas **determinista** (sin IA) y un **catálogo selectivo de 29 programas** de fomento y financiamiento (Sercotec, Corfo, FOSIS, ANID, FIA, ProChile, INDAP, Municipalidad de Providencia, GORE RM y Google), pensado para emprendedores y empresas de Providencia y de Chile.
- Evalúa, con datos que entrega quien la usa, si una empresa **cumple, no cumple o falta confirmar** los requisitos medibles de cada programa, y explica qué le falta. Cada regla enlaza a su fuente oficial y cada programa tiene la fecha en que se revisó.
- Se usa en producción dentro de [DataHub Providencia](https://hub-data.inge-innova.com) (herramienta «Diagnóstico»), pero funciona por separado.

**Qué no es**
- **No es oficial ni está afiliado** a Corfo, Sercotec, FOSIS, ANID, FIA, ProChile, INDAP, la Municipalidad de Providencia, el Gobierno Regional ni el Ministerio de Economía. Los nombres y contenidos de los programas pertenecen a sus instituciones.
- **No garantiza nada**: orienta. Una empresa que «cumple» puede igual ser rechazada, y una que «no cumple» puede tener una excepción que las bases explican. Las bases oficiales vigentes mandan.
- **No postula ni se conecta con los sistemas de las instituciones**, ni consulta registros de personas o empresas. El motor solo recibe los datos del perfil que le entrega quien lo usa.
- **No es un directorio completo de la oferta del Estado.** [Chile Emprende](https://www.chileatiende.gob.cl/fichas/144389-chile-emprende), la plataforma del Ministerio de Economía, reúne más de cien instrumentos y es más amplia. Este proyecto es más acotado a propósito: cubre menos programas, pero con reglas verificables, fuente por regla y el estado «por confirmar» cuando falta un dato. Es un complemento, no un reemplazo.

**Límites que debes conocer**
- El catálogo es **selectivo**: se incluyen programas cuyos requisitos se pueden medir con el perfil y confirmar en una fuente oficial. Los que no cumplen eso quedan fuera (ver «Estado»).
- Solo se evalúan los requisitos que el perfil puede medir. **No se evalúan** el aporte propio, el foco regional ni los criterios de puntaje de cada concurso.
- Los plazos y requisitos cambian en cada convocatoria. El catálogo lo mantiene una persona, de forma manual: cada programa lleva su fecha de revisión y una alerta semanal avisa cuando una revisión está por cumplir un año, pero **puede estar desactualizado** entre revisiones.
- Se aceptan correcciones: si ves una regla o un plazo desactualizado, abre un *issue* con el enlace a la fuente actual (ver [CONTRIBUTING.md](CONTRIBUTING.md)).

*English summary:* open-source (MIT) deterministic eligibility engine and a curated catalog of 29 Chilean funding programs, used in production in DataHub Providencia. It guides, it does not guarantee; it is independent and not affiliated with any of the institutions it describes. It is narrower than the official [Chile Emprende](https://www.chileatiende.gob.cl/fichas/144389-chile-emprende) portal by design: fewer programs, but machine-checkable rules with an official source for each one.

## Cómo funciona
- **Sin IA y sin llamadas externas**: el mismo perfil da siempre el mismo resultado.
- Cada programa es un dato en `src/Elegibilidad/Catalogo/fondos.json`: sus reglas, la **fuente oficial** de cada regla y la **fecha en que se revisó**.
- Tres estados por regla y por programa: `Cumple`, `NoCumple` y `PorConfirmar` (falta un dato; no es un descarte).
- `Radar(perfil)`: todos los programas ordenados (cumple, por confirmar, descartados) con puntaje de afinidad y lo que falta.
- `QueDesbloquea(perfil)`: qué arreglo habilita más programas.
- Una regla puede aplicar solo en ciertos casos (`cuando`), por ejemplo las ventas mínimas de Crece según la antigüedad.
- **Plazo de postulación**: cada programa trae una `Ventana` (permanente, por fechas o consultada a la fuente). `CalendarioSercotec` lee el calendario público de Sercotec y `ConvocatoriasFia` la API pública de convocatorias de FIA (fechas reales y enlace de postulación) y entrega, por programa, si hay convocatorias abiertas con el enlace exacto de «Postular»; si no se pudo consultar, el estado es «desconocido», nunca «cerrado».
- **Texto oficial dentro de la aplicación**: `PaginaOficial` (Sercotec y Corfo) y `ConvocatoriasFia` entregan los apartados oficiales ya en texto plano. Los sitios oficiales no permiten mostrarse dentro de otra página (X-Frame-Options / frame-ancestors, también Corfo), por eso se lee el contenido en el servidor y solo el envío de la postulación se hace en el sitio de cada institución.
- **Catálogo validado al cargarlo**: un campo de perfil mal escrito, un programa o una regla repetidos, un operador desconocido o una fecha de revisión inválida detienen la carga con un mensaje claro (antes un error de tipeo dejaba el programa «por confirmar» para siempre sin avisar). La lista de campos válidos es `Perfil.CamposConocidos`.
- **Lógica de tres valores (Kleene) también en las reglas condicionadas**: si falta el dato de la condición pero la regla ya se cumple por sí sola, el resultado es «cumple»; solo queda «por confirmar» si la regla tampoco se cumple o se desconoce.
- **`VersionCatalogo`**: identificador de 12 caracteres del contenido exacto de `fondos.json`; permite saber con qué reglas se calculó un resultado guardado o impreso.
- **`RevisionesPendientes(hoy, avisarDiasAntes)`**: programas cuya revisión contra las bases oficiales venció o vence pronto (365 días). El flujo semanal «Vigencia» de GitHub falla 30 días antes y avisa por correo.
- **`SinRequisitosMedibles`**: un programa sin reglas (acompañamiento o servicios sin dinero) sale «cumple» con afinidad 100 solo porque no hay nada que comprobar; quien lo muestre debe decirlo así.
- **Ficha completa** de cada programa (proceso, plazos, criterios de evaluación, gastos y enlace oficial) para mostrarla dentro de tu aplicación.

## Uso
```csharp
var motor = CatalogoFondos.Cargar();
var perfil = new Perfil().Con("iniciado_actividades", true).Con("antiguedad_meses", 8).Con("ventas_uf", (double?)null);
foreach (var r in motor.Radar(perfil)) Console.WriteLine($"{r.Veredicto} {r.Afinidad}% {r.Programa.Nombre}");
```
Campos del perfil: `iniciado_actividades`, `capital_riesgo`, `al_dia`, `antiguedad_meses`, `ventas_uf`, `edad`, `es_mujer`, `persona_juridica`, `proyecto_innovacion`,
`sector_agroalimentario`, `cursos_sercotec`, `cursos_ruta_digital`, `es_almacen`, `curso_almacenes`, `cliente_cdn_12m`, `sin_cotizaciones_6m`, `rubro_masculinizado`,
`ventas_24m` y `segmento_modo_empleo` (los dos últimos los calcula quien arma el perfil). Un dato sin informar se deja en `null`. Un campo nuevo se agrega a `Perfil.CamposConocidos` y a esta lista.

## Pruebas
```bash
dotnet test
```

## Mantener el catálogo
1. Toda regla nueva lleva `fuente` (enlace oficial de las bases) y el programa su `revisado`.
2. Lo que las bases no permitan comprobar se deja sin regla: no se inventa.
3. Pasado un año de `revisado`, volver a comparar con las bases (el flujo «Vigencia» avisa 30 días antes).
4. `dotnet test` valida el catálogo completo; un campo desconocido o un programa repetido lo detienen.

## Estado
Catálogo de 29 programas con ficha, enlace de postulación y control de plazo, revisados entre el 13 y el 21 de octubre de 2026 contra sus páginas oficiales:
- **Sercotec (12)**: Capital Semilla Emprende, Abeja Emprende, Pioneras, Semilla Modo Empleo, Crece, Crece Sostenible, Digitaliza tu Almacén, Kit Digital, Pymes Globales, Negocios Digitales, MejoraNegocios, Promoción y canales.
- **Corfo (4)**: Crédito MIPYME, Garantía Pro-Inversión, Crea y Valida, Súmate a Innovar.
- **ANID (1)**: Startup Ciencia. **FIA (1)**: Proyectos de innovación (Línea 3). **Google (2)**: Start y Scale.
- **FOSIS (3)**: Emprendamos (Básico y Avanzado), Emprendamos Semilla y Emprendamos Grupal (Región Metropolitana).
- **ProChile (1)**: concursos de promoción de exportaciones. **INDAP (2)**: Crédito de corto plazo y Programa de Desarrollo de Inversiones (PDI).
- **Municipalidad de Providencia (2)**: Comunidad EmprendePRO y Hub Providencia, y Revive Barrio · Mujer Emprende + PRO.
- **GORE RM (1)**: HUB Metropolitano (talleres, asesorías, ruedas de negocios y espacios, sin aporte en dinero).

Los programas sin aporte en dinero (Comunidad EmprendePRO y HUB Metropolitano) no traen reglas de elegibilidad: las páginas oficiales no declaran requisitos, y no se inventan. Varios programas de 2026 figuran como «postulación cerrada» según su plazo oficial (FOSIS, ProChile, Revive Barrio).

Quedan fuera, a propósito o por falta de datos confiables:
- Programas de organizaciones (barrios comerciales, ferias libres, gremios, cooperativas): la audiencia no es una empresa.
- ANID IDeA: sus beneficiarias son universidades y entidades sin fines de lucro.
- Semilla de Asignación Flexible (Corfo) y los fondos concursables de los gobiernos regionales (GORE): las bases cambian por convocatoria y región y no se pudieron confirmar. Del GORE RM solo se incluye el HUB Metropolitano, que es un servicio y no un concurso.

Los porcentajes de aporte propio y el foco regional están en las bases de cada convocatoria, no en las fichas: el motor no los evalúa.

## Aviso importante
Este proyecto **orienta, no garantiza**. Los requisitos vienen de páginas públicas de cada programa y cambian en cada convocatoria; el resultado del motor no asegura que una postulación sea admisible ni constituye asesoría. Verifica siempre las bases oficiales vigentes antes de postular. Cada regla indica su fuente y cada programa su fecha de revisión.

## Contribuir y seguridad
La rama `main` está protegida: todo cambio entra por *pull request* y lo aprueba el propietario del proyecto.

Ver [CONTRIBUTING.md](CONTRIBUTING.md), [CODE_OF_CONDUCT.md](CODE_OF_CONDUCT.md) y [SECURITY.md](SECURITY.md). Historial de cambios en [CHANGELOG.md](CHANGELOG.md).

## Licencia
[MIT](LICENSE) © 2026 Jorge Luis Saraiva Ruas ([@saraivaruas](https://github.com/saraivaruas)), propietario y autor del proyecto. La licencia MIT cubre el código y el catálogo de este repositorio. Los datos del catálogo resumen información pública de Sercotec, Corfo, FOSIS, ANID, FIA, ProChile, INDAP, la Municipalidad de Providencia, el GORE RM y Google, siempre con enlace a la fuente; los nombres, marcas y textos oficiales de los programas pertenecen a sus respectivas instituciones, con las que este proyecto no tiene relación.
