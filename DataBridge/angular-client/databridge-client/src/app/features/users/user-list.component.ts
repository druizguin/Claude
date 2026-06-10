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
import { User } from '../../core/models/user.model';
import { UserFormComponent } from './user-form.component';

@Component({
  selector: 'app-user-list',
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
        <mat-card-title>Users <span class="count">({{ total }})</span></mat-card-title>
        <span class="toolbar-spacer"></span>
        <button mat-raised-button color="primary" (click)="openForm()">
          <mat-icon>person_add</mat-icon> New User
        </button>
      </mat-card-header>
      <mat-card-content>
        <div class="filter-row">
          <mat-form-field appearance="outline">
            <mat-label>Search name/email</mat-label>
            <input matInput [(ngModel)]="filterSearch" (ngModelChange)="onFilter()">
          </mat-form-field>
          <mat-form-field appearance="outline">
            <mat-label>Country</mat-label>
            <input matInput [(ngModel)]="filterCountry" (ngModelChange)="onFilter()">
          </mat-form-field>
          <mat-form-field appearance="outline">
            <mat-label>Status</mat-label>
            <mat-select [(ngModel)]="filterStatus" (ngModelChange)="onFilter()">
              <mat-option value="">All</mat-option>
              <mat-option value="active">Active</mat-option>
              <mat-option value="pending">Pending</mat-option>
            </mat-select>
          </mat-form-field>
          <button mat-stroked-button (click)="clearFilters()"><mat-icon>clear</mat-icon></button>
        </div>

        <table mat-table [dataSource]="users" matSort (matSortChange)="onSort($event)">
          <ng-container matColumnDef="name">
            <th mat-header-cell *matHeaderCellDef mat-sort-header>Name</th>
            <td mat-cell *matCellDef="let u">{{ u.name }}</td>
          </ng-container>
          <ng-container matColumnDef="email">
            <th mat-header-cell *matHeaderCellDef>Email</th>
            <td mat-cell *matCellDef="let u">{{ u.email }}</td>
          </ng-container>
          <ng-container matColumnDef="age">
            <th mat-header-cell *matHeaderCellDef mat-sort-header>Age</th>
            <td mat-cell *matCellDef="let u">{{ u.age }}</td>
          </ng-container>
          <ng-container matColumnDef="country">
            <th mat-header-cell *matHeaderCellDef mat-sort-header>Country</th>
            <td mat-cell *matCellDef="let u">{{ u.country }}</td>
          </ng-container>
          <ng-container matColumnDef="status">
            <th mat-header-cell *matHeaderCellDef>Status</th>
            <td mat-cell *matCellDef="let u">
              <mat-chip [class]="'chip-' + u.status">{{ u.status }}</mat-chip>
            </td>
          </ng-container>
          <ng-container matColumnDef="address">
            <th mat-header-cell *matHeaderCellDef>Address (cross-source)</th>
            <td mat-cell *matCellDef="let u">
              {{ u.addressPrincipal ? (u.addressPrincipal.street + ', ' + u.addressPrincipal.city) : '—' }}
            </td>
          </ng-container>
          <ng-container matColumnDef="actions">
            <th mat-header-cell *matHeaderCellDef>Actions</th>
            <td mat-cell *matCellDef="let u" class="actions-cell">
              <button mat-icon-button color="primary" (click)="openForm(u)"><mat-icon>edit</mat-icon></button>
              <button mat-icon-button color="warn" (click)="delete(u)"><mat-icon>delete</mat-icon></button>
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
  styles: [`
    .count { font-weight:300; font-size:.85em; margin-left:8px; }
    table { width:100%; }
    mat-card-header { display:flex; align-items:center; margin-bottom:16px; }
  `]
})
export class UserListComponent implements OnInit {
  private readonly api    = inject(ApiService);
  private readonly dialog = inject(MatDialog);
  private readonly snack  = inject(MatSnackBar);

  columns = ['name', 'email', 'age', 'country', 'status', 'address', 'actions'];
  users: User[] = [];
  total = 0; pageSize = 10; pageIndex = 0;
  sortField = 'name'; sortDir: 'asc' | 'desc' = 'asc';
  filterSearch = ''; filterCountry = ''; filterStatus = '';

  ngOnInit() { this.load(); }

  load() {
    const filter: Record<string, unknown> = {};
    if (this.filterSearch) filter['or'] = [
      { name: { like: `%${this.filterSearch}%` } },
      { email: { like: `%${this.filterSearch}%` } }
    ];
    if (this.filterCountry) filter['country'] = this.filterCountry;
    if (this.filterStatus)  filter['status']  = this.filterStatus;

    const spec = {
      from: 'users',
      select: ['name', 'email', 'age', 'country', 'status', 'signupDate',
               'addressPrincipalId', 'AddressPrincipal.street', 'AddressPrincipal.city'],
      filter: Object.keys(filter).length ? filter : undefined,
      orderby: [{ field: this.sortField, direction: this.sortDir }],
      page: { from: this.pageIndex * this.pageSize, offset: this.pageSize }
    };

    this.api.query<User>('users', spec).subscribe(resp => {
      this.users = resp.data.map(r => ({ id: r.id, ...r.attributes } as unknown as User));
      this.total = resp.meta?.total ?? 0;
    });
  }

  onFilter() { this.pageIndex = 0; this.load(); }
  onPage(e: PageEvent) { this.pageIndex = e.pageIndex; this.pageSize = e.pageSize; this.load(); }
  onSort(e: Sort) { this.sortField = e.active; this.sortDir = e.direction as 'asc'|'desc' || 'asc'; this.load(); }
  clearFilters() { this.filterSearch = ''; this.filterCountry = ''; this.filterStatus = ''; this.pageIndex = 0; this.load(); }

  openForm(user?: User) {
    const ref = this.dialog.open(UserFormComponent, { data: user, width: '480px' });
    ref.afterClosed().subscribe(r => { if (r) this.load(); });
  }

  delete(u: User) {
    if (!confirm(`Delete user "${u.name}"?`)) return;
    this.api.delete('users', u.id).subscribe({
      next: () => { this.snack.open('Deleted', 'OK', { duration: 2000 }); this.load(); },
      error: () => this.snack.open('Delete failed', 'OK', { duration: 3000 })
    });
  }
}
