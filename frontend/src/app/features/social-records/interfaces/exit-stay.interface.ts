import { ExitReason } from './stay.interface';

export interface RegisterExitRequest {
  exitDate: string;
  exitReason: ExitReason;
  exitDetail?: string | null;
}