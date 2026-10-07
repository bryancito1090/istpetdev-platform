export interface InventoryItem {
  id: string;
  servicePointId: string;
  productId: string;
  lotNumber: string;
  quantity: number;
  expirationDateUtc?: string;
}
