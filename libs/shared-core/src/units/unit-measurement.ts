export type MeasurementUnit = 'KG' | 'G' | 'L' | 'ML' | 'UNIT' | 'BOX' | 'VIAL';

export interface UnitDefinition {
  code: MeasurementUnit;
  label: string;
  isFractional: boolean;
}

export const SUPPORTED_UNITS: Record<MeasurementUnit, UnitDefinition> = {
  KG: { code: 'KG', label: 'Kilogramos', isFractional: true },
  G: { code: 'G', label: 'Gramos', isFractional: true },
  L: { code: 'L', label: 'Litros', isFractional: true },
  ML: { code: 'ML', label: 'Mililitros', isFractional: true },
  UNIT: { code: 'UNIT', label: 'Unidad', isFractional: false },
  BOX: { code: 'BOX', label: 'Caja', isFractional: false },
  VIAL: { code: 'VIAL', label: 'Frasco / Ampolla', isFractional: false }
};
