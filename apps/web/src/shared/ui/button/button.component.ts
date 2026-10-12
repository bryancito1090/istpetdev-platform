import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { LucideLoader } from '@lucide/angular';

export type ButtonVariant = 'primary' | 'secondary' | 'destructive' | 'simulation' | 'outline' | 'ghost' | 'accent';
export type ButtonSize = 'sm' | 'md' | 'lg';

@Component({
  selector: 'ui-button',
  standalone: true,
  imports: [CommonModule, LucideLoader],
  template: `
    <button
      [type]="type"
      [disabled]="disabled || loading"
      [attr.aria-busy]="loading"
      [attr.aria-disabled]="disabled || loading"
      [attr.aria-label]="ariaLabel || null"
      (click)="handleClick($event)"
      class="inline-flex items-center justify-center font-medium rounded-lg transition-all focus:outline-none focus:ring-2 focus:ring-offset-2 select-none"
      [ngClass]="[
        variantClasses,
        sizeClasses,
        fullWidth ? 'w-full' : '',
        (disabled || loading) ? 'opacity-60 cursor-not-allowed pointer-events-none' : 'cursor-pointer active:scale-[0.98]'
      ]">
      
      <!-- Indicador de carga animado (simulando rotación sin depender de texto) -->
      @if (loading) {
        <svg
          lucideLoader
          [size]="iconSize"
          class="animate-spin mr-2 shrink-0 stroke-[2.5]"
          aria-hidden="true">
        </svg>
      }

      <!-- Contenedor del contenido proyectado (texto e iconos) -->
      <span class="inline-flex items-center gap-1.5 truncate">
        <ng-content></ng-content>
      </span>
    </button>
  `,
  styles: [`
    :host {
      display: inline-block;
    }
    :host(.w-full) {
      display: block;
      width: 100%;
    }
  `]
})
export class ButtonComponent {
  @Input() variant: ButtonVariant = 'primary';
  @Input() size: ButtonSize = 'md';
  @Input() type: 'button' | 'submit' | 'reset' = 'button';
  @Input() disabled = false;
  @Input() loading = false;
  @Input() fullWidth = false;
  @Input() ariaLabel?: string;

  @Output() buttonClick = new EventEmitter<MouseEvent>();

  handleClick(event: MouseEvent): void {
    if (!this.disabled && !this.loading) {
      this.buttonClick.emit(event);
    }
  }

  get iconSize(): number {
    switch (this.size) {
      case 'sm':
        return 14;
      case 'lg':
        return 18;
      case 'md':
      default:
        return 16;
    }
  }

  get variantClasses(): string {
    switch (this.variant) {
      case 'primary':
        return 'bg-primary-600 hover:bg-primary-700 text-white shadow-sm focus:ring-primary-500 border border-transparent';
      case 'accent':
        return 'bg-accent-500 hover:bg-accent-600 text-white shadow-md focus:ring-accent-500 border border-transparent';
      case 'secondary':
        return 'bg-white hover:bg-slate-50 text-slate-700 border border-slate-300 shadow-sm focus:ring-slate-400';
      case 'destructive':
        return 'bg-red-600 hover:bg-red-700 text-white shadow-sm focus:ring-red-500 border border-transparent';
      case 'simulation':
        return 'bg-slate-800 hover:bg-slate-900 text-white border border-slate-700 shadow-sm focus:ring-slate-500';
      case 'outline':
        return 'bg-transparent hover:bg-slate-50 text-slate-700 border border-slate-300 focus:ring-slate-400';
      case 'ghost':
        return 'bg-transparent hover:bg-slate-100 text-slate-600 hover:text-slate-900 focus:ring-slate-400 border border-transparent';
      default:
        return 'bg-primary-600 hover:bg-primary-700 text-white shadow-sm focus:ring-primary-500';
    }
  }

  get sizeClasses(): string {
    switch (this.size) {
      case 'sm':
        return 'text-xs px-3 py-1.5 min-h-[32px]';
      case 'lg':
        return 'text-base px-5 py-2.5 min-h-[44px]';
      case 'md':
      default:
        return 'text-sm px-4 py-2 min-h-[38px]';
    }
  }
}
