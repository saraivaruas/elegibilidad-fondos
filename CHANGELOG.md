# Cambios

Propietario y autor: Jorge Luis Saraiva Ruas ([@saraivaruas](https://github.com/saraivaruas)).

Formato basado en [Keep a Changelog](https://keepachangelog.com/es-ES/1.1.0/); versiones según [SemVer](https://semver.org/lang/es/).

## [0.3.0] - 2026-10-19
### Agregado
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
