export interface ServicePoint {
  id: string;
  code: string;
  name: string;
  type: string;
  latitude: number | null;
  longitude: number | null;
  isActive: boolean;
}
