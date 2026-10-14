import { Component } from '@angular/core';

@Component({
  selector: 'widget-footer',
  standalone: true,
  template: `
    <footer class="bg-white border-t border-slate-200 mt-auto">
      <div class="max-w-6xl mx-auto px-4 sm:px-6 py-6 text-xs text-slate-400">
        © 2026 IstpetDev — Abastecimiento y trazabilidad de insumos hospitalarios.
      </div>
    </footer>
  `
})
export class FooterWidget {}
