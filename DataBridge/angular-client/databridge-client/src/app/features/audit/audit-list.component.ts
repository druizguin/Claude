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
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatSelectModule } from '@angular/material/select';
import { ApiService } from '../../core/services/api.service';
import { AuditRecord } from '../../core/models/audit.model';

@Component({
  selector: 'app-audit-list',
  standalone: true,
  imports: [
    CommonModule, FormsModule,
    MatTableModule, MatPaginatorModule, MatSortModule,
    MatInputModule, MatFormFieldModule, MatButtonModule,
    MatIconModule, MatCardModule, MatChipsModule, MatSelectModule
  ],
  template: `
    <mat-card>
      <mat-card-header>
        <mat-card-title>Audit Log <span class="count">({{ total }})</span></mat-card-title>
      </mat-card-header>
      <mat-card-content>
        <div class="filter-row">
          <mat-form-field appearance="outline">
            <mat-label>Operation</mat-label>
            <mat-select [(ngModel)]="filterOp" (ngModelChange)="onFilter()">
              <mat-option value="">All</mat-option>
              <mat-option value="Create">Create</mat-option>
              <mat-option value="Read">Read</mat-option>
              <mat-option value="Update">Update</mat-option>
              <mat-option value="Delete">Delete</mat-option>
            </mat-select>
          </mat-form-field>
          <mat-form-field appearance="outline">
            <mat-label>Entity Type</mat-label>
            <mat-select [(ngModel)]="filterEntity" (ngModelChange)="onFilter()">
              <mat-option value="">All</mat-option>
              <mat-option value="Product">Product</mat-option>
              <mat-option value="User">User</mat-option>
              <mat-option value="Purchase">Purchase</mat-option>
            </mat-select>
          </mat-form-field>
          <button mat-stroked-button (click)="clearFilters()"><mat-icon>clear</mat-icon></button>
        </div>

        <table mat-table [dataSource]="records" matSort (matSortChange)="onSort($event)">
          <ng-container matColumnDef="timestamp">
            <th mat-header-cell *matHeaderCellDef mat-sort-header>Timestamp</th>
            <td mat-cell *matCellDef="let r">{{ r.timestamp | date:'medium' }}</td>
          </ng-container>
          <ng-container matColumnDef="operationType">
            <th mat-header-cell *matHeaderCellDef>Operation</th>
            <td mat-cell *matCellDef="let r">
              <mat-chip [style.background]="opColor(r.operationType)">{{ r.operationType }}</mat-chip>
            </td>
          </ng-container>
          <ng-container matColumnDef="entityType">
            <th mat-header-cell *matHeaderCellDef>Entity</th>
            <td mat-cell *matCellDef="let r">{{ r.entityType }}</td>
          </ng-container>
          <ng-container matColumnDef="entityId">
            <th mat-header-cell *matHeaderCellDef>Entity ID</th>
            <td mat-cell *matCellDef="let r" style="font-size:11px">{{ r.entityId | slice:0:8 }}…</td>
          </ng-container>
          <ng-container matColumnDef="personName">
            <th mat-header-cell *matHeaderCellDef>Person</th>
            <td mat-cell *matCellDef="let r">{{ r.personName }}</td>
          </ng-container>

          <tr mat-header-row *matHeaderRowDef="columns"></tr>
          <tr mat-row *matRowDef="let row; columns: columns;"></tr>
        </table>

        <mat-paginator [length]="total" [pageSize]="pageSize" [pageSizeOptions]="[10,20,50]"
          (page)="onPage($event)"></mat-paginator>
      </mat-card-content>
    </mat-card>
  `,
  styles: [`.count{font-weight:300;font-size:.85em;margin-left:8px}table{width:100%}mat-card-header{margin-bottom:16px}`]
})
export class AuditListComponent implements OnInit {
  private readonly api = inject(ApiService);

  columns = ['timestamp', 'operationType', 'entityType', 'entityId', 'personName'];
  records: AuditRecord[] = [];
  total = 0; pageSize = 20; pageIndex = 0;
  sortField = 'timestamp'; sortDir: 'asc' | 'desc' = 'desc';
  filterOp = ''; filterEntity = '';

  ngOnInit() { this.load(); }

  load() {
    const filter: Record<string, unknown> = {};
    if (this.filterOp)     filter['OperationType'] = this.filterOp;
    if (this.filterEntity) filter['EntityType']    = this.filterEntity;

    const spec = {
      from: 'audit-records',
      filter: Object.keys(filter).length ? filter : undefined,
      orderby: [{ field: this.sortField, direction: this.sortDir }],
      page: { from: this.pageIndex * this.pageSize, offset: this.pageSize }
    };

    this.api.query<AuditRecord>('audit', spec).subscribe(resp => {
      this.records = resp.data.map(r => ({ id: r.id, ...r.attributes } as unknown as AuditRecord));
      this.total   = resp.meta?.total ?? 0;
    });
  }

  onFilter() { this.pageIndex = 0; this.load(); }
  onPage(e: PageEvent) { this.pageIndex = e.pageIndex; this.pageSize = e.pageSize; this.load(); }
  onSort(e: Sort) { this.sortField = e.active; this.sortDir = e.direction as 'asc'|'desc' || 'desc'; this.load(); }
  clearFilters() { this.filterOp = ''; this.filterEntity = ''; this.pageIndex = 0; this.load(); }

  opColor(op: string): string {
    return { Create: '#c8e6c9', Update: '#fff9c4', Delete: '#ffcdd2', Read: '#e3f2fd' }[op] ?? '#f5f5f5';
  }
}
