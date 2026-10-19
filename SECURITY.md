# Política de seguridad

## Versiones con soporte
Se corrige la última versión publicada en la rama `main`.

## Cómo reportar una vulnerabilidad
**No abras un *issue* público.** Escribe a **contacto@inge-innova.com** con el asunto «Seguridad ElegibilidadFondos», describiendo el problema y cómo reproducirlo. Respondemos en un plazo de 5 días hábiles y coordinamos contigo la divulgación una vez corregido.

## Alcance
La biblioteca no hace llamadas de red ni maneja credenciales: evalúa datos que le entrega quien la usa. Los riesgos relevantes son un catálogo malicioso o mal formado (el motor valida operadores y reglas al cargarlo) y resultados de elegibilidad incorrectos por reglas desactualizadas (ver el aviso del README).
