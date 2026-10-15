import {
  Component,
  ElementRef,
  EventEmitter,
  OnDestroy,
  OnInit,
  Output,
  ViewChild,
  signal
} from '@angular/core';
import { CommonModule } from '@angular/common';
import jsQR from 'jsqr';

export type ScannerStatus =
  | 'idle'
  | 'requesting'
  | 'scanning'
  | 'permission-denied'
  | 'unsupported'
  | 'error';

@Component({
  selector: 'app-qr-scanner',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="relative w-full rounded-2xl overflow-hidden bg-slate-950 text-white shadow-xl min-h-[260px] flex flex-col items-center justify-center p-4">
      <!-- Elemento de video para captura de cámara -->
      <video
        #videoElement
        playsinline
        muted
        autoplay
        class="absolute inset-0 w-full h-full object-cover"
        [class.hidden]="status() !== 'scanning'">
      </video>

      <!-- Canvas off-screen para procesamiento de fotogramas con jsQR -->
      <canvas #canvasElement class="hidden"></canvas>

      <!-- Overlay visual de visor táctico cuando está escaneando -->
      @if (status() === 'scanning') {
        <div class="relative z-10 flex flex-col items-center justify-center w-full h-full pointer-events-none py-6">
          <div class="relative w-56 h-56 border-2 border-emerald-400/80 rounded-2xl flex items-center justify-center shadow-[0_0_25px_rgba(52,211,153,0.3)]">
            <!-- Esquinas reforzadas -->
            <div class="absolute -top-1 -left-1 w-6 h-6 border-t-4 border-l-4 border-emerald-400 rounded-tl-lg"></div>
            <div class="absolute -top-1 -right-1 w-6 h-6 border-t-4 border-r-4 border-emerald-400 rounded-tr-lg"></div>
            <div class="absolute -bottom-1 -left-1 w-6 h-6 border-b-4 border-l-4 border-emerald-400 rounded-bl-lg"></div>
            <div class="absolute -bottom-1 -right-1 w-6 h-6 border-b-4 border-r-4 border-emerald-400 rounded-br-lg"></div>

            <!-- Línea de barrido óptico animada -->
            <div class="w-full h-0.5 bg-gradient-to-r from-transparent via-emerald-400 to-transparent animate-pulse"></div>
          </div>
          <p class="mt-4 text-xs font-medium text-slate-200 tracking-wide bg-slate-900/80 px-3 py-1 rounded-full backdrop-blur-sm">
            Enfoque el código QR del bulto
          </p>
        </div>
      }

      <!-- Estado: Solicitando acceso a cámara -->
      @if (status() === 'requesting') {
        <div class="relative z-10 flex flex-col items-center justify-center text-center p-6 space-y-3">
          <div class="w-12 h-12 border-3 border-emerald-400 border-t-transparent rounded-full animate-spin"></div>
          <p class="text-sm font-semibold text-slate-200">Activando cámara del dispositivo...</p>
          <p class="text-xs text-slate-400">Por favor autorice el acceso cuando el navegador lo solicite.</p>
        </div>
      }

      <!-- Estado: Permiso denegado -->
      @if (status() === 'permission-denied') {
        <div class="relative z-10 flex flex-col items-center justify-center text-center p-6 space-y-3 max-w-sm">
          <div class="w-12 h-12 rounded-full bg-amber-500/20 text-amber-400 flex items-center justify-center">
            <svg class="w-6 h-6" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
              <path stroke-linecap="round" stroke-linejoin="round" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" />
            </svg>
          </div>
          <p class="text-sm font-bold text-amber-300">Activa el permiso de cámara en el navegador</p>
          <p class="text-xs text-slate-300 leading-relaxed">
            El acceso a la cámara fue bloqueado o no otorgado. Puede activar el permiso en la configuración del navegador o usar el campo manual inferior.
          </p>
          <button
            type="button"
            (click)="startCamera()"
            class="mt-2 px-4 py-2 bg-emerald-600 hover:bg-emerald-500 text-white text-xs font-semibold rounded-lg transition shadow">
            Reintentar Acceso
          </button>
        </div>
      }

      <!-- Estado: No soportado / Contexto no seguro -->
      @if (status() === 'unsupported' || status() === 'error') {
        <div class="relative z-10 flex flex-col items-center justify-center text-center p-6 space-y-3 max-w-sm">
          <div class="w-12 h-12 rounded-full bg-red-500/20 text-red-400 flex items-center justify-center">
            <svg class="w-6 h-6" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
              <path stroke-linecap="round" stroke-linejoin="round" d="M6 18L18 6M6 6l12 12" />
            </svg>
          </div>
          <p class="text-sm font-bold text-red-300">Cámara no disponible</p>
          <p class="text-xs text-slate-300">
            {{ errorMessage() || 'No fue posible iniciar el sensor de cámara. Use el campo manual de abajo para continuar.' }}
          </p>
        </div>
      }
    </div>
  `
})
export class QrScannerComponent implements OnInit, OnDestroy {
  @ViewChild('videoElement') videoRef?: ElementRef<HTMLVideoElement>;
  @ViewChild('canvasElement') canvasRef?: ElementRef<HTMLCanvasElement>;

  @Output() scanned = new EventEmitter<string>();

  readonly status = signal<ScannerStatus>('idle');
  readonly errorMessage = signal<string | null>(null);

  private mediaStream: MediaStream | null = null;
  private animationFrameId: number | null = null;
  private barcodeDetector: any = null;
  private isProcessing = false;

  async ngOnInit(): Promise<void> {
    this.initBarcodeDetector();
    await this.startCamera();
  }

  ngOnDestroy(): void {
    this.stopCamera();
  }

  private initBarcodeDetector(): void {
    if (typeof window !== 'undefined' && 'BarcodeDetector' in window) {
      try {
        const BarcodeDetectorClass = (window as any).BarcodeDetector;
        this.barcodeDetector = new BarcodeDetectorClass({ formats: ['qr_code'] });
      } catch {
        this.barcodeDetector = null;
      }
    }
  }

  async startCamera(): Promise<void> {
    this.stopCamera();
    this.status.set('requesting');
    this.errorMessage.set(null);

    // Validación de contexto seguro (HTTPS o localhost)
    if (typeof window !== 'undefined' && window.isSecureContext === false) {
      this.status.set('unsupported');
      this.errorMessage.set('La cámara requiere una conexión segura (HTTPS o localhost).');
      return;
    }

    if (
      typeof navigator === 'undefined' ||
      !navigator.mediaDevices ||
      !navigator.mediaDevices.getUserMedia
    ) {
      this.status.set('unsupported');
      this.errorMessage.set('Tu navegador no soporta captura de video (getUserMedia).');
      return;
    }

    try {
      this.mediaStream = await navigator.mediaDevices.getUserMedia({
        video: {
          facingMode: { ideal: 'environment' },
          width: { ideal: 1280 },
          height: { ideal: 720 }
        },
        audio: false
      });

      const video = this.videoRef?.nativeElement;
      if (video) {
        video.srcObject = this.mediaStream;
        video.setAttribute('playsinline', 'true');
        video.muted = true;
        await video.play();
        this.status.set('scanning');
        this.scanLoop();
      } else {
        // En caso de que ViewChild aún no esté montado en el DOM
        setTimeout(() => this.bindVideoElement(), 150);
      }
    } catch (err: any) {
      const errorName = err?.name || '';
      if (
        errorName === 'NotAllowedError' ||
        errorName === 'PermissionDeniedError' ||
        errorName === 'SecurityError'
      ) {
        this.status.set('permission-denied');
      } else {
        this.status.set('error');
        this.errorMessage.set(err?.message || 'Error al conectar con la cámara.');
      }
    }
  }

  private async bindVideoElement(): Promise<void> {
    const video = this.videoRef?.nativeElement;
    if (video && this.mediaStream) {
      try {
        video.srcObject = this.mediaStream;
        video.setAttribute('playsinline', 'true');
        video.muted = true;
        await video.play();
        this.status.set('scanning');
        this.scanLoop();
      } catch (err: any) {
        this.status.set('error');
        this.errorMessage.set(err?.message || 'Error al iniciar reproducción de video.');
      }
    }
  }

  stopCamera(): void {
    if (this.animationFrameId !== null) {
      cancelAnimationFrame(this.animationFrameId);
      this.animationFrameId = null;
    }

    if (this.mediaStream) {
      this.mediaStream.getTracks().forEach((track) => track.stop());
      this.mediaStream = null;
    }

    this.isProcessing = false;
    if (this.status() === 'scanning') {
      this.status.set('idle');
    }
  }

  private scanLoop(): void {
    if (this.status() !== 'scanning') return;

    this.animationFrameId = requestAnimationFrame(async () => {
      await this.processCurrentFrame();
      if (this.status() === 'scanning') {
        this.scanLoop();
      }
    });
  }

  private async processCurrentFrame(): Promise<void> {
    if (this.isProcessing) return;
    const video = this.videoRef?.nativeElement;
    if (!video || video.readyState < video.HAVE_CURRENT_DATA) return;

    this.isProcessing = true;

    try {
      // 1. Prioridad: API nativa BarcodeDetector (Chrome Android / Edge)
      if (this.barcodeDetector) {
        try {
          const barcodes = await this.barcodeDetector.detect(video);
          if (barcodes && barcodes.length > 0 && barcodes[0]?.rawValue) {
            this.emitSuccessfulScan(barcodes[0].rawValue.trim());
            return;
          }
        } catch {
          // Si falla BarcodeDetector en este frame, degradar a jsQR
        }
      }

      // 2. Fallback: Procesamiento por fotogramas con jsQR (iOS Safari / navegadores estándar)
      const canvas = this.canvasRef?.nativeElement;
      if (canvas && video.videoWidth > 0 && video.videoHeight > 0) {
        canvas.width = video.videoWidth;
        canvas.height = video.videoHeight;
        const ctx = canvas.getContext('2d', { willReadFrequently: true });
        if (ctx) {
          ctx.drawImage(video, 0, 0, canvas.width, canvas.height);
          const imageData = ctx.getImageData(0, 0, canvas.width, canvas.height);
          const code = jsQR(imageData.data, imageData.width, imageData.height, {
            inversionAttempts: 'dontInvert'
          });

          if (code && code.data && code.data.trim()) {
            this.emitSuccessfulScan(code.data.trim());
            return;
          }
        }
      }
    } finally {
      this.isProcessing = false;
    }
  }

  private emitSuccessfulScan(rawText: string): void {
    this.playAudioBeep();
    this.stopCamera();
    this.scanned.emit(rawText);
  }

  private playAudioBeep(): void {
    try {
      if (typeof window === 'undefined') return;
      const AudioCtx = window.AudioContext || (window as any).webkitAudioContext;
      if (!AudioCtx) return;
      const ctx = new AudioCtx();
      const osc = ctx.createOscillator();
      const gain = ctx.createGain();
      osc.connect(gain);
      gain.connect(ctx.destination);
      osc.type = 'sine';
      osc.frequency.setValueAtTime(880, ctx.currentTime);
      gain.gain.setValueAtTime(0.12, ctx.currentTime);
      gain.gain.exponentialRampToValueAtTime(0.01, ctx.currentTime + 0.1);
      osc.start(ctx.currentTime);
      osc.stop(ctx.currentTime + 0.1);
    } catch {
      // Ignorar bloqueos de autoplay de audio
    }
  }
}
