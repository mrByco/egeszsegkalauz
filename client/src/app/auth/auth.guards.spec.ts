import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ActivatedRouteSnapshot, RouterStateSnapshot, UrlTree, provideRouter } from '@angular/router';

import { provideApiConfiguration } from '../api/api-configuration';
import { permissionGuard } from './auth.guards';
import { AuthService } from './auth.service';

describe('permissionGuard', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        provideHttpClient(),
        provideHttpClientTesting(),
        provideApiConfiguration(''),
      ],
    });
  });

  function run() {
    return TestBed.runInInjectionContext(() =>
      permissionGuard('RolesEdit')({} as ActivatedRouteSnapshot, {} as RouterStateSnapshot),
    );
  }

  it('lets users with the permission through', () => {
    TestBed.inject(AuthService).user.set({ nanoid: 'a', email: 'a@b.hu', permissions: ['RolesEdit'] });
    expect(run()).toBe(true);
  });

  it('sends users without the permission home', () => {
    TestBed.inject(AuthService).user.set({ nanoid: 'a', email: 'a@b.hu', permissions: ['RolesView'] });
    const result = run();
    expect(result).toBeInstanceOf(UrlTree);
    expect((result as UrlTree).toString()).toBe('/');
  });
});
