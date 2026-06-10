import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { ApiService } from '../../core/services/api.service';
import { Product } from '../../core/models/product.model';
import { User } from '../../core/models/user.model';

@Component({
  selector: 'app-purchase-form',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule,
    MatDialogModule, MatButtonModule, MatInputModule,
    MatFormFieldModule, MatSelectModule, MatSnackBarModule
  ],
  template: `
    <h2 mat-dialog-title>New Purchase</h2>
    <mat-dialog-content>
      <form [formGroup]="form" style="display:flex;flex-direction:column;gap:12px;padding-top:8px">
        <mat-form-field appearance="outline">
          <mat-label>User</mat-label>
          <mat-select formControlName="UserId" required>
            @for (u of users; track u.id) {
              <mat-option [value]="u.id">{{ u.name }}</mat-option>
            }
          </mat-select>
        </mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>Product</mat-label>
          <mat-select formControlName="ProductId" required (ngModelChange)="onProductChange()">
            @for (p of products; track p.id) {
              <mat-option [value]="p.id">{{ p.name }} ({{ p.price | currency }})</mat-option>
            }
          </mat-select>
        </mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>Quantity</mat-label>
          <input matInput type="number" min="1" formControlName="Quantity" (input)="calcTotal()">
        </mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>Total Price</mat-label>
          <input matInput type="number" formControlName="TotalPrice" readonly>
        </mat-form-field>
      </form>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button mat-dialog-close>Cancel</button>
      <button mat-raised-button color="primary" (click)="save()" [disabled]="form.invalid || saving">
        {{ saving ? 'Saving…' : 'Save' }}
      </button>
    </mat-dialog-actions>
  `
})
export class PurchaseFormComponent implements OnInit {
  private readonly api   = inject(ApiService);
  private readonly snack = inject(MatSnackBar);
  private readonly ref   = inject(MatDialogRef<PurchaseFormComponent>);
  private readonly fb    = inject(FormBuilder);

  users: User[] = [];
  products: Product[] = [];
  saving = false;

  form = this.fb.group({
    UserId:     ['', Validators.required],
    ProductId:  ['', Validators.required],
    Quantity:   [1, [Validators.required, Validators.min(1)]],
    TotalPrice: [{ value: 0, disabled: true }]
  });

  ngOnInit() {
    this.api.getCollection<User>('users', 0, 100).subscribe(r =>
      this.users = r.data.map(d => ({ id: d.id, ...d.attributes } as unknown as User)));
    this.api.getCollection<Product>('products', 0, 100).subscribe(r =>
      this.products = r.data.map(d => ({ id: d.id, ...d.attributes } as unknown as Product)));
  }

  calcTotal() {
    const pid  = this.form.get('ProductId')?.value;
    const qty  = this.form.get('Quantity')?.value ?? 1;
    const prod = this.products.find(p => p.id === pid);
    if (prod) this.form.get('TotalPrice')?.setValue(prod.price * qty);
  }

  onProductChange() { this.calcTotal(); }

  save() {
    if (this.form.invalid) return;
    this.saving = true;
    const raw   = this.form.getRawValue();
    this.api.create('purchases', { ...raw, Status: 'completed' }).subscribe({
      next: () => { this.snack.open('Saved!', 'OK', { duration: 2000 }); this.ref.close(true); },
      error: () => { this.snack.open('Save failed', 'OK', { duration: 3000 }); this.saving = false; }
    });
  }
}
