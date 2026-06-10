export interface Purchase {
  id: string;
  userId: string;
  productId: string;
  quantity: number;
  totalPrice: number;
  purchaseDate: string;
  status: string;
}
