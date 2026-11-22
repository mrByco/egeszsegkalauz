import { Component, ElementRef, inject, resource, signal, viewChild } from '@angular/core';
import { RouterLink } from '@angular/router';

import { Api } from '../../../api/api';
import { rolesDelete, rolesList } from '../../../api/functions';
import { RoleDto } from '../../../api/models';
import { AuthService } from '../../../auth/auth.service';
import { PERMISSION_LABELS } from '../../../auth/permission-labels';

@Component({
  selector: 'app-roles',
  imports: [RouterLink],
  templateUrl: './roles.html',
})
export class Roles {
  private readonly api = inject(Api);
  protected readonly auth = inject(AuthService);
  protected readonly permissionLabels = PERMISSION_LABELS;

  private readonly dialog = viewChild.required<ElementRef<HTMLDialogElement>>('deleteDialog');

  protected readonly roles = resource({ loader: () => this.api.invoke(rolesList) });
  protected readonly deleting = signal<RoleDto | null>(null);
  protected readonly error = signal<string | null>(null);

  protected confirmDelete(role: RoleDto): void {
    this.deleting.set(role);
    this.error.set(null);
    this.dialog().nativeElement.showModal();
  }

  protected async delete(): Promise<void> {
    const role = this.deleting();
    if (!role) {
      return;
    }

    try {
      await this.api.invoke(rolesDelete, { nanoid: role.nanoid });
      this.roles.update((roles) => roles?.filter((r) => r.nanoid !== role.nanoid));
      this.dialog().nativeElement.close();
    } catch {
      this.error.set('A törlés nem sikerült. Próbáld újra később.');
    }
  }
}
