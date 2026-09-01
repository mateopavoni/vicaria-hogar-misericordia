// 0 = Ambulatory, 1 = Resident (el backend no serializa el enum como string, ver PersonType.cs)
export enum PersonType {
  Ambulatory = 0,
  Resident = 1,
}

export interface ContactRequest {
  firstName: string;
  lastName: string | null;
  phone: string | null;
  address: string | null;
}

export interface CreateSocialRecordRequest {
  firstName: string;
  lastName: string | null;
  dni: string | null;
  personType: PersonType | null;
  generalNotes: string | null;
  hasDocumentation: boolean;
  contact: ContactRequest | null;
}

export interface CreateSocialRecordResponse {
  personId: string;
  id: string;
}
