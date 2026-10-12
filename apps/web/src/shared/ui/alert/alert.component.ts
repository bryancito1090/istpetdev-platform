import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

export type AlertVariant = 'info' | 'success' | 'warning' | 'error';

@Component({
  selector: 'ui-alert',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div
      role="alert"
      class="p-4 rounded-xl border text-sm flex items-start gap-3 transition-all"
      [ngClass]="variantClasses">
      
      <div class="shrink-0 mt-0.5">
        @switch (variant) {
          @case ('error') {
            <svg class="w-5 h-5 text-red-600" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" />
            </svg>
          }
          @case ('warning') {
            <svg class="w-5 h-5 text-amber-600" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
            </svg>
          }
          @case ('success') {
            <svg class="w-5 h-5 text-emerald-600" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
            </svg>
          }
          @default {
            <svg class="w-5 h-5 text-primary-600" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
            </svg>
          }
        }
      </div>

      <div class="flex-1">
        @if (title) {
          <span class="font-bold block tracking-tight mb-0.5">{{ title }}</span>
        }
        <div class="leading-relaxed">
          <ng-content></ng-content>
        </div>
      </div>
    </div>
  `
})
export class AlertComponent {
  @Input() variant: AlertVariant = 'info';
  @Input() title?: string;

  get variantClasses(): string {
    switch (this.variant) {
      case 'error':
        return 'bg-red-50 border-red-200 text-red-900';
      case 'warning':
        return 'bg-amber-50 border-amber-200 text-amber-900';
      case 'success':
        return 'bg-emerald-50 border-emerald-200 text-emerald-900';
      case 'info':
      default:
        return 'bg-primary-50 border-primary-200 text-primary-950';
    }
  }
}
