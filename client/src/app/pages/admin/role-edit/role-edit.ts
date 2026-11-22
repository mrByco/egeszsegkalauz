import { HttpErrorResponse, HttpStatusCode } from '@angular/common/http';
import { Component, effect, inject, input, resource, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';

import { Api } from '../../../api/api';
import { rolesCreate, rolesGet, rolesUpdate } from '../../../api/functions';
import { Permission } from '../../../api/models';
import { PERMISSION } from '../../../api/models/permission-array';
import { PERMISSION_LABELS } from '../../../auth/permission-labels';

const ROLES_PATH = '/admin/szerepkorok';

@Component({
  selector: 'app-role-edit',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './role-edit.html',
})
export class RoleEdit {
  private readonly api = inject(Api);
  private readonly router = inject(Router);

  readonly nanoid = input<string>();

  protected readonly rolesPath = ROLES_PATH;
  protected readonly permissions = PERMISSION.map((permission) => ({
    permission,
    label: PERMISSION_LABELS[permission],
  }));

  protected readonly role = resource({
    params: () => this.nanoid(),
    loader: ({ params: nanoid }) => this.api.invoke(rolesGet, { nanoid }),
  });

  protected readonly form = inject(NonNullableFormBuilder).group({
    name: ['', [Validators.required, Validators.maxLength(100)]],
    permissions: [[] as Permission[]],
  });
  protected readonly submitting = signal(false);
  protected readonly error = signal<string | null>(null);

  constructor() {
    effect(() => {
      const role = this.role.value();
      if (role) {
        this.form.setValue({ name: role.name, permissions: role.permissions });
      }
    });
  }

  protected hasPermission(permission: Permission): boolean {
    return this.form.controls.permissions.value.includes(permission);
  }

  protected toggle(permission: Permission, checked: boolean): void {
    const current = this.form.controls.permissions.value.filter((p) => p !== permission);
    this.form.controls.permissions.setValue(checked ? [...current, permission] : current);
  }

  protected async submit(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting.set(true);
    this.error.set(null);
    try {
      const body = this.form.getRawValue();
      const nanoid = this.nanoid();
      await (nanoid
        ? this.api.invoke(rolesUpdate, { nanoid, body })
        : this.api.invoke(rolesCreate, { body }));
      await this.router.navigateByUrl(ROLES_PATH);
    } catch (error) {
      this.error.set(
        error instanceof HttpErrorResponse && error.status === HttpStatusCode.Conflict
          ? 'Ilyen nevű szerepkör már létezik.'
          : 'A mentés nem sikerült. Próbáld újra később.',
      );
    } finally {
      this.submitting.set(false);
    }
  }
}
