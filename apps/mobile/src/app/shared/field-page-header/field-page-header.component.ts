import { Component, input, output } from '@angular/core';

@Component({
  selector: 'app-field-page-header',
  standalone: true,
  template: `
    <header class="field-header">
      <div class="brand-bar">
        <div class="brand-lockup" aria-label="Istet Dev, operaciones de campo">
          <span class="brand-name">Istet Dev</span>
          <span class="brand-caption">OPERACIONES DE CAMPO</span>
        </div>
        @if (showHelp()) {
          <button type="button" class="icon-button help-button" aria-label="Ayuda" (click)="help.emit()">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" aria-hidden="true">
              <circle cx="12" cy="12" r="9" />
              <path stroke-linecap="round" stroke-linejoin="round" d="M9.6 9a2.45 2.45 0 1 1 4.1 1.82c-.95.83-1.7 1.3-1.7 2.68M12 16.7h.01" />
            </svg>
          </button>
        }
      </div>

      @if (!brandOnly()) {
      <div class="navigation-bar">
        @if (home()) {
          <button type="button" class="icon-button" aria-label="Menú principal" (click)="menu.emit()">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.3" aria-hidden="true">
              <path stroke-linecap="round" stroke-linejoin="round" d="M3.75 6.75h16.5M3.75 12h16.5M3.75 17.25h16.5" />
            </svg>
          </button>
          <h1 class="page-title">{{ userName() || 'Operaciones de campo' }}</h1>
          <div class="actions">
            <button type="button" class="icon-button badge-anchor" aria-label="Notificaciones" (click)="sync.emit()">
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" aria-hidden="true">
                <path stroke-linecap="round" stroke-linejoin="round" d="M14.857 17.082a23.848 23.848 0 005.454-1.31A8.967 8.967 0 0118 9.75v-.7V9A6 6 0 006 9v.75a8.967 8.967 0 01-2.312 6.022c1.733.64 3.56 1.085 5.455 1.31m5.714 0a24.255 24.255 0 01-5.714 0m5.714 0a3 3 0 11-5.714 0" />
              </svg>
              @if (pendingCount() > 0) { <span class="badge">{{ pendingCount() }}</span> }
            </button>
            <button type="button" class="icon-button" aria-label="Buscar" (click)="search.emit()">
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2" aria-hidden="true">
                <path stroke-linecap="round" stroke-linejoin="round" d="M21 21l-5.197-5.197m0 0A7.5 7.5 0 105.196 5.196a7.5 7.5 0 0010.607 10.607z" />
              </svg>
            </button>
          </div>
        } @else {
          <button type="button" class="icon-button" aria-label="Volver a Inicio" (click)="back.emit()">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.3" aria-hidden="true">
              <path stroke-linecap="round" stroke-linejoin="round" d="M10.5 19.5L3 12m0 0l7.5-7.5M3 12h18" />
            </svg>
          </button>
          <h1 class="page-title">{{ title() }}</h1>
          <div class="actions">
            @if (syncAction()) {
              <button type="button" class="icon-button badge-anchor" aria-label="Cola de sincronización" (click)="sync.emit()">
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" aria-hidden="true">
                <path stroke-linecap="round" stroke-linejoin="round" d="M14.857 17.082a23.848 23.848 0 005.454-1.31A8.967 8.967 0 0118 9.75v-.7V9A6 6 0 006 9v.75a8.967 8.967 0 01-2.312 6.022c1.733.64 3.56 1.085 5.455 1.31m5.714 0a24.255 24.255 0 01-5.714 0m5.714 0a3 3 0 11-5.714 0" />
              </svg>
              @if (pendingCount() > 0) { <span class="badge">{{ pendingCount() }}</span> }
              </button>
            } @else if (pendingCount() > 0) {
              <span class="static-badge" aria-label="Operaciones pendientes">{{ pendingCount() }}</span>
            }
          </div>
        }
      </div>
      }
    </header>
  `,
  styles: [`
    :host { display: block; flex: 0 0 auto; background: #04303B; font-family: 'Inter', system-ui, sans-serif; }
    .field-header { overflow: hidden; background: #04303B; }
    .brand-bar { min-height: 56px; padding: max(8px, env(safe-area-inset-top)) 20px 8px; display: flex; align-items: center; justify-content: space-between; color: #fff; }
    .brand-lockup { display: flex; flex-direction: column; gap: 2px; }
    .brand-name { font-size: 15px; line-height: 1.2; font-weight: 800; letter-spacing: -.03em; }
    .brand-caption { color: #a5d6df; font-size: 8px; font-weight: 700; letter-spacing: .12em; }
    .navigation-bar { min-height: 66px; padding: 8px 12px; display: flex; align-items: center; gap: 8px; background: #fff; border-radius: 20px 20px 0 0; }
    .icon-button { flex: 0 0 44px; width: 44px; height: 44px; position: relative; display: grid; place-items: center; padding: 10px; border: 0; border-radius: 12px; background: transparent; color: #0a2540; cursor: pointer; }
    .icon-button:hover { background: #f1f5f9; }
    .icon-button svg { width: 23px; height: 23px; }
    .help-button { color: #fff; }
    .help-button:hover { background: #ffffff18; }
    .help-button svg { width: 21px; height: 21px; }
    .page-title { min-width: 0; flex: 1; margin: 0; overflow: hidden; color: #0a2540; font-size: 16px; line-height: 1.25; font-weight: 750; letter-spacing: -.02em; text-overflow: ellipsis; white-space: nowrap; }
    .actions { display: flex; align-items: center; gap: 2px; }
    .badge { position: absolute; top: 1px; right: 1px; min-width: 18px; height: 18px; padding: 0 5px; display: grid; place-items: center; border: 2px solid #fff; border-radius: 999px; background: #e11d48; color: #fff; font-size: 9px; font-weight: 800; }
    .static-badge { min-width: 24px; padding: 4px 7px; border-radius: 999px; background: #fff1f2; color: #be123c; font-size: 11px; font-weight: 800; text-align: center; }
    @media (max-width: 360px) { .brand-bar { padding-inline: 16px; } .navigation-bar { padding-inline: 8px; gap: 4px; } .page-title { font-size: 14px; } .icon-button { flex-basis: 40px; width: 40px; height: 40px; } }
    @media (prefers-reduced-motion: reduce) { *, *::before, *::after { scroll-behavior: auto !important; transition-duration: .01ms !important; } }
  `]
})
export class FieldPageHeaderComponent {
  readonly title = input('');
  readonly brandOnly = input(false);
  readonly showHelp = input(true);
  readonly home = input(false);
  readonly userName = input('');
  readonly pendingCount = input(0);
  readonly syncAction = input(true);
  readonly back = output<void>();
  readonly menu = output<void>();
  readonly sync = output<void>();
  readonly search = output<void>();
  readonly help = output<void>();
}
