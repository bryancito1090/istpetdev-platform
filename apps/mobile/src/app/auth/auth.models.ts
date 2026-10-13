/**
 * Modelos y tipos de autenticación y sesión para la PWA Móvil.
 * Cumple con Spec 004 (T8: Sesión, roles y guards) y NFR-03 (seguridad de tokens en memoria).
 */

export type FieldUserRole = 
  | 'CarrierDriver'       // Conductor de custodia / transporte en ruta
  | 'WarehouseOperator'   // Operador de bodega / despacho
  | 'LogisticsManager'   // Administrador / supervisor logístico
  | 'Auditor';            // Auditor de trazabilidad

export interface MobileUserProfile {
  id: string;
  name: string;
  email: string;
  role: FieldUserRole;
  roleLabel: string;
  vehicleId?: string;       // Solo para CarrierDriver (ej: VH-EC-101)
  warehouseId?: string;     // Solo para WarehouseOperator (ej: WH-CENTRAL-01)
  avatarInitials: string;
}

export interface MobileSessionState {
  isAuthenticated: boolean;
  user: MobileUserProfile | null;
  /**
   * Token JWT Bearer.
   * IMPORTANTE (NFR-03): Se mantiene únicamente en memoria volátil o sessionStorage transitorio.
   * NUNCA se persiste en IndexedDB durable.
   */
  accessToken: string | null;
  expiresAtUtc: string | null;
}

export const DEMO_DRIVER_PROFILE: MobileUserProfile = {
  id: 'usr-driver-ec-001',
  name: 'Carlos Chofer (Demo)',
  email: 'carlos.chofer@istpetdev.ec',
  role: 'CarrierDriver',
  roleLabel: 'Conductor de Custodia en Ruta',
  vehicleId: 'VH-EC-101',
  avatarInitials: 'CC'
};

export const DEMO_WAREHOUSE_PROFILE: MobileUserProfile = {
  id: 'usr-wh-ec-002',
  name: 'Elena Bodega (Demo)',
  email: 'elena.bodega@istpetdev.ec',
  role: 'WarehouseOperator',
  roleLabel: 'Operador de Despacho y Custodia',
  warehouseId: 'WH-CENTRAL-01',
  avatarInitials: 'EB'
};

export const DEMO_MANAGER_PROFILE: MobileUserProfile = {
  id: 'usr-mgr-ec-003',
  name: 'Santiago Supervisor (Demo)',
  email: 'santiago.supervisor@istpetdev.ec',
  role: 'LogisticsManager',
  roleLabel: 'Supervisor de Logística y Rutas',
  avatarInitials: 'SS'
};
