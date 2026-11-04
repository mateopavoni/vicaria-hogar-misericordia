// Calendario semanal de visitas de la Casa de
// Convivencia — independiente del calendario general del Centro Barrial,
// con su propia fuente de datos. Visible/editable solo para Referente, DirectoraDeCasona
// y CoordinadorDeCasaConvivencia (ver permissions.ts, visitas.*); Escucha no tiene
// ningún permiso acá, ni siquiera de lectura.
// `start` es fecha y hora local en texto ("YYYY-MM-DDTHH:mm:00"), sin zona horaria.

export type VisitStatus = 'pending' | 'done' | 'cancelled';

export interface Resident {
  id: string;
  fullName: string;
}

export interface Visit {
  id: string;
  residentId: string;
  // nombre del residente, para no buscarlo en la lista en cada vista
  residentName: string;
  visitorName: string;
  start: string;
  durationMinutes: number;
  status: VisitStatus;
  // motivo opcional de cancelación; vacío si no se canceló o se canceló sin motivo
  cancellationReason?: string | null;
}

export interface CreateVisitDto {
  residentId: string;
  visitorName: string;
  start: string;
  durationMinutes: number;
  // el solapamiento es una advertencia: el backend responde 409 y se reenvía con true
  // cuando la persona confirma que quiere guardar igual
  allowOverlap?: boolean;
}

export type UpdateVisitDto = CreateVisitDto;
