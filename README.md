# ElegibilidadFondos

Proyecto de **Jorge Luis Saraiva Ruas** ([@saraivaruas](https://github.com/saraivaruas)) · MIT

Motor de elegibilidad para fondos y programas de financiamiento en Chile (Corfo, Sercotec, ANID, FIA, GORE y otros).
Responde a una empresa tres cosas: **a qué programas puede postular, cuáles le faltan por confirmar y qué le falta para calificar**.

Se usa dentro de DataHub Providencia (herramienta «Diagnóstico») y también sirve por separado.

## Cómo funciona
- **Sin IA y sin llamadas externas**: el mismo perfil da siempre el mismo resultado.
- Cada programa es un dato en `src/Elegibilidad/Catalogo/fondos.json`: sus reglas, la **fuente oficial** de cada regla y la **fecha en que se revisó**.
- Tres estados por regla y por programa: `Cumple`, `NoCumple` y `PorConfirmar` (falta un dato; no es un descarte).
- `Radar(perfil)`: todos los programas ordenados (cumple, por confirmar, descartados) con puntaje de afinidad y lo que falta.
- `QueDesbloquea(perfil)`: qué arreglo habilita más programas.
- Una regla puede aplicar solo en ciertos casos (`cuando`), por ejemplo las ventas mínimas de Crece según la antigüedad.

## Uso
```csharp
var motor = CatalogoFondos.Cargar();
var perfil = new Perfil().Con("iniciado_actividades", true).Con("antiguedad_meses", 8).Con("ventas_uf", (double?)null);
foreach (var r in motor.Radar(perfil)) Console.WriteLine($"{r.Veredicto} {r.Afinidad}% {r.Programa.Nombre}");
```
Campos del perfil: `iniciado_actividades`, `capital_riesgo`, `al_dia`, `antiguedad_meses`, `ventas_uf`, `edad`, `es_mujer`, `persona_juridica`, `proyecto_innovacion`,
`sector_agroalimentario`, `cursos_sercotec`, `cursos_ruta_digital`, `es_almacen`, `curso_almacenes`, `cliente_cdn_12m`, `sin_cotizaciones_6m`, `rubro_masculinizado`,
`ventas_24m` y `segmento_modo_empleo` (los dos últimos los calcula quien arma el perfil). Un dato sin informar se deja en `null`.

## Pruebas
```bash
dotnet test
```

## Mantener el catálogo
1. Toda regla nueva lleva `fuente` (enlace oficial de las bases) y el programa su `revisado`.
2. Lo que las bases no permitan comprobar se deja sin regla: no se inventa.
3. Pasado un año de `revisado`, volver a comparar con las bases.

## Estado
Catálogo de 20 programas, revisados el 19-oct-2026 contra sus páginas oficiales:
- **Sercotec (12)**: Capital Semilla Emprende, Abeja Emprende, Pioneras, Semilla Modo Empleo, Crece, Crece Sostenible, Digitaliza tu Almacén, Kit Digital, Pymes Globales, Negocios Digitales, MejoraNegocios, Promoción y canales.
- **Corfo (4)**: Crédito MIPYME, Garantía Pro-Inversión, Crea y Valida, Súmate a Innovar.
- **ANID (1)**: Startup Ciencia. **FIA (1)**: Proyectos de innovación (Línea 3). **Google (2)**: Start y Scale.

Quedan fuera, a propósito o por falta de datos confiables:
- Programas de organizaciones (barrios comerciales, ferias libres, gremios, cooperativas): la audiencia no es una empresa.
- ANID IDeA: sus beneficiarias son universidades y entidades sin fines de lucro.
- Semilla de Asignación Flexible (Corfo) y los fondos de los gobiernos regionales (GORE): las bases cambian por convocatoria y región y no se pudieron confirmar.

Los porcentajes de aporte propio y el foco regional están en las bases de cada convocatoria, no en las fichas: el motor no los evalúa.

## Aviso importante
Este proyecto **orienta, no garantiza**. Los requisitos vienen de páginas públicas de cada programa y cambian en cada convocatoria; el resultado del motor no asegura que una postulación sea admisible ni constituye asesoría. Verifica siempre las bases oficiales vigentes antes de postular. Cada regla indica su fuente y cada programa su fecha de revisión.

## Contribuir y seguridad
Ver [CONTRIBUTING.md](CONTRIBUTING.md), [CODE_OF_CONDUCT.md](CODE_OF_CONDUCT.md) y [SECURITY.md](SECURITY.md). Historial de cambios en [CHANGELOG.md](CHANGELOG.md).

## Licencia
[MIT](LICENSE) © 2026 Jorge Luis Saraiva Ruas ([@saraivaruas](https://github.com/saraivaruas)), propietario y autor del proyecto. Los datos del catálogo resumen información pública de Sercotec, Corfo, ANID, FIA y Google; los nombres de los programas pertenecen a sus respectivas instituciones, con las que este proyecto no tiene relación.
