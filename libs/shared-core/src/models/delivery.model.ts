export type DeliveryStatus = 'Draft' | 'Dispatched' | 'InTransit' | 'Received' | 'Cancelled';

export interface Delivery {
  id: string;
  trackingNumber: string;
  originServicePointId: string;
  destinationServicePointId: string;
  status: DeliveryStatus;
  version: number;
}
