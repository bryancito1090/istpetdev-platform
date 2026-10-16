import {
  DeliveryListItem,
  HandlingUnitResponse,
  MobileAssignmentResponse
} from '@istpetdev/shared-core';

/**
 * Fixture de datos sintéticos etiquetados para el entorno de demo offline de Campo (Spec 004).
 * Cumple con G1 (datos parametrizados y etiquetados como sintéticos, no hardcodeados en lógica).
 */

export interface MockServerData {
  driverAccountId: string;
  driverName: string;
  assignment: MobileAssignmentResponse;
  deliveries: DeliveryListItem[];
  handlingUnits: Record<string, HandlingUnitResponse>;
  idempotencyRecords: Record<
    string,
    {
      action: string;
      operationId: string;
      requestFingerprint: string;
      responseStatusCode: number;
      responseBody: any;
      recordedAtUtc: string;
    }
  >;
  tokens: Record<
    string,
    {
      unitId: string;
      token: string;
      isRevoked: boolean;
      expiresAtUtc: string;
    }
  >;
  evidenceFiles: Record<
    string,
    {
      evidenceId: string;
      receiptId: string;
      kind: 'photo' | 'signature';
      mimeType: string;
      byteSize: number;
      sha256: string;
      status: 'Pending' | 'Uploaded' | 'Confirmed' | 'Failed';
      confirmedAtUtc?: string;
    }
  >;
}

export const INITIAL_FIELD_DEMO_DATA: MockServerData = {
  driverAccountId: 'driver-alex-001',
  driverName: 'Alexander Martínez',
  assignment: {
    serverTimeUtc: new Date().toISOString(),
    driverAccountId: 'driver-alex-001',
    routes: [
      {
        routePlanId: 'rp-quito-sur-001',
        revision: 1,
        downloadedAtUtc: new Date().toISOString(),
        driverMessage: 'Ruta multi-bodega: Recoger lote en Bodega Norte y Bodega Sur antes de entregas hospitalarias.',
        vehicleCode: 'VEH-01-FURGON',
        stopsCount: 4,
        totalDistanceKm: 28.4,
        estimatedDurationMinutes: 110,
        deliveries: [
          {
            deliveryId: 'pk-wh-norte-01',
            sequence: 1,
            kind: 'Pickup',
            locationId: 'wh-norte-01',
            pointCode: 'BOD-NORTE',
            pointName: 'Bodega Distribución Norte (Quito)',
            address: 'Parque Industrial Carcelén, Galpón 4',
            dueAtUtc: new Date(Date.now() + 1 * 3600 * 1000).toISOString(),
            status: 'Dispatched',
            handlingUnits: [
              {
                handlingUnitId: 'hu-pickup-001',
                code: 'REC-BOD-01',
                deliveryId: 'pk-wh-norte-01',
                status: 'Dispatched',
                labelUrl: 'https://istpetdev.local/campo/label/hu-pickup-001',
                token: 'tok-sec-demo-pk-01',
                lines: [
                  {
                    deliveryLineId: 'dl-pk-1',
                    lotId: 'LOT-PINOK-01',
                    sku: 'PINOKLIN-DESINF-5L',
                    quantity: 20,
                    unit: 'Galón'
                  }
                ],
                createdAtUtc: new Date().toISOString()
              }
            ]
          },
          {
            deliveryId: 'del-hosp-001',
            sequence: 2,
            kind: 'Delivery',
            pointCode: 'PTO-HOSP-01',
            pointName: 'Hospital General San Francisco',
            address: 'Av. Jaime Roldós Aguilera y Calle 14, Quito',
            dueAtUtc: new Date(Date.now() + 4 * 3600 * 1000).toISOString(),
            status: 'Dispatched',
            handlingUnits: [
              {
                handlingUnitId: 'hu-caja-001',
                code: 'CAJA-MED-001',
                deliveryId: 'del-hosp-001',
                status: 'Dispatched',
                labelUrl: 'https://istpetdev.local/campo/label/hu-caja-001',
                token: 'tok-sec-demo-hosp-001',
                lines: [
                  {
                    deliveryLineId: 'dl-001-1',
                    lotId: 'LOT-PINOK-01',
                    sku: 'PINOKLIN-DESINF-5L',
                    quantity: 12,
                    unit: 'Galón'
                  }
                ],
                createdAtUtc: new Date().toISOString()
              }
            ]
          },
          {
            deliveryId: 'del-clin-002',
            sequence: 3,
            kind: 'Delivery',
            pointCode: 'PTO-CLIN-02',
            pointName: 'Clínica Metropolitana Norte',
            address: 'Av. Naciones Unidas y Amazonas, Quito',
            dueAtUtc: new Date(Date.now() + 6 * 3600 * 1000).toISOString(),
            status: 'Dispatched',
            handlingUnits: [
              {
                handlingUnitId: 'hu-caja-002',
                code: 'CAJA-MED-002',
                deliveryId: 'del-clin-002',
                status: 'Dispatched',
                labelUrl: 'https://istpetdev.local/campo/label/hu-caja-002',
                token: 'tok-sec-demo-clin-002',
                lines: [
                  {
                    deliveryLineId: 'dl-002-1',
                    lotId: 'LOT-AMOX-01',
                    sku: 'AMOXICILINA-500MG',
                    quantity: 15,
                    unit: 'Frascos'
                  },
                  {
                    deliveryLineId: 'dl-002-2',
                    lotId: 'LOT-ALC-01',
                    sku: 'ALCOHOL-70-1L',
                    quantity: 8,
                    unit: 'Litros'
                  }
                ],
                createdAtUtc: new Date().toISOString()
              }
            ]
          },
          {
            deliveryId: 'del-salud-003',
            sequence: 4,
            kind: 'Delivery',
            pointCode: 'PTO-SALUD-03',
            pointName: 'Centro de Salud Nro 3',
            address: 'Calle Guayaquil y Espejo, Centro Histórico, Quito',
            dueAtUtc: new Date(Date.now() + 8 * 3600 * 1000).toISOString(),
            status: 'Dispatched',
            handlingUnits: [
              {
                handlingUnitId: 'hu-caja-003',
                code: 'CAJA-MED-003',
                deliveryId: 'del-salud-003',
                status: 'Dispatched',
                labelUrl: 'https://istpetdev.local/campo/label/hu-caja-003',
                token: 'tok-sec-demo-salud-003',
                lines: [
                  {
                    deliveryLineId: 'dl-003-1',
                    lotId: 'LOT-SUERO-01',
                    sku: 'SUERO-FISIOLOGICO-500ML',
                    quantity: 24,
                    unit: 'Fundas'
                  },
                  {
                    deliveryLineId: 'dl-003-2',
                    lotId: 'LOT-PINOK-01',
                    sku: 'PINOKLIN-DESINF-5L',
                    quantity: 6,
                    unit: 'Galón'
                  }
                ],
                createdAtUtc: new Date().toISOString()
              }
            ]
          }
        ]
      }
    ]
  },
  deliveries: [
    {
      deliveryId: 'del-hosp-001',
      routePlanId: 'rp-quito-sur-001',
      pointCode: 'PTO-HOSP-01',
      pointName: 'Hospital General San Francisco',
      dueAtUtc: new Date(Date.now() + 4 * 3600 * 1000).toISOString(),
      status: 'Dispatched',
      evidenceStatus: 'Pending',
      packagesCount: 1
    },
    {
      deliveryId: 'del-clin-002',
      routePlanId: 'rp-quito-sur-001',
      pointCode: 'PTO-CLIN-02',
      pointName: 'Clínica Metropolitana Norte',
      dueAtUtc: new Date(Date.now() + 6 * 3600 * 1000).toISOString(),
      status: 'Dispatched',
      evidenceStatus: 'Pending',
      packagesCount: 1
    },
    {
      deliveryId: 'del-salud-003',
      routePlanId: 'rp-quito-sur-001',
      pointCode: 'PTO-SALUD-03',
      pointName: 'Centro de Salud Nro 3',
      dueAtUtc: new Date(Date.now() + 8 * 3600 * 1000).toISOString(),
      status: 'Dispatched',
      evidenceStatus: 'Pending',
      packagesCount: 1
    }
  ],
  handlingUnits: {
    'hu-caja-001': {
      handlingUnitId: 'hu-caja-001',
      code: 'CAJA-MED-001',
      deliveryId: 'del-hosp-001',
      status: 'Dispatched',
      labelUrl: 'https://istpetdev.local/campo/label/hu-caja-001',
      token: 'tok-sec-demo-hosp-001',
      lines: [
        {
          deliveryLineId: 'dl-001-1',
          lotId: 'LOT-PINOK-01',
          sku: 'PINOKLIN-DESINF-5L',
          quantity: 12,
          unit: 'Galón'
        }
      ],
      createdAtUtc: new Date().toISOString()
    },
    'hu-caja-002': {
      handlingUnitId: 'hu-caja-002',
      code: 'CAJA-MED-002',
      deliveryId: 'del-clin-002',
      status: 'Dispatched',
      labelUrl: 'https://istpetdev.local/campo/label/hu-caja-002',
      token: 'tok-sec-demo-clin-002',
      lines: [
        {
          deliveryLineId: 'dl-002-1',
          lotId: 'LOT-AMOX-01',
          sku: 'AMOXICILINA-500MG',
          quantity: 15,
          unit: 'Frascos'
        },
        {
          deliveryLineId: 'dl-002-2',
          lotId: 'LOT-ALC-01',
          sku: 'ALCOHOL-70-1L',
          quantity: 8,
          unit: 'Litros'
        }
      ],
      createdAtUtc: new Date().toISOString()
    },
    'hu-caja-003': {
      handlingUnitId: 'hu-caja-003',
      code: 'CAJA-MED-003',
      deliveryId: 'del-salud-003',
      status: 'Dispatched',
      labelUrl: 'https://istpetdev.local/campo/label/hu-caja-003',
      token: 'tok-sec-demo-salud-003',
      lines: [
        {
          deliveryLineId: 'dl-003-1',
          lotId: 'LOT-SUERO-01',
          sku: 'SUERO-FISIOLOGICO-500ML',
          quantity: 24,
          unit: 'Fundas'
        },
        {
          deliveryLineId: 'dl-003-2',
          lotId: 'LOT-PINOK-01',
          sku: 'PINOKLIN-DESINF-5L',
          quantity: 6,
          unit: 'Galón'
        }
      ],
      createdAtUtc: new Date().toISOString()
    }
  },
  idempotencyRecords: {},
  tokens: {
    'tok-sec-demo-hosp-001': {
      unitId: 'hu-caja-001',
      token: 'tok-sec-demo-hosp-001',
      isRevoked: false,
      expiresAtUtc: new Date(Date.now() + 7 * 86400 * 1000).toISOString()
    }
  },
  evidenceFiles: {}
};
