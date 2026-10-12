import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { SkeletonComponent } from '../skeleton/skeleton.component';
import { BadgeComponent, BadgeVariant } from '../badge/badge.component';
import { LucideTrendingUp, LucideTrendingDown, LucideMinus } from '@lucide/angular';

export type StatTrendDirection = 'up' | 'down' | 'neutral';

@Component({
  selector: 'ui-stat-card',
  standalone: true,
  imports: [
    CommonModule,
    SkeletonComponent,
    BadgeComponent,
    LucideTrendingUp,
    LucideTrendingDown,
    LucideMinus
  ],
  template: `
    @if (loading) {
      <!-- Estado de carga: Skeleton Screen simulando la geometría exacta (Regla de los 5 estados) -->
      <ui-skeleton variant="card"></ui-skeleton>
    } @else {
      <div class="bg-white p-6 rounded-xl border border-slate-200 shadow-sm flex flex-col justify-between transition-all hover:border-slate-300">
        <div>
          <!-- Encabezado con etiqueta y badge semántico opcional -->
          <div class="flex items-center justify-between gap-2 mb-2">
            <span class="text-xs font-semibold uppercase tracking-wider text-slate-500 truncate">
              {{ title }}
            </span>
            @if (badgeText) {
              <ui-badge
                [variant]="badgeVariant"
                size="sm"
                [showIcon]="badgeShowIcon"
                [label]="badgeText">
              </ui-badge>
            }
          </div>

          <!-- Cifra / KPI Principal en tipografía monoespaciada obligatoria (Sec 1.3) -->
          <div
            class="text-3xl font-extrabold tracking-tight font-mono text-slate-900"
            [ngClass]="valueColorClass">
            {{ value }}
            @if (unit) {
              <span class="text-base font-medium font-sans text-slate-500 ml-1">{{ unit }}</span>
            }
          </div>
        </div>

        <!-- Pie de métrica: Tendencia y descripción -->
        <div class="mt-3 pt-2 border-t border-slate-100 flex flex-col gap-1">
          @if (trend) {
            <div class="flex items-center gap-1.5 text-xs font-medium" [ngClass]="trendColorClass">
              @switch (trend) {
                @case ('up') {
                  <svg lucideTrendingUp [size]="14" class="stroke-[2.2] shrink-0"></svg>
                }
                @case ('down') {
                  <svg lucideTrendingDown [size]="14" class="stroke-[2.2] shrink-0"></svg>
                }
                @default {
                  <svg lucideMinus [size]="14" class="stroke-[2.2] shrink-0"></svg>
                }
              }
              <span>{{ trendValue }}</span>
              @if (trendLabel) {
                <span class="text-slate-500 font-normal">({{ trendLabel }})</span>
              }
            </div>
          }

          @if (description) {
            <p class="text-xs text-slate-500 truncate" [title]="description">
              {{ description }}
            </p>
          }
        </div>
      </div>
    }
  `,
  styles: [`
    :host {
      display: block;
      width: 100%;
    }
  `]
})
export class StatCardComponent {
  @Input({ required: true }) title!: string;
  @Input({ required: true }) value!: string | number;
  @Input() unit?: string;
  @Input() description?: string;
  @Input() loading = false;

  // Badge contextual
  @Input() badgeText?: string;
  @Input() badgeVariant: BadgeVariant = 'neutral';
  @Input() badgeShowIcon = true;

  // Indicador de tendencia
  @Input() trend?: StatTrendDirection;
  @Input() trendValue?: string;
  @Input() trendLabel?: string;
  @Input() trendPositive: 'up' | 'down' = 'up'; // si 'up' es positivo (ej. ahorro) o si 'down' es positivo (ej. costos)

  // Color personalizado para el valor principal
  @Input() valueColor: 'default' | 'critical' | 'warning' | 'success' | 'transit' = 'default';

  get valueColorClass(): string {
    switch (this.valueColor) {
      case 'critical':
        return 'text-red-600';
      case 'warning':
        return 'text-accent-600';
      case 'success':
        return 'text-secondary-800';
      case 'transit':
        return 'text-primary-600';
      case 'default':
      default:
        return 'text-slate-900';
    }
  }

  get trendColorClass(): string {
    if (!this.trend || this.trend === 'neutral') return 'text-slate-500';

    const isGood = this.trend === this.trendPositive;
    return isGood ? 'text-secondary-700 font-semibold' : 'text-red-600';
  }
}
