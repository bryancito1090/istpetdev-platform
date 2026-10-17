import { describe, expect, it } from 'vitest';
import {
  computeRouteTrackingSummary,
  computeStopProgressList,
  getDeliveryStatusBadgeClass,
  getHuStatusBadgeClass,
  isStopCompleted,
  translateDeliveryStatus,
  translateHuStatus,
  translateStopStatus
} from './route-tracking.model';

describe('Seguimiento de la ruta en el Planificador (#231)', () => {
  describe('Traductores de estado al español', () => {
    it('traduce los estados del bulto según la convención de la PWA (#162)', () => {
      expect(translateHuStatus('Draft')).toBe('En preparación');
      expect(translateHuStatus('Prepared')).toBe('En bodega');
      expect(translateHuStatus('Sealed')).toBe('Sellado en bodega');
      expect(translateHuStatus('Dispatched')).toBe('Listo para recoger');
      expect(translateHuStatus('InTransit')).toBe('En vehículo');
      expect(translateHuStatus('Delivered')).toBe('Entregado');
      expect(translateHuStatus('Received')).toBe('Recibido conforme');
      expect(translateHuStatus('Partial')).toBe('Recibido parcial');
      expect(translateHuStatus('Cancelled')).toBe('Cancelado');
      expect(translateHuStatus(null)).toBe('Sin estado');
    });

    it('traduce los estados de la entrega', () => {
      expect(translateDeliveryStatus('Proposed')).toBe('Planificada');
      expect(translateDeliveryStatus('WaitingWarehouse')).toBe('Esperando a Bodega');
      expect(translateDeliveryStatus('Dispatched')).toBe('Despachada');
      expect(translateDeliveryStatus('InTransit')).toBe('En ruta');
      expect(translateDeliveryStatus('Delivered')).toBe('Entregada');
      expect(translateDeliveryStatus('Received')).toBe('Recibida conforme');
      expect(translateDeliveryStatus('Partial')).toBe('Recibida parcial');
      expect(translateDeliveryStatus('Cancelled')).toBe('Cancelada');
    });

    it('traduce los estados de la parada', () => {
      expect(translateStopStatus('Completed')).toBe('Completada');
      expect(translateStopStatus('InTransit')).toBe('En curso');
      expect(translateStopStatus('Pending')).toBe('Pendiente');
      expect(translateStopStatus('Proposed')).toBe('Planificada');
    });

    it('asigna clases visuales adecuadas a cada estado de bulto y entrega', () => {
      expect(getHuStatusBadgeClass('Received')).toContain('emerald');
      expect(getHuStatusBadgeClass('InTransit')).toContain('blue');
      expect(getHuStatusBadgeClass('Partial')).toContain('amber');
      expect(getDeliveryStatusBadgeClass('Delivered')).toContain('emerald');
    });
  });

  describe('Cálculo del resumen de ruta (computeRouteTrackingSummary)', () => {
    it('calcula correctamente «3 de 6 paradas entregadas · 2 bultos en el vehículo» (#231)', () => {
      const mockStops = [
        {
          sequence: 1,
          kind: 'Pickup',
          servicePointName: 'Bodega Central',
          status: 'Completed',
          handlingUnits: [
            { id: 'hu-1', code: 'BUL-001', status: 'InTransit' },
            { id: 'hu-2', code: 'BUL-002', status: 'InTransit' },
            { id: 'hu-3', code: 'BUL-003', status: 'Delivered' },
            { id: 'hu-4', code: 'BUL-004', status: 'Delivered' },
            { id: 'hu-5', code: 'BUL-005', status: 'Delivered' }
          ]
        },
        { sequence: 2, kind: 'Delivery', servicePointName: 'Punto A', status: 'Completed', deliveryStatus: 'Delivered' },
        { sequence: 3, kind: 'Delivery', servicePointName: 'Punto B', status: 'Completed', deliveryStatus: 'Received' },
        { sequence: 4, kind: 'Delivery', servicePointName: 'Punto C', status: 'Completed', deliveryStatus: 'Partial' },
        { sequence: 5, kind: 'Delivery', servicePointName: 'Punto D', status: 'Pending', deliveryStatus: 'InTransit' },
        { sequence: 6, kind: 'Delivery', servicePointName: 'Punto E', status: 'Pending', deliveryStatus: 'Dispatched' },
        { sequence: 7, kind: 'Delivery', servicePointName: 'Punto F', status: 'Pending', deliveryStatus: 'Proposed' }
      ];

      const summary = computeRouteTrackingSummary(mockStops);

      expect(summary.totalDeliveryStops).toBe(6);
      expect(summary.deliveredStopsCount).toBe(3);
      expect(summary.bultosInVehicle).toBe(2);
      expect(summary.summaryText).toBe('3 de 6 paradas entregadas · 2 bultos en el vehículo');
      expect(summary.currentStopSequence).toBe(5); // Primer parada no completada
      expect(summary.isFinished).toBe(false);
    });

    it('maneja una ruta completada al 100%', () => {
      const mockStops = [
        { sequence: 1, kind: 'Delivery', servicePointName: 'Punto A', status: 'Completed', deliveryStatus: 'Received' },
        { sequence: 2, kind: 'Delivery', servicePointName: 'Punto B', status: 'Completed', deliveryStatus: 'Received' }
      ];

      const summary = computeRouteTrackingSummary(mockStops);
      expect(summary.deliveredStopsCount).toBe(2);
      expect(summary.totalDeliveryStops).toBe(2);
      expect(summary.isFinished).toBe(true);
      expect(summary.summaryText).toContain('Ruta completada: 2 de 2 paradas entregadas');
    });

    it('maneja ruta vacía sin romper', () => {
      const summary = computeRouteTrackingSummary([]);
      expect(summary.totalStops).toBe(0);
      expect(summary.deliveredStopsCount).toBe(0);
      expect(summary.summaryText).toBe('Sin paradas en la ruta');
    });
  });

  describe('Generación de línea de avance por parada (computeStopProgressList)', () => {
    it('marca la primera no entregada con isCurrent = true (indicador de parada actual)', () => {
      const mockStops = [
        {
          sequence: 1,
          kind: 'Pickup',
          servicePointName: 'Bodega El Inca',
          servicePointCode: 'WH-01',
          status: 'Completed'
        },
        {
          sequence: 2,
          kind: 'Delivery',
          servicePointName: 'Hospital Eugenio Espejo',
          servicePointCode: 'HEE-01',
          status: 'Completed',
          deliveryStatus: 'Received'
        },
        {
          sequence: 3,
          kind: 'Delivery',
          servicePointName: 'Centro de Salud N3',
          servicePointCode: 'CS-03',
          status: 'Pending',
          deliveryStatus: 'InTransit',
          handlingUnits: [{ id: 'h1', code: 'BUL-99', status: 'InTransit' }]
        },
        {
          sequence: 4,
          kind: 'Delivery',
          servicePointName: 'Farmacia Popular',
          servicePointCode: 'FP-01',
          status: 'Pending',
          deliveryStatus: 'Dispatched'
        }
      ];

      const progress = computeStopProgressList(mockStops);

      expect(progress.length).toBe(4);
      // Parada 1: Recogida completada
      expect(progress[0].sequence).toBe(1);
      expect(progress[0].isCompleted).toBe(true);
      expect(progress[0].isCurrent).toBe(false);

      // Parada 2: Entrega completada
      expect(progress[1].sequence).toBe(2);
      expect(progress[1].isCompleted).toBe(true);
      expect(progress[1].isCurrent).toBe(false);

      // Parada 3: Primera no entregada -> debe ser la parada actual
      expect(progress[2].sequence).toBe(3);
      expect(progress[2].isCurrent).toBe(true);
      expect(progress[2].isCompleted).toBe(false);
      expect(progress[2].statusLabel).toBe('En camino');
      expect(progress[2].handlingUnits[0].statusLabel).toBe('En vehículo');

      // Parada 4: Pendiente futura
      expect(progress[3].sequence).toBe(4);
      expect(progress[3].isCurrent).toBe(false);
      expect(progress[3].isCompleted).toBe(false);
      expect(progress[3].isPending).toBe(true);
    });
  });
});
