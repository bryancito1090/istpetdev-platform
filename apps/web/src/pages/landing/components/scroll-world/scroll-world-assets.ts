/**
 * Assets oficiales para el módulo ScrollWorld de la plataforma ISTPET Logística.
 * Rutas de escenas fotográficas cinemáticas 16:9 y clips de vídeo generados con IA.
 */
export interface ScrollWorldWalkthroughAssets {
  readonly scene1Warehouse: string;
  readonly scene2Telemetry: string;
  readonly scene3Silos: string;
  readonly scene4Hospital: string;
  readonly clip1Video?: string;
}

export const SCROLL_WORLD_ASSETS = {
  walkthrough: {
    scene1Warehouse: '/assets/scroll-world/walkthrough-1-warehouse.jpg',
    scene2Telemetry: '/assets/scroll-world/walkthrough-2-telemetry.jpg',
    scene3Silos: '/assets/scroll-world/walkthrough-3-silos.jpg',
    scene4Hospital: '/assets/scroll-world/walkthrough-4-hospital.jpg',
    clip1Video: '/assets/scroll-world/clips/clip_1.mp4'
  }
} as const;
