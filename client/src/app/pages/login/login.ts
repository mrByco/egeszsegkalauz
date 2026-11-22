import { HttpErrorResponse, HttpStatusCode } from '@angular/common/http';
import { Component, inject, input, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';

import { AuthService, REGISTER_PATH } from '../../auth/auth.service';

// Production builds define ngDevMode as false, which drops the dev credentials from the bundle.
const QUICK_LOGINS =
  typeof ngDevMode === 'undefined' || ngDevMode
    ? [
        { label: 'Admin', email: 'admin@egeszsegkalauz.local', password: 'Jelszo123!' },
        {
          label: 'Teszt Elek (Megtekintő)',
          email: 'teszt.elek@egeszsegkalauz.local',
          password: 'Jelszo123!',
        },
      ]
    : [];

@Component({
  selector: 'app-login',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './login.html',
})
export class Login {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  readonly returnUrl = input<string>();

  protected readonly registerPath = REGISTER_PATH;
  protected readonly quickLogins = ['localhost', '127.0.0.1'].includes(location.hostname)
    ? QUICK_LOGINS
    : [];

  protected readonly form = inject(NonNullableFormBuilder).group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', Validators.required],
  });
  protected readonly submitting = signal(false);
  protected readonly error = signal<string | null>(null);

  protected async submit(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const { email, password } = this.form.getRawValue();
    await this.signIn(email, password);
  }

  protected async quickLogin(email: string, password: string): Promise<void> {
    await this.signIn(email, password);
  }

  private async signIn(email: string, password: string): Promise<void> {
    this.submitting.set(true);
    this.error.set(null);
    try {
      await this.auth.login(email, password);
      await this.router.navigateByUrl(this.returnUrl() ?? '/');
    } catch (error) {
      this.error.set(
        error instanceof HttpErrorResponse && error.status === HttpStatusCode.Unauthorized
          ? 'Hibás e-mail cím vagy jelszó.'
          : 'A bejelentkezés nem sikerült. Próbáld újra később.',
      );
    } finally {
      this.submitting.set(false);
    }
  }
}
