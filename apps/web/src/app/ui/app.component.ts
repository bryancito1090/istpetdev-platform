import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { HeaderWidget } from '@widgets/header/header.widget';
import { FooterWidget } from '@widgets/footer/footer.widget';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, HeaderWidget, FooterWidget],
  template: `
    <div class="min-h-screen flex flex-col bg-white text-[#0a2540] overflow-x-hidden">
      <widget-header></widget-header>
      <main class="flex-1 w-full flex flex-col">
        <router-outlet></router-outlet>
      </main>
      <widget-footer></widget-footer>
    </div>
  `
})
export class AppComponent {}
