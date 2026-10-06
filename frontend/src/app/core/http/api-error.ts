// extrae un mensaje legible de una respuesta de error de la API: { message } o el primer
// mensaje de un ValidationProblem ({ errors: { Campo: ['mensaje'] } }); si no hay, el fallback
export function extractApiError(err: unknown, fallback: string): string {
  const body = (err as { error?: { message?: string; errors?: Record<string, string[]> } })?.error;

  if (body?.message) {
    return body.message;
  }

  const first = body?.errors ? Object.values(body.errors).flat()[0] : undefined;
  return first ?? fallback;
}
