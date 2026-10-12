import { Component, Input, forwardRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ControlValueAccessor, NG_VALUE_ACCESSOR, FormsModule } from '@angular/forms';

@Component({
  selector: 'ui-input',
  standalone: true,
  imports: [CommonModule, FormsModule],
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => InputComponent),
      multi: true
    }
  ],
  template: `
    <div class="w-full flex flex-col gap-1.5">
      @if (label) {
        <label [for]="inputId" class="text-xs font-semibold text-slate-700 tracking-tight flex items-center justify-between">
          <span>{{ label }}</span>
          @if (required) {
            <span class="text-red-500 font-normal text-[11px]">* Requerido</span>
          }
        </label>
      }

      <div class="relative flex items-center">
        <input
          [id]="inputId"
          [type]="type"
          [placeholder]="placeholder"
          [disabled]="disabled"
          [value]="value"
          [attr.aria-invalid]="!!errorMessage"
          [attr.aria-describedby]="errorMessage ? inputId + '-error' : hint ? inputId + '-hint' : null"
          (input)="handleInput($event)"
          (blur)="handleBlur()"
          class="w-full px-3.5 py-2 text-sm bg-white border rounded-lg transition-all focus:outline-none focus:ring-2 focus:ring-offset-0 placeholder:text-slate-400"
          [ngClass]="[
            errorMessage 
              ? 'border-red-400 focus:border-red-500 focus:ring-red-200 text-red-900' 
              : 'border-slate-300 focus:border-primary-600 focus:ring-primary-100 text-slate-900',
            disabled ? 'bg-slate-100 text-slate-400 cursor-not-allowed' : ''
          ]" />
      </div>

      @if (errorMessage) {
        <p [id]="inputId + '-error'" class="text-xs text-red-600 font-medium flex items-center gap-1">
          <svg class="w-3.5 h-3.5 shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" />
          </svg>
          <span>{{ errorMessage }}</span>
        </p>
      } @else if (hint) {
        <p [id]="inputId + '-hint'" class="text-[11px] text-slate-500">
          {{ hint }}
        </p>
      }
    </div>
  `
})
export class InputComponent implements ControlValueAccessor {
  @Input() label?: string;
  @Input() type = 'text';
  @Input() placeholder = '';
  @Input() inputId = 'input-' + Math.random().toString(36).substring(2, 9);
  @Input() required = false;
  @Input() disabled = false;
  @Input() hint?: string;
  @Input() errorMessage?: string;

  value = '';

  onChange: (val: string) => void = () => {};
  onTouched: () => void = () => {};

  writeValue(val: string): void {
    this.value = val || '';
  }

  registerOnChange(fn: (val: string) => void): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  setDisabledState?(isDisabled: boolean): void {
    this.disabled = isDisabled;
  }

  handleInput(event: Event): void {
    const target = event.target as HTMLInputElement;
    this.value = target.value;
    this.onChange(this.value);
  }

  handleBlur(): void {
    this.onTouched();
  }
}
