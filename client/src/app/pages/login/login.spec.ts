import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';

import { provideApiConfiguration } from '../../api/api-configuration';
import { AuthService } from '../../auth/auth.service';
import { Login } from './login';

describe('Login', () => {
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [Login],
      providers: [
        provideRouter([]),
        provideHttpClient(),
        provideHttpClientTesting(),
        provideApiConfiguration(''),
      ],
    });
    http = TestBed.inject(HttpTestingController);
  });

  async function submit(email: string, password: string) {
    const fixture = TestBed.createComponent(Login);
    const element = fixture.nativeElement as HTMLElement;
    await fixture.whenStable();
    for (const [id, value] of [
      ['email', email],
      ['password', password],
    ]) {
      const input = element.querySelector<HTMLInputElement>(`#${id}`)!;
      input.value = value;
      input.dispatchEvent(new Event('input'));
    }
    element.querySelector('form')!.dispatchEvent(new Event('submit'));
    return { fixture, element };
  }

  it('signs in and navigates home', async () => {
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigateByUrl').mockResolvedValue(true);
    const { fixture } = await submit('teszt.elek@egeszsegkalauz.local', 'Jelszo123!');

    const request = http.expectOne('/api/Auth/login');
    expect(request.request.body).toEqual({
      email: 'teszt.elek@egeszsegkalauz.local',
      password: 'Jelszo123!',
    });
    request.flush({ nanoid: 'abc', email: 'teszt.elek@egeszsegkalauz.local' });
    await fixture.whenStable();

    expect(TestBed.inject(AuthService).user()?.email).toBe('teszt.elek@egeszsegkalauz.local');
    expect(navigate).toHaveBeenCalledWith('/');
  });

  it('shows an error for wrong credentials', async () => {
    const { fixture, element } = await submit('teszt.elek@egeszsegkalauz.local', 'rossz');

    http.expectOne('/api/Auth/login').flush(null, { status: 401, statusText: 'Unauthorized' });
    await fixture.whenStable();

    expect(element.querySelector('[role=alert]')?.textContent).toContain(
      'Hibás e-mail cím vagy jelszó.',
    );
  });

  it('does not call the API with an invalid form', async () => {
    await submit('nem-email', '');

    http.expectNone('/api/Auth/login');
  });
});
