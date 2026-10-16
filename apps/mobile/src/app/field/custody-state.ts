// #162: rangos de avance de la cadena de custodia. El estado efectivo nunca retrocede.
const STATUS_RANK: Record<string, number> = {
  Draft: 0,
  Planned: 0,
  Sealed: 0,
  Prepared: 0,
  Dispatched: 1,
  InTransit: 2,
  Delivered: 3,
  Received: 4,
  Partial: 4,
};

export type EffectiveDeliveryStatusInput = {
  deliveryStatus: string;
  handlingUnits?: Array<{ status: string }>;
  acceptedCustodyStatus?: string | null;
};

/**
 * Estado autoritativo y monotónico de la entrega: combina el snapshot descargado, el último
 * comando de custodia confirmado por el servidor y el estado de los bultos. Un snapshot viejo
 * (p.ej. Delivered con un Deliver confirmado) no puede sobrescribir un estado más avanzado
 * (Received/Partial), y el legado Delivered con bultos Received/Partial se interpreta como recibido.
 */
export function effectiveDeliveryStatus(input: EffectiveDeliveryStatusInput): string {
  const candidates: string[] = [input.deliveryStatus];
  if (input.acceptedCustodyStatus) {
    candidates.push(input.acceptedCustodyStatus);
  }

  const units = input.handlingUnits ?? [];
  if (units.length > 0 && units.every((hu) => hu.status === 'Received')) {
    candidates.push('Received');
  } else if (units.some((hu) => hu.status === 'Partial')) {
    candidates.push('Partial');
  }

  return candidates.reduce((best, current) =>
    rankOf(current) > rankOf(best) ? current : best,
  );
}

function rankOf(status: string): number {
  return STATUS_RANK[status] ?? -1;
}
