import { Injectable, computed, signal } from '@angular/core';
import { UserRole, UserSession, SessionState } from './session.model';

const STORAGE_KEY = 'istpetdev_session';

export const DEMO_PROFILES: Record<UserRole, UserSession> = {
  OPERADOR: {
    id: 'usr-018f-a912-4011',
    name: 'Carlos Mendoza',
    email: 'operador@istpetdev.ec',
    role: 'OPERADOR',
    roleLabel: 'Operador de Despacho Logístico',
    facility: 'Centro Logístico Principal Quito (HUB-UIO)',
    avatarInitials: 'CM',
    token: 'jwt-mock-operador-token'
  },
  SUPERVISOR: {
    id: 'usr-018f-a912-4022',
    name: 'Dra. Andrea Morales',
    email: 'supervisor@istpetdev.ec',
    role: 'SUPERVISOR',
    roleLabel: 'Supervisora de Abastecimiento',
    facility: 'Hospital Teodoro Maldonado Carbo (Guayaquil)',
    avatarInitials: 'AM',
    token: 'jwt-mock-supervisor-token'
  },
  AUDITOR: {
    id: 'usr-018f-a912-4033',
    name: 'Ing. Roberto Silva',
    email: 'auditor@istpetdev.ec',
    role: 'AUDITOR',
    roleLabel: 'Auditor de Custodia y Calidad',
    facility: 'Control Nacional de Trazabilidad',
    avatarInitials: 'RS',
    token: 'jwt-mock-auditor-token'
  },
  ADMIN: {
    id: 'usr-018f-a912-4044',
    name: 'Bryan - Líder Técnico',
    email: 'admin@istpetdev.ec',
    role: 'ADMIN',
    roleLabel: 'Administrador de Plataforma',
    facility: 'IstpetDev DevOps & Cloud Hub',
    avatarInitials: 'BL',
    token: 'jwt-mock-admin-token'
  },
  WarehouseOperator: {
    id: 'usr-wh-ec-002',
    name: 'Elena Bodega',
    email: 'elena.bodega@istpetdev.ec',
    role: 'WarehouseOperator',
    roleLabel: 'Operador de Bodega y Rotulado',
    facility: 'Centro Logístico Principal Quito (HUB-UIO)',
    avatarInitials: 'EB',
    token: 'jwt-mock-warehouse-token'
  },
  CarrierDriver: {
    id: 'usr-driver-ec-001',
    name: 'Carlos Chofer',
    email: 'carlos.chofer@istpetdev.ec',
    role: 'CarrierDriver',
    roleLabel: 'Conductor de Custodia en Ruta',
    facility: 'Ruta Activa Sierra Centro',
    avatarInitials: 'CC',
    token: 'jwt-mock-driver-token'
  },
  LogisticsManager: {
    id: 'usr-mgr-ec-003',
    name: 'Santiago Supervisor',
    email: 'santiago.supervisor@istpetdev.ec',
    role: 'LogisticsManager',
    roleLabel: 'Supervisor de Logística y Despacho',
    facility: 'Oficina Central de Operaciones',
    avatarInitials: 'SS',
    token: 'jwt-mock-manager-token'
  }
};

@Injectable({
  providedIn: 'root'
})
export class SessionStore {
  private readonly state = signal<SessionState>(this.loadInitialState());

  readonly isAuthenticated = computed(() => this.state().isAuthenticated);
  readonly currentUser = computed(() => this.state().user);
  readonly userRole = computed(() => this.state().user?.role ?? null);

  private loadInitialState(): SessionState {
    try {
      const stored = localStorage.getItem(STORAGE_KEY);
      if (stored) {
        const parsed = JSON.parse(stored);
        if (parsed && parsed.isAuthenticated && parsed.user) {
          return parsed;
        }
      }
    } catch {
      // Si falla localStorage en entornos aislados
    }

    return {
      isAuthenticated: false,
      user: null,
      lastLoginAt: null
    };
  }

  private saveState(newState: SessionState): void {
    this.state.set(newState);
    try {
      localStorage.setItem(STORAGE_KEY, JSON.stringify(newState));
    } catch {
      // Ignorar cuota o fallo de storage
    }
  }

  loginWithCredentials(email: string, password: string): { success: boolean; error?: string } {
    const trimmedEmail = email.trim().toLowerCase();
    
    // Búsqueda en perfiles demo o credenciales
    const matchedProfile = Object.values(DEMO_PROFILES).find(p => p.email.toLowerCase() === trimmedEmail);
    
    if (matchedProfile) {
      this.saveState({
        isAuthenticated: true,
        user: matchedProfile,
        lastLoginAt: new Date().toISOString()
      });
      return { success: true };
    }

    // Permitir cualquier correo válido con contraseña si es para prueba
    if (trimmedEmail.includes('@') && password.length >= 4) {
      const genericUser: UserSession = {
        id: 'usr-' + Math.random().toString(36).substring(2, 9),
        name: trimmedEmail.split('@')[0].toUpperCase(),
        email: trimmedEmail,
        role: 'OPERADOR',
        roleLabel: 'Operador del Sistema',
        facility: 'Estación Central',
        avatarInitials: trimmedEmail.substring(0, 2).toUpperCase(),
        token: 'jwt-mock-auth-token'
      };

      this.saveState({
        isAuthenticated: true,
        user: genericUser,
        lastLoginAt: new Date().toISOString()
      });
      return { success: true };
    }

    return {
      success: false,
      error: 'Credenciales inválidas. Por favor verifique el correo y la contraseña.'
    };
  }

  loginAsDemo(role: UserRole): void {
    const profile = DEMO_PROFILES[role];
    if (profile) {
      this.saveState({
        isAuthenticated: true,
        user: profile,
        lastLoginAt: new Date().toISOString()
      });
    }
  }

  setSession(user: UserSession): void {
    this.saveState({
      isAuthenticated: true,
      user,
      lastLoginAt: new Date().toISOString()
    });
  }

  logout(): void {
    this.saveState({
      isAuthenticated: false,
      user: null,
      lastLoginAt: null
    });
  }
}
