export interface Product {
  id: string;
  name: string;
  category: string;
  price: number;
  stockQuantity: number;
  barcode: string;
  description?: string;
}
