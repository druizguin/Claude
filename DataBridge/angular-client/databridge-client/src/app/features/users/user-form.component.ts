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
import { User } from '../../core/models/user.model';

@Component({
  selector: 'app-user-form',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule,
    MatDialogModule, MatButtonModule, MatInputModule,
    MatFormFieldModule, MatSelectModule, MatSnackBarModule
  ],
  template: `
    <h2 mat-dialog-title>{{ isEdit ? 'Edit' : 'New' }} User</h2>
    <mat-dialog-content>
      <form [formGroup]="form" style="display:flex;flex-direction:column;gap:12px;padding-top:8px">
        <mat-form-field appearance="outline">
          <mat-label>Name</mat-label>
          <input matInput formControlName="Name" required>
        </mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>Email</mat-label>
          <input matInput type="email" formControlName="Email" required>
        </mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>Age</mat-label>
          <input matInput type="number" formControlName="Age">
        </mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>Country</mat-label>
          <input matInput formControlName="Country">
        </mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>Status</mat-label>
          <mat-select formControlName="Status">
            <mat-option value="active">Active</mat-option>
            <mat-option value="pending">Pending</mat-option>
          </mat-select>
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
export class UserFormComponent {
  private readonly api   = inject(ApiService);
  private readonly snack = inject(MatSnackBar);
  private readonly ref   = inject(MatDialogRef<UserFormComponent>);
  private readonly fb    = inject(FormBuilder);

  isEdit = !!this.data;
  saving = false;

  form = this.fb.group({
    Name:    [this.data?.name    ?? '', Validators.required],
    Email:   [this.data?.email   ?? '', [Validators.required, Validators.email]],
    Age:     [this.data?.age     ?? 18],
    Country: [this.data?.country ?? ''],
    Status:  [this.data?.status  ?? 'active']
  });

  constructor(@Inject(MAT_DIALOG_DATA) public data: User | undefined) {}

  save() {
    if (this.form.invalid) return;
    this.saving = true;
    const attrs = this.form.value as Record<string, unknown>;
    const req   = this.isEdit
      ? this.api.update('users', this.data!.id, attrs)
      : this.api.create('users', attrs);

    req.subscribe({
      next: () => { this.snack.open('Saved!', 'OK', { duration: 2000 }); this.ref.close(true); },
      error: () => { this.snack.open('Save failed', 'OK', { duration: 3000 }); this.saving = false; }
    });
  }
}
