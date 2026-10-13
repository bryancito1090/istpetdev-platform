export type UserRole =
  | 'OPERADOR'
  | 'SUPERVISOR'
  | 'AUDITOR'
  | 'ADMIN'
  | 'WarehouseOperator'
  | 'CarrierDriver'
  | 'LogisticsManager';

export interface UserSession {
  id: string;
  name: string;
  email: string;
  role: UserRole;
  roleLabel: string;
  facility: string;
  token?: string;
  avatarInitials: string;
}

export interface SessionState {
  isAuthenticated: boolean;
  user: UserSession | null;
  lastLoginAt: string | null;
}
