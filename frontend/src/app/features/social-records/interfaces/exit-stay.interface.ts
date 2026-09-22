import { ExitReason } from './stay.interface';

// lo que emite el modal (ExitStayModalComponent)
export interface RegisterExitRequest {
  exitDate: string;
  exitReason: ExitReason;
  exitDetail?: string | null;
}

// lo que espera el backend real (CasonaStayExitDto): exitReason es el enum StayExitReason como numero
export interface CasonaStayExitRequest {
  exitReason: number;
  reason?: string | null;
  newStatus?: number;
}