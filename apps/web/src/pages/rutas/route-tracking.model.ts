/**
 * Modelos, traductores y funciones auxiliares para el seguimiento en tiempo real
 * de la ruta en el Planificador (#231).
 * Estados de bultos alineados con la PWA (#162 y #230).
 */

export interface StopProgressView {
  sequence: number;
  stopId?: string;
  code: string;
  name: string;
  kind: 'Pickup' | 'Delivery';
  kindLabel: string;
  status: string;
  statusLabel: string;
  deliveryStatus?: string;
  deliveryStatusLabel?: string;
  isCurrent: boolean;
  isCompleted: boolean;
  isPending: boolean;
  etaOrWindow: string;
  handlingUnitsCount: number;
  handlingUnits: Array<{
    id: string;
    code: string;
    status: string;
    statusLabel: string;
  }>;
}

export interface RouteTrackingSummary {
  totalStops: number;
  totalDeliveryStops: number;
  deliveredStopsCount: number;
  bultosInVehicle: number;
  totalBultos: number;
  bultosDelivered: number;
  currentStopSequence: number | null;
  summaryText: string;
  isFinished: boolean;
}

/**
 * Traduce el estado del bulto (Handling Unit) a español, alineado con la PWA (#162).
 */
export function translateHuStatus(status?: string | null): string {
  if (!status) return 'Sin estado';
  const clean = status.trim();
  switch (clean) {
    case 'Draft':
    case 'Planned':
      return 'En preparación';
    case 'Prepared':
      return 'En bodega';
    case 'Sealed':
      return 'Sellado en bodega';
    case 'Dispatched':
      return 'Listo para recoger';
    case 'InTransit':
      return 'En vehículo';
    case 'Delivered':
      return 'Entregado';
    case 'Received':
      return 'Recibido conforme';
    case 'Partial':
      return 'Recibido parcial';
    case 'Cancelled':
      return 'Cancelado';
    case 'Rejected':
      return 'Rechazado';
    default:
      return clean;
  }
}

/**
 * Clase CSS para la insignia del estado del bulto.
 */
export function getHuStatusBadgeClass(status?: string | null): string {
  const clean = (status || '').trim();
  switch (clean) {
    case 'Received':
      return 'bg-emerald-100 text-emerald-800 border-emerald-300';
    case 'Delivered':
      return 'bg-teal-100 text-teal-800 border-teal-300';
    case 'Partial':
      return 'bg-amber-100 text-amber-800 border-amber-300';
    case 'InTransit':
      return 'bg-blue-100 text-blue-800 border-blue-300';
    case 'Dispatched':
      return 'bg-indigo-100 text-indigo-800 border-indigo-300';
    case 'Sealed':
    case 'Prepared':
      return 'bg-purple-100 text-purple-800 border-purple-300';
    case 'Rejected':
    case 'Cancelled':
      return 'bg-rose-100 text-rose-800 border-rose-300';
    default:
      return 'bg-slate-100 text-slate-700 border-slate-300';
  }
}

/**
 * Traduce el estado de la entrega a español.
 */
export function translateDeliveryStatus(status?: string | null): string {
  if (!status) return 'Planificada';
  const clean = status.trim();
  switch (clean) {
    case 'Draft':
    case 'Proposed':
      return 'Planificada';
    case 'WaitingWarehouse':
      return 'Esperando a Bodega';
    case 'Dispatched':
      return 'Despachada';
    case 'InTransit':
      return 'En ruta';
    case 'Delivered':
      return 'Entregada';
    case 'Received':
      return 'Recibida conforme';
    case 'Partial':
      return 'Recibida parcial';
    case 'Cancelled':
      return 'Cancelada';
    default:
      return clean;
  }
}

/**
 * Clase CSS para la insignia del estado de entrega.
 */
export function getDeliveryStatusBadgeClass(status?: string | null): string {
  const clean = (status || '').trim();
  switch (clean) {
    case 'Received':
    case 'Delivered':
      return 'bg-emerald-100 text-emerald-800 border-emerald-200';
    case 'Partial':
      return 'bg-amber-100 text-amber-800 border-amber-200';
    case 'InTransit':
      return 'bg-blue-100 text-blue-800 border-blue-200';
    case 'Dispatched':
      return 'bg-indigo-50 text-indigo-700 border-indigo-200';
    case 'Cancelled':
      return 'bg-rose-100 text-rose-800 border-rose-200';
    default:
      return 'bg-slate-100 text-slate-700 border-slate-200';
  }
}

/**
 * Traduce el estado de la parada a español.
 */
export function translateStopStatus(status?: string | null): string {
  if (!status) return 'Pendiente';
  const clean = status.trim();
  switch (clean) {
    case 'Completed':
      return 'Completada';
    case 'InTransit':
    case 'Active':
      return 'En curso';
    case 'Arrived':
      return 'En sitio';
    case 'Proposed':
      return 'Planificada';
    case 'Pending':
      return 'Pendiente';
    case 'Draft':
      return 'Borrador';
    default:
      return clean;
  }
}

/**
 * Evalúa si una parada se considera finalizada/entregada.
 */
export function isStopCompleted(stop: {
  kind?: string;
  status?: string;
  deliveryStatus?: string;
  handlingUnits?: Array<{ status: string }>;
}): boolean {
  if (stop.status === 'Completed') return true;
  if (stop.deliveryStatus === 'Delivered' || stop.deliveryStatus === 'Received' || stop.deliveryStatus === 'Partial') {
    return true;
  }
  if (stop.kind === 'Pickup') {
    // Si la recogida tiene bultos y todos ya están en vehículo o entregados, se considera completada
    const hus = stop.handlingUnits || [];
    if (hus.length > 0 && hus.every(hu => hu.status === 'InTransit' || hu.status === 'Delivered' || hu.status === 'Received' || hu.status === 'Partial')) {
      return true;
    }
  }
  return false;
}

/**
 * Calcula el resumen de avance en tiempo real para el Planificador (#231):
 * Ejemplo: «3 de 6 paradas entregadas · 2 bultos en el vehículo».
 */
export function computeRouteTrackingSummary(stops: any[] | null | undefined): RouteTrackingSummary {
  const list = stops || [];
  if (list.length === 0) {
    return {
      totalStops: 0,
      totalDeliveryStops: 0,
      deliveredStopsCount: 0,
      bultosInVehicle: 0,
      totalBultos: 0,
      bultosDelivered: 0,
      currentStopSequence: null,
      summaryText: 'Sin paradas en la ruta',
      isFinished: false
    };
  }

  // Paradas de entrega (excluyendo la recogida en bodega si existe)
  const deliveryStops = list.filter(s => s.kind !== 'Pickup');
  const totalDeliveryStops = deliveryStops.length > 0 ? deliveryStops.length : list.length;
  const deliveredStopsCount = (deliveryStops.length > 0 ? deliveryStops : list).filter(isStopCompleted).length;

  // Bultos en toda la ruta
  const allHandlingUnits: Array<{ status: string }> = [];
  list.forEach(s => {
    (s.handlingUnits || []).forEach((hu: any) => allHandlingUnits.push(hu));
  });

  const totalBultos = allHandlingUnits.length;
  const bultosDelivered = allHandlingUnits.filter(hu =>
    hu.status === 'Delivered' || hu.status === 'Received' || hu.status === 'Partial'
  ).length;

  // Bultos en el vehículo: estado InTransit o si la recogida ya se hizo y aún no se entrega
  let bultosInVehicle = allHandlingUnits.filter(hu => hu.status === 'InTransit').length;
  if (bultosInVehicle === 0 && totalBultos > 0) {
    const pickupStop = list.find(s => s.kind === 'Pickup');
    const isPickupDone = pickupStop ? isStopCompleted(pickupStop) : false;
    if (isPickupDone) {
      bultosInVehicle = Math.max(0, totalBultos - bultosDelivered);
    }
  }

  // Primera parada no completada en orden secuencial = «parada actual»
  const sortedStops = [...list].sort((a, b) => (a.sequence || 0) - (b.sequence || 0));
  const currentStop = sortedStops.find(s => !isStopCompleted(s));
  const currentStopSequence = currentStop ? currentStop.sequence : null;
  const isFinished = deliveredStopsCount === totalDeliveryStops && totalDeliveryStops > 0;

  const summaryText = isFinished
    ? `Ruta completada: ${deliveredStopsCount} de ${totalDeliveryStops} paradas entregadas`
    : `${deliveredStopsCount} de ${totalDeliveryStops} paradas entregadas · ${bultosInVehicle} ${bultosInVehicle === 1 ? 'bulto' : 'bultos'} en el vehículo`;

  return {
    totalStops: list.length,
    totalDeliveryStops,
    deliveredStopsCount,
    bultosInVehicle,
    totalBultos,
    bultosDelivered,
    currentStopSequence,
    summaryText,
    isFinished
  };
}

/**
 * Transforma las paradas del RoutePlanDetailDto en una lista de progreso con estados en español
 * y la identificación de la «parada actual».
 */
export function computeStopProgressList(stops: any[] | null | undefined): StopProgressView[] {
  const list = stops || [];
  if (list.length === 0) return [];

  const sorted = [...list].sort((a, b) => (a.sequence || 0) - (b.sequence || 0));
  const currentStop = sorted.find(s => !isStopCompleted(s));
  const currentSeq = currentStop ? currentStop.sequence : null;

  return sorted.map(s => {
    const isCompleted = isStopCompleted(s);
    const isCurrent = !isCompleted && s.sequence === currentSeq;
    const isPending = !isCompleted && !isCurrent;

    const etaOrWindow = s.etaUtc
      ? new Date(s.etaUtc).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })
      : (s.windowStart && s.windowEnd ? `${s.windowStart} - ${s.windowEnd}` : '08:30');

    const hus = (s.handlingUnits || []).map((hu: any) => ({
      id: hu.id || hu.code,
      code: hu.code,
      status: hu.status,
      statusLabel: translateHuStatus(hu.status)
    }));

    const rawStatus = isCompleted
      ? 'Completed'
      : (isCurrent ? 'InTransit' : (s.status || 'Pending'));

    return {
      sequence: s.sequence,
      stopId: s.stopId || s.servicePointId,
      code: s.servicePointCode || s.code || `P-${s.sequence}`,
      name: s.servicePointName || s.name || s.servicePointCode || `Parada ${s.sequence}`,
      kind: s.kind === 'Pickup' ? 'Pickup' : 'Delivery',
      kindLabel: s.kind === 'Pickup' ? 'Recogida en Bodega' : 'Entrega',
      status: rawStatus,
      statusLabel: isCompleted
        ? (s.kind === 'Pickup' ? 'Recogido' : 'Entregada')
        : (isCurrent ? 'En camino' : translateStopStatus(s.status)),
      deliveryStatus: s.deliveryStatus,
      deliveryStatusLabel: translateDeliveryStatus(s.deliveryStatus),
      isCurrent,
      isCompleted,
      isPending,
      etaOrWindow,
      handlingUnitsCount: hus.length,
      handlingUnits: hus
    };
  });
}
