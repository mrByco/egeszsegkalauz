import { DatePipe } from '@angular/common';
import { HttpErrorResponse, HttpStatusCode } from '@angular/common/http';
import { Component, ElementRef, inject, resource, signal, viewChild } from '@angular/core';

import { Api } from '../../../api/api';
import { rolesList, usersList, usersSetRoles } from '../../../api/functions';
import { RoleSummaryDto, UserDto } from '../../../api/models';
import { AuthService } from '../../../auth/auth.service';

@Component({
  selector: 'app-users',
  imports: [DatePipe],
  templateUrl: './users.html',
})
export class Users {
  private readonly api = inject(Api);
  protected readonly auth = inject(AuthService);

  private readonly dialog = viewChild.required<ElementRef<HTMLDialogElement>>('rolesDialog');

  protected readonly users = resource({ loader: () => this.api.invoke(usersList) });
  protected readonly roles = resource({
    params: () => this.canAssignRoles() || undefined,
    loader: () => this.api.invoke(rolesList),
  });

  protected readonly editing = signal<UserDto | null>(null);
  protected readonly selected = signal<ReadonlySet<string>>(new Set());
  protected readonly saving = signal(false);
  protected readonly error = signal<string | null>(null);

  protected canAssignRoles(): boolean {
    return this.auth.can('UsersAssignRoles') && this.auth.can('RolesView');
  }

  protected openRoles(user: UserDto): void {
    this.editing.set(user);
    this.selected.set(new Set(user.roles.map((role: RoleSummaryDto) => role.nanoid)));
    this.error.set(null);
    this.dialog().nativeElement.showModal();
  }

  protected toggle(roleNanoid: string, checked: boolean): void {
    const next = new Set(this.selected());
    if (checked) {
      next.add(roleNanoid);
    } else {
      next.delete(roleNanoid);
    }
    this.selected.set(next);
  }

  protected async saveRoles(): Promise<void> {
    const user = this.editing();
    if (!user) {
      return;
    }

    this.saving.set(true);
    this.error.set(null);
    try {
      const updated = await this.api.invoke(usersSetRoles, {
        nanoid: user.nanoid,
        body: { roleNanoids: [...this.selected()] },
      });
      this.users.update((users) => users?.map((u) => (u.nanoid === updated.nanoid ? updated : u)));
      if (updated.nanoid === this.auth.user()?.nanoid) {
        await this.auth.loadCurrentUser();
      }
      this.dialog().nativeElement.close();
    } catch (error) {
      this.error.set(
        error instanceof HttpErrorResponse && error.status === HttpStatusCode.Conflict
          ? 'Legalább egy felhasználónak meg kell tartania az Admin szerepkört.'
          : 'A mentés nem sikerült. Próbáld újra később.',
      );
    } finally {
      this.saving.set(false);
    }
  }
}
