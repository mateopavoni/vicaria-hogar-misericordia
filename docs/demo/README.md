# Datos de demostración

`seed-demo.mjs` carga datos de ejemplo de los Sprint 3, 4 y 5 en el stack local de Docker. Incluye:

- 45 personas con ficha completa, observaciones, historia de vida, campos de Cáritas y adjuntos.
- 4 meses de asistencias.
- Residentes con evaluación psiquiátrica (una por vencer y una vencida), esquemas de medicación, stock y agenda del día.
- Calendario general y personal.
- Colaboradores y visitas a la Casa de Convivencia.
- Cambios de estado e historial de respaldos.

Se corre sobre una base recién creada:

```bash
cd backend
docker compose stop api sql && docker compose rm -f sql
docker compose up -d sql api web
cd ..
node docs/demo/seed-demo.mjs
docker restart vicaria-api   # el proceso diario pasa a Inactivo a quienes dejaron de venir
```

Usuarios (contraseña `Test1234!`):

| Usuario | Rol | Nombre en la demo |
|---|---|---|
| referente@test.com | Referente | Laura Giménez |
| directora@test.com | Directora de Casa de Convivencia | Silvia Ramos |
| escucha@test.com | Escucha | Diego Ferreyra |
| coordinador@test.com | Coordinador | Martín Castro |
