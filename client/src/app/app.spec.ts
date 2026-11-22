import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { provideApiConfiguration } from './api/api-configuration';
import { App } from './app';
import { AuthService } from './auth/auth.service';

describe('App', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [App],
      providers: [
        provideRouter([]),
        provideHttpClient(),
        provideHttpClientTesting(),
        provideApiConfiguration(''),
      ],
    }).compileComponents();
  });

  it('renders the app name', async () => {
    const fixture = TestBed.createComponent(App);
    await fixture.whenStable();
    const header = (fixture.nativeElement as HTMLElement).querySelector('header');
    expect(header?.textContent).toContain('Egészségkalauz');
  });

  it('shows the signed-in user and a logout button', async () => {
    TestBed.inject(AuthService).user.set({
      nanoid: 'abc',
      email: 'teszt.elek@egeszsegkalauz.local',
    });
    const fixture = TestBed.createComponent(App);
    await fixture.whenStable();
    const header = (fixture.nativeElement as HTMLElement).querySelector('header');
    expect(header?.textContent).toContain('teszt.elek@egeszsegkalauz.local');
    expect(header?.textContent).toContain('Kijelentkezés');
  });
});
