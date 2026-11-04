import { Visit } from '../interfaces/visit.interface';

/**
 * Busca otras visitas al mismo residente en el mismo horario. Es solo un aviso:
 * se muestra en el formulario y en el calendario, pero no impide guardar.
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
