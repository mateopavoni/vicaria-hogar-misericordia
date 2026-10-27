import { Observable, firstValueFrom } from 'rxjs';

/**
 * Solo para tests: devuelve el error con el que falla un observable. Los servicios en memoria
 * rechazan con { error: { message } }, la misma forma que lee extractApiError.
 */
export async function errorOf(obs: Observable<unknown>): Promise<{ error: { message: string } }> {
  try {
    await firstValueFrom(obs);
  } catch (e) {
    return e as { error: { message: string } };
  }
  throw new Error('Se esperaba un error y no hubo ninguno');
}
