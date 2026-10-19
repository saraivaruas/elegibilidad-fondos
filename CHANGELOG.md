# Cambios

Formato basado en [Keep a Changelog](https://keepachangelog.com/es-ES/1.1.0/); versiones según [SemVer](https://semver.org/lang/es/).

## [0.1.0] - 2026-10-19
### Agregado
- Motor determinista de elegibilidad con tres estados (cumple, no cumple, por confirmar), puntaje de afinidad, radar y «qué arreglo desbloquea más programas».
- Catálogo inicial de 20 programas: Sercotec (12), Corfo (4), ANID (1), FIA (1) y Google (2), cada regla con su fuente oficial y fecha de revisión.
- Reglas condicionadas (`cuando`) y validación del catálogo al cargarlo.
- 19 pruebas automáticas.
