import {
  AfterViewInit,
  Component,
  ElementRef,
  HostListener,
  OnDestroy,
  ViewChild,
  signal
} from '@angular/core';
import { CommonModule } from '@angular/common';

interface RevealWord {
  readonly text: string;
  readonly highlight?: boolean;
}

@Component({
  selector: 'landing-tagline-reveal',
  standalone: true,
  imports: [CommonModule],
  template: `
    <section
      #revealSection
      aria-label="Declaración de impacto y custodia logística"
      class="w-full py-4 sm:py-6 bg-transparent border-none overflow-hidden">
      <div class="max-w-4xl mx-auto text-center space-y-4">
        
        <span class="inline-flex items-center gap-2 px-3.5 py-1 rounded-full bg-white/10 border border-white/20 text-xs font-zen font-medium uppercase tracking-widest text-[#00C2FF]">
          <span class="h-1.5 w-1.5 rounded-full bg-[#00E5A3] animate-pulse"></span>
          <span>Continuidad Absoluta · 80 Hospitales</span>
        </span>

        <!-- Contenedor de revelado palabra por palabra con tipografía Hina Mincho -->
        <h2 class="font-display text-3xl sm:text-4xl lg:text-5xl tracking-normal leading-[1.25] max-w-3xl mx-auto [text-wrap:balance]">
          @for (word of words; track $index; let idx = $index) {
            <span
              class="inline-block mx-1 my-0.5 transition-all duration-700 ease-[cubic-bezier(0.32,0.72,0,1)] select-none will-change-transform"
              [class.opacity-25]="activeWordCount() <= idx"
              [class.blur-[0.5px]]="activeWordCount() <= idx"
              [class.scale-[0.98]]="activeWordCount() <= idx"
              [class.opacity-100]="activeWordCount() > idx"
              [class.blur-0]="activeWordCount() > idx"
              [class.scale-100]="activeWordCount() > idx"
              [class.text-[#F3EADB]]="activeWordCount() > idx && !word.highlight"
              [class.text-transparent]="activeWordCount() > idx && word.highlight"
              [class.bg-clip-text]="activeWordCount() > idx && word.highlight"
              [class.bg-gradient-to-r]="activeWordCount() > idx && word.highlight"
              [class.from-[#00C2FF]]="activeWordCount() > idx && word.highlight"
              [class.to-[#00E5A3]]="activeWordCount() > idx && word.highlight"
              [class.text-[#F3EADB]/30]="activeWordCount() <= idx">
              {{ word.text }}
            </span>
          }
        </h2>

        <p class="font-zen text-sm sm:text-base text-[#F3EADB]/80 max-w-xl mx-auto [text-wrap:pretty] leading-relaxed">
          Cada despacho parte con identidad criptográfica única y cada recepción queda asentada con firma táctil digital incluso en subsuelos sin cobertura de telefonía.
        </p>

      </div>
    </section>
  `
})
export class TaglineRevealComponent implements AfterViewInit, OnDestroy {
  @ViewChild('revealSection') revealSectionRef!: ElementRef<HTMLElement>;

  readonly words: readonly RevealWord[] = [
    { text: 'Anticipamos' },
    { text: 'la' },
    { text: 'rotura' },
    { text: 'de' },
    { text: 'stock' },
    { text: 'hospitalario' },
    { text: '5', highlight: true },
    { text: 'días', highlight: true },
    { text: 'antes', highlight: true },
    { text: 'y' },
    { text: 'custodiamos' },
    { text: 'cada' },
    { text: 'envase' },
    { text: 'con' },
    { text: 'identidad' },
    { text: 'criptográfica' },
    { text: 'inmutable', highlight: true },
    { text: 'en' },
    { text: 'todo' },
    { text: 'el' },
    { text: 'país.' }
  ];

  readonly activeWordCount = signal<number>(0);
  private rafId: number | null = null;

  ngAfterViewInit(): void {
    if (typeof window === 'undefined') return;
    this.updateProgress();
  }

  ngOnDestroy(): void {
    if (this.rafId !== null && typeof cancelAnimationFrame === 'function') {
      cancelAnimationFrame(this.rafId);
      this.rafId = null;
    }
  }

  @HostListener('window:scroll')
  onScroll(): void {
    if (this.rafId !== null) return;
    this.rafId = requestAnimationFrame(() => {
      this.updateProgress();
      this.rafId = null;
    });
  }

  @HostListener('window:resize')
  onResize(): void {
    this.updateProgress();
  }

  private updateProgress(): void {
    const el = this.revealSectionRef?.nativeElement;
    if (!el) return;

    const rect = el.getBoundingClientRect();
    const windowHeight = window.innerHeight;

    const startY = windowHeight * 0.85;
    const endY = windowHeight * 0.25;
    const currentY = rect.top;

    if (currentY > startY) {
      this.activeWordCount.set(0);
      return;
    }

    if (currentY < endY) {
      this.activeWordCount.set(this.words.length);
      return;
    }

    const progress = Math.max(0, Math.min(1, (startY - currentY) / (startY - endY)));
    const targetWords = Math.floor(progress * (this.words.length + 1));
    this.activeWordCount.set(targetWords);
  }
}
