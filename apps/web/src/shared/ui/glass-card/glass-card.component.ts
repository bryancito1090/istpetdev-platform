import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

export type GlassGlow = 'blue' | 'cyan' | 'mint' | 'none';
export type GlassPadding = 'none' | 'sm' | 'md' | 'lg';

@Component({
  selector: 'ui-glass-card',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div
      class="relative rounded-2xl transition-all duration-300"
      [ngClass]="[
        'glass-credential-card overflow-hidden',
        hoverable ? 'hover:-translate-y-1 hover:shadow-xl hover:border-white' : '',
        paddingClass
      ]">

      <!-- Resplandor ambiental de acento interno -->
      @if (glow !== 'none') {
        <div
          aria-hidden="true"
          class="absolute -top-16 -right-16 w-44 h-44 rounded-full blur-2xl pointer-events-none opacity-40 transition-opacity"
          [ngClass]="glowClass">
        </div>
      }

      <div class="relative z-10">
        <ng-content></ng-content>
      </div>
    </div>
  `
})
export class GlassCardComponent {
  @Input() padding: GlassPadding = 'md';
  @Input() hoverable = false;
  @Input() glow: GlassGlow = 'none';

  get paddingClass(): string {
    switch (this.padding) {
      case 'none':
        return '';
      case 'sm':
        return 'p-4';
      case 'lg':
        return 'p-8 sm:p-10';
      case 'md':
      default:
        return 'p-6 sm:p-7';
    }
  }

  get glowClass(): string {
    switch (this.glow) {
      case 'blue':
        return 'bg-expo-blue/50';
      case 'cyan':
        return 'bg-expo-cyan/50';
      case 'mint':
        return 'bg-expo-mint/50';
      case 'none':
      default:
        return '';
    }
  }
}
