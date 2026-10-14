import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { ApiService } from '@shared/api';

interface StepStatus {
  id: number;
  title: string;
  desc: string;
  route: string;
  actionText: string;
  status: 'done' | 'pending' | 'attention';
  statusText: string;
}

@Component({
  selector: 'page-inicio',
  standalone: true,
  imports: [CommonModule, RouterLink],
  template: `
    <div class="max-w-4xl mx-auto px-4 py-8 space-y-8">
      <!-- Encabezado simple -->
      <div>
        <h1 class="text-2xl font-bold text-slate-900 tracking-tight">
          Flujo de Trabajo Operativo
        </h1>
        <p class="text-sm text-slate-500 mt-1">
          Aquí ves el estado general del sistema y el orden exacto de los pasos que debes seguir.
        </p>
      </div>

      <!-- Pasos del flujo -->
      <div class="space-y-4">
        @for (step of steps(); track step.id) {
          <div class="bg-white border border-slate-200 rounded-xl p-5 flex flex-col sm:flex-row sm:items-center justify-between gap-4">
            <div class="flex items-start gap-4">
              <div 
                class="w-8 h-8 rounded-full flex items-center justify-center font-bold text-sm shrink-0"
                [ngClass]="{
                  'bg-emerald-100 text-emerald-700': step.status === 'done',
                  'bg-amber-100 text-amber-700': step.status === 'attention',
                  'bg-slate-100 text-slate-600': step.status === 'pending'
                }">
                {{ step.id }}
              </div>
              <div>
                <div class="flex items-center gap-2">
                  <h2 class="text-base font-semibold text-slate-900">{{ step.title }}</h2>
                  <span 
                    class="text-xs px-2 py-0.5 rounded-full font-medium"
                    [ngClass]="{
                      'bg-emerald-50 text-emerald-700 border border-emerald-200': step.status === 'done',
                      'bg-amber-50 text-amber-700 border border-amber-200': step.status === 'attention',
                      'bg-slate-50 text-slate-600 border border-slate-200': step.status === 'pending'
                    }">
                    {{ step.statusText }}
                  </span>
                </div>
                <p class="text-xs text-slate-500 mt-1">{{ step.desc }}</p>
              </div>
            </div>

            <div class="shrink-0 self-end sm:self-auto">
              <a 
                [routerLink]="step.route"
                class="inline-flex items-center text-xs font-semibold px-4 py-2 rounded-lg bg-slate-900 text-white hover:bg-slate-800 transition-colors">
                {{ step.actionText }} →
              </a>
            </div>
          </div>
        }
      </div>

      <!-- Resumen de ayuda si el sistema está vacío -->
      <div class="bg-blue-50 border border-blue-200 rounded-xl p-4 text-xs text-blue-800">
        <p class="font-semibold mb-1">¿Sistema recién instalado o vacío?</p>
        <p class="text-blue-700">
          Comienza siempre por el <strong>Paso 1 (Datos)</strong>. Sube los archivos básicos de bodegas, hospitales y productos para que el sistema pueda proyectar la demanda de reabastecimiento.
        </p>
      </div>
    </div>
  `
})
export class InicioPage implements OnInit {
  private readonly apiService = inject(ApiService);

  readonly steps = signal<StepStatus[]>([
    {
      id: 1,
      title: '1. Cargar Datos Maestros',
      desc: 'Subir archivos CSV o Excel con bodegas, hospitales, productos, vehículos y consumos.',
      route: '/datos',
      actionText: 'Ir a Datos',
      status: 'pending',
      statusText: 'Revisar datos'
    },
    {
      id: 2,
      title: '2. Monitorear Alertas de Stock',
      desc: 'Verificar qué hospitales tienen insumos con menos de 5 días de cobertura.',
      route: '/alertas',
      actionText: 'Ver Alertas',
      status: 'pending',
      statusText: 'Pendiente'
    },
    {
      id: 3,
      title: '3. Planificar y Publicar Rutas',
      desc: 'Generar la ruta de despacho óptima y enviarla a los conductores para entrega física.',
      route: '/rutas',
      actionText: 'Ir a Rutas',
      status: 'pending',
      statusText: 'Pendiente'
    }
  ]);

  ngOnInit(): void {
    this.checkSystemStatus();
  }

  private checkSystemStatus(): void {
    // Verificamos si ya hay puntos de servicio y riesgos calculados
    this.apiService.getServicePoints().subscribe(points => {
      const hasPoints = points && points.length > 0;
      
      this.apiService.getRisks().subscribe(risks => {
        const hasRisks = risks && risks.items && risks.items.length > 0;
        const criticalCount = risks?.items?.filter(i => i.level === 'Critical' || i.level === 'High').length ?? 0;

        this.steps.update(list => list.map(s => {
          if (s.id === 1) {
            return {
              ...s,
              status: hasPoints ? 'done' : 'attention',
              statusText: hasPoints ? `${points.length} puntos registrados` : 'Sin datos'
            };
          }
          if (s.id === 2) {
            return {
              ...s,
              status: criticalCount > 0 ? 'attention' : (hasRisks ? 'done' : 'pending'),
              statusText: criticalCount > 0 ? `${criticalCount} alertas críticas` : (hasRisks ? 'Stock bajo control' : 'Sin evaluar')
            };
          }
          return s;
        }));
      });
    });
  }
}
