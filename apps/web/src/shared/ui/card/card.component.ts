import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'ui-card',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div
      class="bg-white rounded-xl border border-slate-200 shadow-sm overflow-hidden transition-all"
      [ngClass]="[
        hoverable ? 'hover:border-slate-300 hover:shadow-md' : '',
        paddingClass
      ]">
      
      @if (title || subtitle) {
        <div class="border-b border-slate-100 pb-4 mb-4">
          @if (title) {
            <h3 class="text-base font-bold text-slate-900 tracking-tight flex items-center justify-between">
              <span>{{ title }}</span>
              <ng-content select="[card-header-action]"></ng-content>
            </h3>
          }
          @if (subtitle) {
            <p class="text-xs text-slate-500 mt-1">{{ subtitle }}</p>
          }
        </div>
      }

      <ng-content></ng-content>

      <ng-content select="[card-footer]"></ng-content>
    </div>
  `
})
export class CardComponent {
  @Input() title?: string;
  @Input() subtitle?: string;
  @Input() hoverable = false;
  @Input() padding: 'none' | 'sm' | 'md' | 'lg' = 'md';

  get paddingClass(): string {
    switch (this.padding) {
      case 'none':
        return '';
      case 'sm':
        return 'p-4';
      case 'lg':
        return 'p-8';
      case 'md':
      default:
        return 'p-6';
    }
  }
}
