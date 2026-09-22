# Constraints

Restricciones de dominio no negociables. No son "known issues" (no son bugs) ni "open questions" (no están en discusión) — son reglas que cualquier feature nueva debe respetar.

## CRÍTICA — Información especialmente sensible (abusos) nunca se carga al sistema

**Regla:** información especialmente sensible (ej. situaciones de abuso) no debe cargarse en el sistema — ni en un campo de texto, ni en un adjunto — ni tampoco en papel. Se maneja exclusivamente de forma verbal entre el equipo de referentes.

**Tipo:** `ASSUMPTION` — acuerdo humano/organizacional, **no hay ningún mecanismo técnico que lo haga cumplir**. Búsqueda de "abuso" / "información sensible" en todo el repo (código, docs, validadores): sin resultados. Un campo como `GeneralNotes` de la ficha (2000 caracteres, sin restricción de contenido) aceptaría esa información sin ningún aviso si alguien la escribe ahí por error.

**Por qué queda marcada CRÍTICA:** el sistema es Trabajo Final Integrador con cliente real, y maneja datos de personas en situación de calle con problemáticas de consumo — un incidente de este tipo no es hipotético. Cualquier feature nueva que toque campos de texto libre (observaciones, historia de vida, notas) debería tener este acuerdo presente, aunque hoy no haya enforcement técnico que lo respalde. Si el equipo quiere pasar esto de `ASSUMPTION` a `DECISION` con enforcement real (ej. un aviso en el campo, una política de contenido), es una decisión de producto — no se implementó acá.

## DECISION, enforced — Ningún campo obligatorio salvo el nombre

**Regla:** ningún campo de la ficha es obligatorio salvo el nombre, porque muchos asistentes no tienen documentación ni domicilio fijo. Cualquier funcionalidad nueva que agregue una barrera de datos obligatorios para el ingreso debe señalarse, no implementarse por defecto (regla explícita en `/PROJECT.md`).

**Tipo:** `DECISION`, **sí está enforced en código** — `CreateSocialRecordDtoValidator` solo tiene `RuleFor(x => x.FirstName).NotEmpty()`; todo el resto de los campos (apellido, DNI, fecha de nacimiento, teléfono, motivo de ingreso, situación habitacional, ocupación, notas) son opcionales, con `MaximumLength` nomás. Verificado en `dev-backend`.

**Cómo aplicarlo:** cualquier DTO/validador nuevo para fichas o entidades relacionadas a personas atendidas debe seguir el mismo patrón — campos opcionales salvo lo estrictamente indispensable, y si algo se marca obligatorio, señalarlo explícitamente en la PR para que se revise contra este principio.
