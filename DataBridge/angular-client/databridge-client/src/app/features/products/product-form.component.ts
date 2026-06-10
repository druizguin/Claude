import { Component, Inject, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { ApiService } from '../../core/services/api.service';
import { Product } from '../../core/models/product.model';

@Component({
  selector: 'app-product-form',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule,
    MatDialogModule, MatButtonModule,
    MatInputModule, MatFormFieldModule,
    MatSelectModule, MatSnackBarModule
  ],
  template: `
    <h2 mat-dialog-title>{{ isEdit ? 'Edit' : 'New' }} Product</h2>
    <mat-dialog-content>
      <form [formGroup]="form" style="display:flex;flex-direction:column;gap:12px;padding-top:8px">
        <mat-form-field appearance="outline">
          <mat-label>Name</mat-label>
          <input matInput formControlName="Name" required>
        </mat-form-field>

        <mat-form-field appearance="outline">
          <mat-label>Category</mat-label>
          <mat-select formControlName="Category" required>
            @for (cat of categories; track cat) {
              <mat-option [value]="cat">{{ cat }}</mat-option>
            }
          </mat-select>
        </mat-form-field>

        <mat-form-field appearance="outline">
          <mat-label>Price</mat-label>
          <input matInput type="number" step="0.01" formControlName="Price" required>
        </mat-form-field>

        <mat-form-field appearance="outline">
          <mat-label>Stock Quantity</mat-label>
          <input matInput type="number" formControlName="StockQuantity" required>
        </mat-form-field>

        <mat-form-field appearance="outline">
          <mat-label>Barcode</mat-label>
          <input matInput formControlName="Barcode">
        </mat-form-field>
      </form>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button mat-dialog-close>Cancel</button>
      <button mat-raised-button color="primary" (click)="save()" [disabled]="form.invalid || saving">
        {{ saving ? 'Saving...' : 'Save' }}
      </button>
    </mat-dialog-actions>
  `
})
export class ProductFormComponent {
  private readonly api    = inject(ApiService);
  private readonly snack  = inject(MatSnackBar);
  private readonly ref    = inject(MatDialogRef<ProductFormComponent>);
  private readonly fb     = inject(FormBuilder);

  categories = ['Fruits', 'Vegetables', 'Dairy', 'Bakery', 'Beverages'];
  isEdit     = !!this.data;
  saving     = false;

  form = this.fb.group({
    Name:          [this.data?.name ?? '', Validators.required],
    Category:      [this.data?.category ?? '', Validators.required],
    Price:         [this.data?.price ?? 0, [Validators.required, Validators.min(0)]],
    StockQuantity: [this.data?.stockQuantity ?? 0, Validators.required],
    Barcode:       [this.data?.barcode ?? '']
  });

  constructor(@Inject(MAT_DIALOG_DATA) public data: Product | undefined) {}

  save() {
    if (this.form.invalid) return;
    this.saving = true;
    const attrs = this.form.value as Record<string, unknown>;
    const req   = this.isEdit
      ? this.api.update('products', this.data!.id, attrs)
      : this.api.create('products', attrs);

    req.subscribe({
      next: () => { this.snack.open('Saved!', 'OK', { duration: 2000 }); this.ref.close(true); },
      error: () => { this.snack.open('Save failed', 'OK', { duration: 3000 }); this.saving = false; }
    });
  }
}
