# Cambios

Propietario y autor: Jorge Luis Saraiva Ruas ([@saraivaruas](https://github.com/saraivaruas)).

Formato basado en [Keep a Changelog](https://keepachangelog.com/es-ES/1.1.0/); versiones según [SemVer](https://semver.org/lang/es/).

## [0.5.1] - 2026-10-22
### Documentación
- README: sección «Alcance real del proyecto» (software libre MIT, qué es, qué no es, límites conocidos y resumen en inglés), aclarando que es independiente, que orienta y no garantiza, y cómo se relaciona con Chile Emprende.
- Licencia: la nota de datos ahora nombra a todas las instituciones del catálogo.
- CONTRIBUTING y README: `main` está protegida y todo cambio se aprueba por *pull request*.

## [0.5.0] - 2026-10-22
### Agregado
- Validación del catálogo al cargarlo: campo de perfil desconocido (`Perfil.CamposConocidos`), programa o regla repetidos, operador desconocido y fecha de revisión inválida.
- `MotorElegibilidad.VersionCatalogo`: huella de 12 caracteres del contenido de `fondos.json`, estable entre sistemas operativos.
- `MotorElegibilidad.RevisionesPendientes(hoy, avisarDiasAntes)` y flujo semanal «Vigencia» en GitHub que avisa 30 días antes de que venza la revisión de un programa.
- `ResultadoPrograma.SinRequisitosMedibles` para los programas sin reglas.
- `CatalogoFondos.Compartido`: el catálogo cargado una sola vez por proceso (de solo lectura).
### Cambiado
- Reglas condicionadas (`cuando`) con lógica de tres valores de Kleene: con la condición desconocida, una regla que ya se cumple da «cumple» (antes quedaba «por confirmar»).
- El CI normal excluye la prueba de vigencia (depende de la fecha, no del código).

## [0.4.0] - 2026-10-21
### Agregado
- Catálogo ampliado de 20 a 29 programas, cada uno con ficha, enlace de postulación, plazo y reglas con su fuente oficial:
  - **FOSIS (3)**: Emprendamos (Básico y Avanzado), Emprendamos Semilla y Emprendamos Grupal (Región Metropolitana), con su plazo 2026 ya cerrado.
  - **ProChile (1)**: concursos de promoción de exportaciones (Silvoagropecuario, Industrias Creativas y Servicios).
  - **INDAP (2)**: Crédito de corto plazo y Programa de Desarrollo de Inversiones (PDI).
  - **Municipalidad de Providencia (2)**: Comunidad EmprendePRO y Hub Providencia (capacitación, asesoría y ferias; sin aporte en dinero) y Revive Barrio · Mujer Emprende + PRO (fondo concursable).
  - **GORE RM (1)**: HUB Metropolitano (talleres, asesorías, ruedas de negocios y espacios; sin aporte en dinero).
- Pruebas de los programas nuevos (entidades presentes, reglas de Emprendamos, programas sin reglas y plazos cerrados); 39 pruebas en total.
### Cambiado
- Los programas sin aporte en dinero no llevan reglas de elegibilidad: las páginas oficiales no declaran requisitos y no se inventan.

## [0.3.0] - 2026-10-19
### Agregado
- `PaginaOficial`: lee el texto oficial de la página de un programa de Sercotec o Corfo («¿Qué es?», «¿Quiénes pueden postular?», «Pasos de Postulación»…) para mostrarlo dentro de la aplicación; `Ficha.TextoOficialUrl` indica de dónde.
- `ConvocatoriasFia` entrega además el texto oficial por apartados (`Secciones`).
- `ConvocatoriasFia`: lee la API pública de FIA (`wp-json/wp/v2/convocatorias`) y obtiene el inicio, el último cierre y el enlace exacto de postulación.
### Cambiado
- Crédito Corfo MIPYME y Garantía Pro-Inversión apuntan al sitio oficial de Corfo (y su listado de intermediarios) en vez de ChileAtiende.
### Agregado
- `CalendarioSercotec`: lee el calendario oficial de Sercotec y resuelve el plazo de cada programa, con el enlace exacto de «Postular».
- `Ventana` por programa: permanente, por fechas o consultada a la fuente oficial (calendario de Sercotec), para marcar «postulación cerrada».
- Enlace oficial para postular en la ficha de cada programa (`Postulacion`), con nota sobre convocatorias regionales o cerradas.

## [0.2.0] - 2026-10-19
### Agregado
- Ficha de cada programa para mostrarla completa dentro de la plataforma: resumen, proceso, plazos, criterios de evaluación, gastos y notas, con su fuente. Fichas con criterios de evaluación: Sercotec (Semilla Emprende, Abeja, Pioneras, Crece, Crece Sostenible); el resto resume lo que informa la página oficial.

## [0.1.0] - 2026-10-19
### Agregado
- Motor determinista de elegibilidad con tres estados (cumple, no cumple, por confirmar), puntaje de afinidad, radar y «qué arreglo desbloquea más programas».
- Catálogo inicial de 20 programas: Sercotec (12), Corfo (4), ANID (1), FIA (1) y Google (2), cada regla con su fuente oficial y fecha de revisión.
- Reglas condicionadas (`cuando`) y validación del catálogo al cargarlo.
- 19 pruebas automáticas.
