import { Address } from './address.model';

export interface User {
  id: string;
  name: string;
  email: string;
  age: number;
  country: string;
  status: string;
  signupDate: string;
  addressPrincipalId?: string;
  addressPrincipal?: Address;
}
