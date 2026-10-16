import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

export type StatBadgeHighlight = 'blue' | 'cyan' | 'mint' | 'neutral';

@Component({
  selector: 'ui-stat-badge',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div
      class="inline-flex items-center gap-2.5 rounded-xl px-3.5 py-2 glass-stat-badge transition-transform duration-200 select-none">
      
      <!-- Slot o Indicador de Icono / Dot -->
      <div
        class="flex items-center justify-center h-7 w-7 rounded-lg shrink-0"
        [ngClass]="iconBgClass">
        <ng-content select="[badge-icon]"></ng-content>
      </div>

      <div class="flex flex-col text-left">
        @if (value) {
          <span class="text-xs font-extrabold tracking-tight text-slate-900 leading-none">
            {{ value }}
          </span>
        }
        @if (label) {
          <span class="text-[11px] font-medium text-slate-500 mt-0.5 leading-none">
            {{ label }}
          </span>
        }
        <ng-content></ng-content>
      </div>
    </div>
  `
})
export class StatBadgeComponent {
  @Input() value?: string;
  @Input() label?: string;
  @Input() highlight: StatBadgeHighlight = 'blue';

  get iconBgClass(): string {
    switch (this.highlight) {
      case 'cyan':
        return 'bg-cyan-50 text-cyan-600';
      case 'mint':
        return 'bg-emerald-50 text-emerald-600';
      case 'neutral':
        return 'bg-slate-100 text-slate-600';
      case 'blue':
      default:
        return 'bg-blue-50 text-blue-600';
    }
  }
}
