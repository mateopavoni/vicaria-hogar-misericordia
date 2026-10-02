// SCRUM-74/SCRUM-213/SCRUM-214: calendario semanal de visitas de la Casa de
// Convivencia — independiente del calendario general del Centro Barrial (SCRUM-28),
// con su propia fuente de datos. Visible/editable solo para Referente, DirectoraDeCasona
// y CoordinadorDeCasaConvivencia (ver permissions.ts, visitas.*); Escucha no tiene
// ningún permiso acá, ni siquiera de lectura.
// NOTA: el backend de esta feature todavía no existe (se decidió trabajar solo en el
// frontend, como el resto del calendario). Contrato asumido:
//   GET  /api/casa-convivencia-visitas?desde=&hasta=   (fechas YYYY-MM-DD)
//   GET  /api/casa-convivencia-visitas/residentes      (solo personas con personType
//        Resident, ver PersonType en social-record.interface.ts — se asume un
//        endpoint propio y liviano en vez de reusar /api/social-records/list, que es
//        paginado y no está pensado para alimentar un selector)
//   POST /api/casa-convivencia-visitas
//   PUT  /api/casa-convivencia-visitas/{id}
//   PATCH /api/casa-convivencia-visitas/{id}/estado    (cambiar a pendiente/
//        realizada/cancelada; la cancelación manda el motivo opcional)
// `start` es datetime-local en texto (sin zona horaria, "YYYY-MM-DDTHH:mm:00"),
// mismo criterio que calendar-event.interface.ts y todayLocalIso().

export type VisitStatus = 'pending' | 'done' | 'cancelled';

export interface Resident {
  id: string;
  fullName: string;
}

export interface Visit {
  id: string;
  residentId: string;
  // nombre del residente desnormalizado para no tener que resolverlo contra la lista
  // de residentes en cada vista del calendario — se asume que el backend lo incluye.
  residentName: string;
  visitorName: string;
  start: string;
  durationMinutes: number;
  status: VisitStatus;
  // motivo opcional de cancelación (AC: "las visitas canceladas quedan registradas
  // con motivo opcional"). null/undefined si nunca se canceló, o si se canceló sin
  // motivo.
  cancellationReason?: string | null;
}

export interface CreateVisitDto {
  residentId: string;
  visitorName: string;
  start: string;
  durationMinutes: number;
}

export type UpdateVisitDto = CreateVisitDto;
