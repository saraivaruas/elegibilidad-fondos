# Cómo contribuir

Gracias por querer mejorar ElegibilidadFondos. Hay tres tipos de aporte, de más a menos frecuente:

## 1. Corregir o agregar un programa en el catálogo
El catálogo es `src/Elegibilidad/Catalogo/fondos.json`. Reglas para cambiarlo:
1. **Toda regla lleva `fuente`**: el enlace oficial (Sercotec, Corfo, ANID, FIA, ChileAtiende, bases de la convocatoria) donde se lee el requisito.
2. **Actualiza `revisado`** del programa con la fecha en que comparaste las reglas con la fuente.
3. **No inventes ni supongas.** Lo que la fuente no dice, no se convierte en regla. Si un dato no se puede comprobar, la regla queda «por confirmar» (el campo del perfil se deja en `null`).
4. En `vigencia` explica cómo funcionan las fechas del programa; no guardes una fecha que se vuelva falsa en semanas.
5. Agrega o ajusta una prueba en `tests/` para la regla nueva.

## 2. Cambiar el motor
- El motor es **determinista**: sin IA ni llamadas externas. No se aceptan cambios que lo vuelvan no determinista.
- Todo cambio de comportamiento lleva pruebas.

## 3. Reportar un error o una regla desactualizada
Abre un *issue* con el programa, la regla, el enlace a la fuente actual y qué dice ahora.

## Antes de enviar un cambio
```bash
dotnet test
```
Debe pasar todo. Envía un *pull request* pequeño y descrito: qué cambia y por qué. Al enviarlo aceptas que tu aporte se publique bajo la licencia MIT del proyecto.

Ver también el [código de conducta](CODE_OF_CONDUCT.md).
