import { HttpErrorResponse, HttpStatusCode } from '@angular/common/http';
import { Component, inject, input, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';

import { AuthService } from '../../auth/auth.service';

@Component({
  selector: 'app-login',
  imports: [ReactiveFormsModule],
  templateUrl: './login.html',
})
export class Login {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  readonly returnUrl = input<string>();

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

    this.submitting.set(true);
    this.error.set(null);
    try {
      const { email, password } = this.form.getRawValue();
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
