import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
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
import { Purchase } from '../../core/models/purchase.model';
import { PurchaseFormComponent } from './purchase-form.component';

@Component({
  selector: 'app-purchase-list',
  standalone: true,
  imports: [
    CommonModule, FormsModule,
    MatTableModule, MatPaginatorModule, MatSortModule,
    MatInputModule, MatFormFieldModule, MatButtonModule,
    MatIconModule, MatDialogModule, MatCardModule,
    MatChipsModule, MatSelectModule, MatSnackBarModule
  ],
  template: `
    <mat-card>
      <mat-card-header>
        <mat-card-title>Purchases <span class="count">({{ total }})</span></mat-card-title>
        <span class="toolbar-spacer"></span>
        <button mat-raised-button color="primary" (click)="openForm()">
          <mat-icon>add_shopping_cart</mat-icon> New Purchase
        </button>
      </mat-card-header>
      <mat-card-content>
        <div class="filter-row">
          <mat-form-field appearance="outline">
            <mat-label>Status</mat-label>
            <mat-select [(ngModel)]="filterStatus" (ngModelChange)="onFilter()">
              <mat-option value="">All</mat-option>
              <mat-option value="completed">Completed</mat-option>
              <mat-option value="pending">Pending</mat-option>
            </mat-select>
          </mat-form-field>
          <button mat-stroked-button (click)="clearFilters()"><mat-icon>clear</mat-icon></button>
        </div>

        <table mat-table [dataSource]="purchases" matSort (matSortChange)="onSort($event)">
          <ng-container matColumnDef="purchaseDate">
            <th mat-header-cell *matHeaderCellDef mat-sort-header>Date</th>
            <td mat-cell *matCellDef="let p">{{ p.purchaseDate | date:'short' }}</td>
          </ng-container>
          <ng-container matColumnDef="userId">
            <th mat-header-cell *matHeaderCellDef>User ID</th>
            <td mat-cell *matCellDef="let p" style="font-size:11px">{{ p.userId | slice:0:8 }}…</td>
          </ng-container>
          <ng-container matColumnDef="productId">
            <th mat-header-cell *matHeaderCellDef>Product ID</th>
            <td mat-cell *matCellDef="let p" style="font-size:11px">{{ p.productId | slice:0:8 }}…</td>
          </ng-container>
          <ng-container matColumnDef="quantity">
            <th mat-header-cell *matHeaderCellDef mat-sort-header>Qty</th>
            <td mat-cell *matCellDef="let p">{{ p.quantity }}</td>
          </ng-container>
          <ng-container matColumnDef="totalPrice">
            <th mat-header-cell *matHeaderCellDef mat-sort-header>Total</th>
            <td mat-cell *matCellDef="let p">{{ p.totalPrice | currency }}</td>
          </ng-container>
          <ng-container matColumnDef="status">
            <th mat-header-cell *matHeaderCellDef>Status</th>
            <td mat-cell *matCellDef="let p">
              <mat-chip [class]="'chip-' + p.status">{{ p.status }}</mat-chip>
            </td>
          </ng-container>
          <ng-container matColumnDef="actions">
            <th mat-header-cell *matHeaderCellDef>Actions</th>
            <td mat-cell *matCellDef="let p" class="actions-cell">
              <button mat-icon-button color="warn" (click)="delete(p)"><mat-icon>delete</mat-icon></button>
            </td>
          </ng-container>
          <tr mat-header-row *matHeaderRowDef="columns"></tr>
          <tr mat-row *matRowDef="let row; columns: columns;"></tr>
        </table>
        <mat-paginator [length]="total" [pageSize]="pageSize" [pageSizeOptions]="[5,10,20]"
          (page)="onPage($event)"></mat-paginator>
      </mat-card-content>
    </mat-card>
  `,
  styles: [`.count{font-weight:300;font-size:.85em;margin-left:8px}table{width:100%}mat-card-header{display:flex;align-items:center;margin-bottom:16px}`]
})
export class PurchaseListComponent implements OnInit {
  private readonly api    = inject(ApiService);
  private readonly dialog = inject(MatDialog);
  private readonly snack  = inject(MatSnackBar);

  columns = ['purchaseDate', 'userId', 'productId', 'quantity', 'totalPrice', 'status', 'actions'];
  purchases: Purchase[] = [];
  total = 0; pageSize = 10; pageIndex = 0;
  sortField = 'purchaseDate'; sortDir: 'asc' | 'desc' = 'desc';
  filterStatus = '';

  ngOnInit() { this.load(); }

  load() {
    const filter: Record<string, unknown> = {};
    if (this.filterStatus) filter['status'] = this.filterStatus;

    const spec = {
      from: 'purchases',
      filter: Object.keys(filter).length ? filter : undefined,
      orderby: [{ field: this.sortField, direction: this.sortDir }],
      page: { from: this.pageIndex * this.pageSize, offset: this.pageSize }
    };

    this.api.query<Purchase>('purchases', spec).subscribe(resp => {
      this.purchases = resp.data.map(r => ({ id: r.id, ...r.attributes } as unknown as Purchase));
      this.total     = resp.meta?.total ?? 0;
    });
  }

  onFilter() { this.pageIndex = 0; this.load(); }
  onPage(e: PageEvent) { this.pageIndex = e.pageIndex; this.pageSize = e.pageSize; this.load(); }
  onSort(e: Sort) { this.sortField = e.active; this.sortDir = e.direction as 'asc'|'desc' || 'desc'; this.load(); }
  clearFilters() { this.filterStatus = ''; this.pageIndex = 0; this.load(); }

  openForm() {
    const ref = this.dialog.open(PurchaseFormComponent, { width: '500px' });
    ref.afterClosed().subscribe(r => { if (r) this.load(); });
  }

  delete(p: Purchase) {
    if (!confirm('Delete this purchase?')) return;
    this.api.delete('purchases', p.id).subscribe({
      next: () => { this.snack.open('Deleted', 'OK', { duration: 2000 }); this.load(); },
      error: () => this.snack.open('Delete failed', 'OK', { duration: 3000 })
    });
  }
}
