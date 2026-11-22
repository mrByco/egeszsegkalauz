import { Injectable, inject, signal } from '@angular/core';
import { Router } from '@angular/router';

import { Api } from '../api/api';
import { authLogin, authLogout, authMe } from '../api/functions';
import { CurrentUserDto } from '../api/models';

export const LOGIN_PATH = '/bejelentkezes';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly api = inject(Api);
  private readonly router = inject(Router);

  readonly user = signal<CurrentUserDto | null>(null);

  async loadCurrentUser(): Promise<void> {
    try {
      this.user.set(await this.api.invoke(authMe));
    } catch {
      this.user.set(null);
    }
  }

  async login(email: string, password: string): Promise<void> {
    this.user.set(await this.api.invoke(authLogin, { body: { email, password } }));
  }

  async logout(): Promise<void> {
    await this.api.invoke(authLogout);
    this.user.set(null);
    await this.router.navigateByUrl(LOGIN_PATH);
  }

  async sessionExpired(): Promise<void> {
    this.user.set(null);
    await this.router.navigate([LOGIN_PATH], { queryParams: { returnUrl: this.router.url } });
  }
}
