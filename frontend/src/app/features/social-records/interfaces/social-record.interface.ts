// 0 = Ambulatory, 1 = Resident (el backend no serializa el enum como string, ver PersonType.cs)
export enum PersonType {
  Ambulatory = 0,
  Resident = 1,
}
export enum PersonStatus {
  Active = 0,
  Inactive = 1,
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
  dateOfBirth: string | null;
  phone: string | null;
  personType: PersonType | null;
  reasonForEntry: string | null;
  entryDate: string | null;
  housingSituation: string | null;
  overnightLocation: string | null;
  occupation: string | null;
  generalNotes: string | null;
  hasDocumentation: boolean;
  contact: ContactRequest | null;
}

export interface CreateSocialRecordResponse {
  personId: string;
  id: string;
}

export interface SocialRecordListItem {
  id: string;
  personId: string;
  firstName: string;
  lastName: string | null;
  dni: string | null;
  dateOfBirth: string | null;
  personType: PersonType;
  status: PersonStatus;
  lastModifiedAt: string;
}

export interface SocialRecordsResponse {
  items: SocialRecordListItem[];
  total: number;
  totalPages: number;
}


//   export interface SocialRecordSearchResult {
//     id: string;
//     personId: string;
//     firstName: string;
//     lastName: string | null;
//     dni: string | null;
//     dateOfBirth: string | null;
//     lastModifiedAt: string;
//     personType: PersonType;
// }