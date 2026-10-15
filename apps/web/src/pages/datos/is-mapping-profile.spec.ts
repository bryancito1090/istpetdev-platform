import { describe, expect, it } from 'vitest';
import { isMappingProfile } from './datos.page';

// Los tres .json que trae docs/dataset/test/national-v1-seed-11: solo mapping.json es perfil.
describe('isMappingProfile', () => {
  it('reconoce el perfil de mapeo del dataset', () => {
    expect(isMappingProfile('{"name":"simulation-national","files":{"stock":{"file":"stock.csv"}}}')).toBe(true);
    expect(isMappingProfile('{"delimiter":";","decimalSeparator":",","files":{}}')).toBe(true);
  });

  it('descarta el manifiesto y el calendario aunque sean JSON válidos', () => {
    expect(isMappingProfile('{"datasetVersion":"national-v1","seed":11,"pointsCount":80}')).toBe(false);
    expect(isMappingProfile('{"Key":{"Seed":11},"Demand":[]}')).toBe(false);
  });

  it('descarta JSON inválido, listas y perfiles con claves desconocidas', () => {
    expect(isMappingProfile('no es json')).toBe(false);
    expect(isMappingProfile('[{"files":{}}]')).toBe(false);
    expect(isMappingProfile('{"files":{},"seed":11}')).toBe(false);
    expect(isMappingProfile('{"files":[]}')).toBe(false);
  });
});
