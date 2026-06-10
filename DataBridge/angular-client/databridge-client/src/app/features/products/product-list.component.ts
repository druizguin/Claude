import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup } from '@angular/forms';
import { MatTableModule } from '@angular/material/table';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatSortModule, Sort } from '@angular/material/sort';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatDialogModule, MatDialog } from '@angular/material/dialog';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatSelectModule } from '@angular/material/select';
import { MatSnackBarModule, MatSnackBar } from '@angular/material/snack-bar';
import { ApiService } from '../../core/services/api.service';
import { Product } from '../../core/models/product.model';
import { QuerySpec } from '../../core/models/jsonapi.model';
import { ProductFormComponent } from './product-form.component';

@Component({
  selector: 'app-product-list',
  standalone: true,
  imports: [
    CommonModule, FormsModule, ReactiveFormsModule,
    MatTableModule, MatPaginatorModule, MatSortModule,
    MatInputModule, MatFormFieldModule, MatButtonModule,
    MatIconModule, MatDialogModule, MatCardModule,
    MatChipsModule, MatSelectModule, MatSnackBarModule
  ],
  template: `
    <mat-card>
      <mat-card-header>
        <mat-card-title>Products <span class="count">({{ total }})</span></mat-card-title>
        <span class="toolbar-spacer"></span>
        <button mat-raised-button color="primary" (click)="openForm()">
          <mat-icon>add</mat-icon> New Product
        </button>
      </mat-card-header>

      <mat-card-content>
        <!-- Filters -->
        <div class="filter-row">
          <mat-form-field appearance="outline">
            <mat-label>Search name</mat-label>
            <input matInput [(ngModel)]="filterName" (ngModelChange)="onFilter()" placeholder="e.g. Apple">
          </mat-form-field>

          <mat-form-field appearance="outline">
            <mat-label>Category</mat-label>
            <mat-select [(ngModel)]="filterCategory" (ngModelChange)="onFilter()">
              <mat-option value="">All</mat-option>
              @for (cat of categories; track cat) {
                <mat-option [value]="cat">{{ cat }}</mat-option>
              }
            </mat-select>
          </mat-form-field>

          <mat-form-field appearance="outline">
            <mat-label>Min Price</mat-label>
            <input matInput type="number" [(ngModel)]="filterMinPrice" (ngModelChange)="onFilter()">
          </mat-form-field>

          <button mat-stroked-button (click)="clearFilters()">
            <mat-icon>clear</mat-icon> Clear
          </button>
        </div>

        <!-- Table -->
        <table mat-table [dataSource]="products" matSort (matSortChange)="onSort($event)">
          <ng-container matColumnDef="name">
            <th mat-header-cell *matHeaderCellDef mat-sort-header>Name</th>
            <td mat-cell *matCellDef="let p">{{ p.name }}</td>
          </ng-container>
          <ng-container matColumnDef="category">
            <th mat-header-cell *matHeaderCellDef mat-sort-header>Category</th>
            <td mat-cell *matCellDef="let p">
              <mat-chip>{{ p.category }}</mat-chip>
            </td>
          </ng-container>
          <ng-container matColumnDef="price">
            <th mat-header-cell *matHeaderCellDef mat-sort-header>Price</th>
            <td mat-cell *matCellDef="let p">{{ p.price | currency }}</td>
          </ng-container>
          <ng-container matColumnDef="stockQuantity">
            <th mat-header-cell *matHeaderCellDef mat-sort-header>Stock</th>
            <td mat-cell *matCellDef="let p">{{ p.stockQuantity }}</td>
          </ng-container>
          <ng-container matColumnDef="barcode">
            <th mat-header-cell *matHeaderCellDef>Barcode</th>
            <td mat-cell *matCellDef="let p">{{ p.barcode }}</td>
          </ng-container>
          <ng-container matColumnDef="actions">
            <th mat-header-cell *matHeaderCellDef>Actions</th>
            <td mat-cell *matCellDef="let p" class="actions-cell">
              <button mat-icon-button color="primary" (click)="openForm(p)">
                <mat-icon>edit</mat-icon>
              </button>
              <button mat-icon-button color="warn" (click)="delete(p)">
                <mat-icon>delete</mat-icon>
              </button>
            </td>
          </ng-container>

          <tr mat-header-row *matHeaderRowDef="columns"></tr>
          <tr mat-row *matRowDef="let row; columns: columns;"></tr>
        </table>

        <mat-paginator
          [length]="total"
          [pageSize]="pageSize"
          [pageSizeOptions]="[5, 10, 20, 50]"
          (page)="onPage($event)">
        </mat-paginator>
      </mat-card-content>
    </mat-card>
  `,
  styles: [`
    .count { font-weight: 300; font-size: 0.85em; margin-left: 8px; }
    table { width: 100%; }
    mat-card-header { display: flex; align-items: center; margin-bottom: 16px; }
  `]
})
export class ProductListComponent implements OnInit {
  private readonly api    = inject(ApiService);
  private readonly dialog = inject(MatDialog);
  private readonly snack  = inject(MatSnackBar);

  columns     = ['name', 'category', 'price', 'stockQuantity', 'barcode', 'actions'];
  categories  = ['Fruits', 'Vegetables', 'Dairy', 'Bakery', 'Beverages'];
  products: Product[] = [];
  total       = 0;
  pageSize    = 10;
  pageIndex   = 0;
  sortField   = 'name';
  sortDir: 'asc' | 'desc' = 'asc';

  filterName      = '';
  filterCategory  = '';
  filterMinPrice: number | null = null;

  ngOnInit() { this.load(); }

  load() {
    const spec = this.buildSpec();
    this.api.query<Product>('products', spec).subscribe(resp => {
      this.products = resp.data.map(r => ({ id: r.id, ...r.attributes } as unknown as Product));
      this.total    = resp.meta?.total ?? 0;
    });
  }

  buildSpec(): QuerySpec {
    const filter: Record<string, unknown> = {};
    if (this.filterName)      filter['name']     = { like: `%${this.filterName}%` };
    if (this.filterCategory)  filter['category'] = this.filterCategory;
    if (this.filterMinPrice != null) filter['price'] = { gte: this.filterMinPrice };

    return {
      from: 'products',
      filter: Object.keys(filter).length ? filter : undefined,
      orderby: [{ field: this.sortField, direction: this.sortDir }],
      page: { from: this.pageIndex * this.pageSize, offset: this.pageSize }
    };
  }

  onFilter()  { this.pageIndex = 0; this.load(); }
  onPage(e: PageEvent) { this.pageIndex = e.pageIndex; this.pageSize = e.pageSize; this.load(); }
  onSort(e: Sort) { this.sortField = e.active; this.sortDir = e.direction as 'asc' | 'desc' || 'asc'; this.load(); }

  clearFilters() {
    this.filterName = ''; this.filterCategory = ''; this.filterMinPrice = null;
    this.pageIndex  = 0;
    this.load();
  }

  openForm(product?: Product) {
    const ref = this.dialog.open(ProductFormComponent, { data: product, width: '480px' });
    ref.afterClosed().subscribe(result => { if (result) this.load(); });
  }

  delete(p: Product) {
    if (!confirm(`Delete "${p.name}"?`)) return;
    this.api.delete('products', p.id).subscribe({
      next: () => { this.snack.open('Deleted', 'OK', { duration: 2000 }); this.load(); },
      error: () => this.snack.open('Delete failed', 'OK', { duration: 3000 })
    });
  }
}
