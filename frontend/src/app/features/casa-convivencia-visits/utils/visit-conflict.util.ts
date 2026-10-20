import { Visit } from '../interfaces/visit.interface';

/**
 * SCRUM-74/SCRUM-214 (AC): "el sistema alerta si hay dos visitas al mismo residente
 * en el mismo horario". Es una ALERTA, no un bloqueo: el AC no pide impedir guardar,
 * así que esto se usa para mostrar un aviso (en el formulario al guardar, y como
 * ícono sobre la visita en el calendario), nunca para deshabilitar el botón de guardar.
 *
 * Una visita cancelada no cuenta como conflicto (ya no va a pasar), así que se excluye.
 */
export function findConflictingVisits(
  candidate: { id?: string; residentId: string; start: string; durationMinutes: number },
  existingVisits: Visit[],
): Visit[] {
  if (!candidate.residentId || !candidate.start || !candidate.durationMinutes) {
    return [];
  }

  const candidateStart = new Date(candidate.start).getTime();
  const candidateEnd = candidateStart + candidate.durationMinutes * 60_000;

  return existingVisits.filter((visit) => {
    if (visit.id === candidate.id) {
      return false; // no comparar una visita que se está editando contra sí misma
    }
    if (visit.residentId !== candidate.residentId || visit.status === 'cancelled') {
      return false;
    }
    const visitStart = new Date(visit.start).getTime();
    const visitEnd = visitStart + visit.durationMinutes * 60_000;
    return candidateStart < visitEnd && visitStart < candidateEnd;
  });
}
