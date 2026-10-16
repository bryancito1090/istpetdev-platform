import { describe, expect, it } from 'vitest';
import { effectiveDeliveryStatus } from './custody-state';

describe('effectiveDeliveryStatus (#162)', () => {
  it('no retrocede: un Deliver confirmado viejo no sobrescribe un snapshot Received', () => {
    const status = effectiveDeliveryStatus({
      deliveryStatus: 'Received',
      handlingUnits: [{ status: 'Received' }],
      acceptedCustodyStatus: 'Delivered',
    });

    expect(status).toBe('Received');
  });

  it('avanza con el último comando confirmado si el snapshot está atrasado', () => {
    const status = effectiveDeliveryStatus({
      deliveryStatus: 'InTransit',
      handlingUnits: [{ status: 'InTransit' }],
      acceptedCustodyStatus: 'Delivered',
    });

    expect(status).toBe('Delivered');
  });

  it('legado: Delivered con todos los bultos Received se interpreta como Received', () => {
    const status = effectiveDeliveryStatus({
      deliveryStatus: 'Delivered',
      handlingUnits: [{ status: 'Received' }, { status: 'Received' }],
    });

    expect(status).toBe('Received');
  });

  it('legado: Delivered con un bulto Partial se interpreta como Partial', () => {
    const status = effectiveDeliveryStatus({
      deliveryStatus: 'Delivered',
      handlingUnits: [{ status: 'Received' }, { status: 'Partial' }],
    });

    expect(status).toBe('Partial');
  });

  it('sin bultos ni comandos conserva el estado del snapshot', () => {
    const status = effectiveDeliveryStatus({ deliveryStatus: 'Dispatched', handlingUnits: [] });

    expect(status).toBe('Dispatched');
  });
});
