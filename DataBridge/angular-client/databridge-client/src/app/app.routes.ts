import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', redirectTo: 'products', pathMatch: 'full' },
  {
    path: 'products',
    loadComponent: () => import('./features/products/product-list.component')
      .then(m => m.ProductListComponent)
  },
  {
    path: 'users',
    loadComponent: () => import('./features/users/user-list.component')
      .then(m => m.UserListComponent)
  },
  {
    path: 'purchases',
    loadComponent: () => import('./features/purchases/purchase-list.component')
      .then(m => m.PurchaseListComponent)
  },
  {
    path: 'audit',
    loadComponent: () => import('./features/audit/audit-list.component')
      .then(m => m.AuditListComponent)
  }
];
