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
        driverMessage: 'Ruta inicial asignada. Entregas prioritarias en sector Hospitalario Norte.',
        vehicleCode: 'VEH-01-FURGON',
        stopsCount: 3,
        deliveries: [
          {
            deliveryId: 'del-hosp-001',
            sequence: 1,
            pointCode: 'PTO-HOSP-01',
            pointName: 'Hospital General San Francisco',
            address: 'Av. Jaime Roldós Aguilera y Calle 14, Quito',
            dueAtUtc: new Date(Date.now() + 4 * 3600 * 1000).toISOString(),
            status: 'Prepared',
            handlingUnits: [
              {
                handlingUnitId: 'hu-caja-001',
                code: 'CAJA-MED-001',
                deliveryId: 'del-hosp-001',
                status: 'Prepared',
                labelUrl: 'https://istpetdev.local/campo/label/hu-caja-001',
                token: 'tok-sec-demo-hosp-001',
                lines: [
                  {
                    deliveryLineId: 'dl-001-1',
                    lotId: 'LOT-DESINF-A1',
                    sku: 'SKU-AMONIO-CUAT-4L',
                    quantity: 12,
                    unit: 'L'
                  }
                ],
                createdAtUtc: new Date().toISOString()
              }
            ]
          },
          {
            deliveryId: 'del-clin-002',
            sequence: 2,
            pointCode: 'PTO-CLIN-02',
            pointName: 'Clínica Metropolitana Norte',
            address: 'Av. Naciones Unidas y Amazonas, Quito',
            dueAtUtc: new Date(Date.now() + 6 * 3600 * 1000).toISOString(),
            status: 'Planned',
            handlingUnits: []
          },
          {
            deliveryId: 'del-salud-003',
            sequence: 3,
            pointCode: 'PTO-SALUD-03',
            pointName: 'Centro de Salud Nro 3',
            address: 'Calle Guayaquil y Espejo, Centro Histórico, Quito',
            dueAtUtc: new Date(Date.now() + 8 * 3600 * 1000).toISOString(),
            status: 'Planned',
            handlingUnits: []
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
      status: 'Prepared',
      evidenceStatus: 'Pending',
      packagesCount: 1
    },
    {
      deliveryId: 'del-clin-002',
      routePlanId: 'rp-quito-sur-001',
      pointCode: 'PTO-CLIN-02',
      pointName: 'Clínica Metropolitana Norte',
      dueAtUtc: new Date(Date.now() + 6 * 3600 * 1000).toISOString(),
      status: 'Planned',
      evidenceStatus: 'Pending',
      packagesCount: 0
    },
    {
      deliveryId: 'del-salud-003',
      routePlanId: 'rp-quito-sur-001',
      pointCode: 'PTO-SALUD-03',
      pointName: 'Centro de Salud Nro 3',
      dueAtUtc: new Date(Date.now() + 8 * 3600 * 1000).toISOString(),
      status: 'Planned',
      evidenceStatus: 'Pending',
      packagesCount: 0
    }
  ],
  handlingUnits: {
    'hu-caja-001': {
      handlingUnitId: 'hu-caja-001',
      code: 'CAJA-MED-001',
      deliveryId: 'del-hosp-001',
      status: 'Prepared',
      labelUrl: 'https://istpetdev.local/campo/label/hu-caja-001',
      token: 'tok-sec-demo-hosp-001',
      lines: [
        {
          deliveryLineId: 'dl-001-1',
          lotId: 'LOT-DESINF-A1',
          sku: 'SKU-AMONIO-CUAT-4L',
          quantity: 12,
          unit: 'L'
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
