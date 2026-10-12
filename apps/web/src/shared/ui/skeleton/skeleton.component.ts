import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

export type SkeletonVariant = 'rectangular' | 'text' | 'circular' | 'card' | 'table-row';

@Component({
  selector: 'ui-skeleton',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div
      [attr.role]="'status'"
      [attr.aria-label]="srText"
      class="w-full">
      
      @switch (variant) {
        @case ('circular') {
          <div
            class="animate-pulse bg-slate-200 rounded-full shrink-0"
            [ngStyle]="{
              width: width || '40px',
              height: height || width || '40px'
            }"
            aria-hidden="true">
          </div>
        }

        @case ('text') {
          <div class="space-y-2 w-full" aria-hidden="true">
            @for (line of textLines; track $index) {
              <div
                class="animate-pulse bg-slate-200 rounded"
                [ngStyle]="{
                  width: $last && textLines.length > 1 ? '70%' : (width || '100%'),
                  height: height || '0.875rem'
                }">
              </div>
            }
          </div>
        }

        @case ('card') {
          <div
            class="animate-pulse bg-white border border-slate-200 rounded-xl p-6 space-y-4 shadow-sm w-full"
            aria-hidden="true">
            <div class="flex items-center justify-between">
              <div class="h-3.5 bg-slate-200 rounded w-1/3"></div>
              <div class="h-4 bg-slate-200 rounded-md w-14"></div>
            </div>
            <div class="h-8 bg-slate-200 rounded w-1/2"></div>
            <div class="h-3 bg-slate-200 rounded w-2/3"></div>
          </div>
        }

        @case ('table-row') {
          <div
            class="animate-pulse flex items-center justify-between p-4 border-b border-slate-100 gap-4 w-full"
            aria-hidden="true">
            <div class="h-4 bg-slate-200 rounded w-1/4"></div>
            <div class="h-4 bg-slate-200 rounded w-1/6"></div>
            <div class="h-4 bg-slate-200 rounded w-1/5"></div>
            <div class="h-4 bg-slate-200 rounded w-1/8"></div>
          </div>
        }

        @default {
          <!-- Rectangular estándar -->
          <div
            class="animate-pulse bg-slate-200 rounded-md w-full"
            [ngStyle]="{
              width: width || '100%',
              height: height || '2.5rem'
            }"
            aria-hidden="true">
          </div>
        }
      }

      <span class="sr-only">{{ srText }}</span>
    </div>
  `,
  styles: [`
    :host {
      display: block;
      width: 100%;
    }
  `]
})
export class SkeletonComponent {
  @Input() variant: SkeletonVariant = 'rectangular';
  @Input() width?: string;
  @Input() height?: string;
  @Input() lines = 1;
  @Input() srText = 'Cargando contenido...';

  get textLines(): number[] {
    const count = Math.max(1, this.lines);
    return Array.from({ length: count }, (_, i) => i);
  }
}
