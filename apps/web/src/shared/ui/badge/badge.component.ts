import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  LucideTriangleAlert,
  LucideTruck,
  LucideCheck,
  LucideActivity
} from '@lucide/angular';

export type BadgeVariant = 'critical' | 'warning' | 'transit' | 'success' | 'neutral';
export type BadgeSize = 'sm' | 'md';

@Component({
  selector: 'ui-badge',
  standalone: true,
  imports: [
    CommonModule,
    LucideTriangleAlert,
    LucideTruck,
    LucideCheck,
    LucideActivity
  ],
  template: `
    <span
      [attr.role]="'status'"
      [attr.aria-label]="ariaLabelText"
      class="inline-flex items-center font-medium border rounded-md transition-colors"
      [ngClass]="[variantClasses, sizeClasses]">
      
      <!-- Indicador de pulso / dot opcional -->
      @if (withDot) {
        <span class="relative flex h-2 w-2 mr-1.5">
          @if (pulseDot) {
            <span
              class="animate-ping absolute inline-flex h-full w-full rounded-full opacity-75"
              [ngClass]="dotColorClass">
            </span>
          }
          <span class="relative inline-flex rounded-full h-2 w-2" [ngClass]="dotColorClass"></span>
        </span>
      }

      <!-- Iconografía obligatoria para accesibilidad WCAG 2.2 (no depender solo del color) -->
      @if (showIcon) {
        <span class="inline-flex items-center shrink-0 mr-1.5" aria-hidden="true">
          @switch (variant) {
            @case ('critical') {
              <svg lucideTriangleAlert [size]="iconSize" class="stroke-[2.2] text-red-600"></svg>
            }
            @case ('warning') {
              <svg lucideTriangleAlert [size]="iconSize" class="stroke-[2.2] text-accent-600"></svg>
            }
            @case ('transit') {
              <svg lucideTruck [size]="iconSize" class="stroke-[2.2] text-primary-600"></svg>
            }
            @case ('success') {
              <svg lucideCheck [size]="iconSize" class="stroke-[2.5] text-secondary-700"></svg>
            }
            @default {
              <svg lucideActivity [size]="iconSize" class="stroke-[2.2] text-slate-500"></svg>
            }
          }
        </span>
      }

      <!-- Contenido textual (Label directo o proyección) -->
      <span class="truncate">
        @if (label) {
          {{ label }}
        } @else {
          <ng-content></ng-content>
        }
      </span>
    </span>
  `,
  styles: [`
    :host {
      display: inline-flex;
      vertical-align: middle;
    }
  `]
})
export class BadgeComponent {
  @Input() variant: BadgeVariant = 'neutral';
  @Input() size: BadgeSize = 'sm';
  @Input() label?: string;
  @Input() withDot = false;
  @Input() pulseDot = false;
  @Input() showIcon = true;
  @Input() ariaLabel?: string;

  get iconSize(): number {
    return this.size === 'sm' ? 12 : 14;
  }

  get ariaLabelText(): string {
    if (this.ariaLabel) return this.ariaLabel;
    const prefix = {
      critical: 'Alerta crítica',
      warning: 'Advertencia operativa',
      transit: 'En tránsito logístico',
      success: 'Completado con éxito',
      neutral: 'Estado'
    }[this.variant];
    return this.label ? `${prefix}: ${this.label}` : prefix;
  }

  get variantClasses(): string {
    switch (this.variant) {
      case 'critical':
        return 'bg-red-50 text-red-700 border-red-200';
      case 'warning':
        return 'bg-accent-50 text-accent-800 border-accent-200';
      case 'transit':
        return 'bg-primary-50 text-primary-800 border-primary-200';
      case 'success':
        return 'bg-secondary-50 text-secondary-900 border-secondary-300';
      case 'neutral':
      default:
        return 'bg-slate-100 text-slate-700 border-slate-200';
    }
  }

  get sizeClasses(): string {
    switch (this.size) {
      case 'md':
        return 'text-xs px-2.5 py-1';
      case 'sm':
      default:
        return 'text-[11px] px-2 py-0.5';
    }
  }

  get dotColorClass(): string {
    switch (this.variant) {
      case 'critical':
        return 'bg-red-500';
      case 'warning':
        return 'bg-accent-500';
      case 'transit':
        return 'bg-primary-500';
      case 'success':
        return 'bg-secondary-500';
      case 'neutral':
      default:
        return 'bg-slate-400';
    }
  }
}
