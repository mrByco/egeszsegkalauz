import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';

import { provideApiConfiguration } from '../../api/api-configuration';
import { AuthService } from '../../auth/auth.service';
import { Register } from './register';

const EMAIL = 'uj.felhasznalo@egeszsegkalauz.local';

describe('Register', () => {
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [Register],
      providers: [
        provideRouter([]),
        provideHttpClient(),
        provideHttpClientTesting(),
        provideApiConfiguration(''),
      ],
    });
    http = TestBed.inject(HttpTestingController);
  });

  async function submit(email: string, password: string, passwordConfirmation = password) {
    const fixture = TestBed.createComponent(Register);
    const element = fixture.nativeElement as HTMLElement;
    await fixture.whenStable();
    for (const [id, value] of [
      ['email', email],
      ['password', password],
      ['passwordConfirmation', passwordConfirmation],
    ]) {
      const input = element.querySelector<HTMLInputElement>(`#${id}`)!;
      input.value = value;
      input.dispatchEvent(new Event('input'));
    }
    element.querySelector('form')!.dispatchEvent(new Event('submit'));
    return { fixture, element };
  }

  it('registers, signs in and navigates home', async () => {
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigateByUrl').mockResolvedValue(true);
    const { fixture } = await submit(EMAIL, 'UjJelszo123');

    const request = http.expectOne('/api/Auth/register');
    expect(request.request.body).toEqual({ email: EMAIL, password: 'UjJelszo123' });
    request.flush({ nanoid: 'abc', email: EMAIL, permissions: [] });
    await fixture.whenStable();

    expect(TestBed.inject(AuthService).user()?.email).toBe(EMAIL);
    expect(navigate).toHaveBeenCalledWith('/');
  });

  it('shows an error when the email is taken', async () => {
    const { fixture, element } = await submit(EMAIL, 'UjJelszo123');

    http.expectOne('/api/Auth/register').flush(null, { status: 409, statusText: 'Conflict' });
    await fixture.whenStable();

    expect(element.querySelector('[role=alert]')?.textContent).toContain(
      'Ezzel az e-mail címmel már regisztráltak.',
    );
  });

  it('rejects a short password', async () => {
    const { fixture, element } = await submit(EMAIL, 'rovid');
    await fixture.whenStable();

    http.expectNone('/api/Auth/register');
    expect(element.textContent).toContain('A jelszó legalább 8 karakter legyen.');
  });

  it('rejects mismatching passwords', async () => {
    const { fixture, element } = await submit(EMAIL, 'UjJelszo123', 'MasJelszo123');
    await fixture.whenStable();

    http.expectNone('/api/Auth/register');
    expect(element.textContent).toContain('A két jelszó nem egyezik.');
  });
});
