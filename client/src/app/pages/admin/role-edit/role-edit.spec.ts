import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';

import { provideApiConfiguration } from '../../../api/api-configuration';
import { RoleEdit } from './role-edit';

describe('RoleEdit', () => {
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [RoleEdit],
      providers: [
        provideRouter([]),
        provideHttpClient(),
        provideHttpClientTesting(),
        provideApiConfiguration(''),
      ],
    });
    http = TestBed.inject(HttpTestingController);
  });

  async function fillAndSubmit(element: HTMLElement, name: string, permissions: string[]) {
    const input = element.querySelector<HTMLInputElement>('#name')!;
    input.value = name;
    input.dispatchEvent(new Event('input'));
    for (const permission of permissions) {
      const checkbox = element.querySelector<HTMLInputElement>(`[data-permission=${permission}]`)!;
      checkbox.checked = true;
      checkbox.dispatchEvent(new Event('change'));
    }
    element.querySelector('form')!.dispatchEvent(new Event('submit'));
  }

  it('lists every permission from the API enum', async () => {
    const fixture = TestBed.createComponent(RoleEdit);
    await fixture.whenStable();

    const element = fixture.nativeElement as HTMLElement;
    expect(element.querySelectorAll('[data-permission]').length).toBe(4);
    expect(element.textContent).toContain('Szerepkörök kiosztása felhasználóknak');
  });

  it('creates a role with the checked permissions', async () => {
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigateByUrl').mockResolvedValue(true);
    const fixture = TestBed.createComponent(RoleEdit);
    await fixture.whenStable();

    await fillAndSubmit(fixture.nativeElement, 'Szerkesztő', ['RolesView', 'RolesEdit']);

    const request = http.expectOne('/api/Roles');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({ name: 'Szerkesztő', permissions: ['RolesView', 'RolesEdit'] });
    request.flush({ nanoid: 'r1', name: 'Szerkesztő', isSystem: false, permissions: [], userCount: 0 });
    await fixture.whenStable();

    expect(navigate).toHaveBeenCalledWith('/admin/szerepkorok');
  });

  it('loads and updates an existing role', async () => {
    const fixture = TestBed.createComponent(RoleEdit);
    fixture.componentRef.setInput('nanoid', 'r1');
    TestBed.tick();
    http.expectOne('/api/Roles/r1').flush({
      nanoid: 'r1',
      name: 'Megtekintő',
      isSystem: false,
      permissions: ['UsersView'],
      userCount: 1,
    });
    await fixture.whenStable();

    const element = fixture.nativeElement as HTMLElement;
    expect(element.querySelector<HTMLInputElement>('#name')!.value).toBe('Megtekintő');
    expect(element.querySelector<HTMLInputElement>('[data-permission=UsersView]')!.checked).toBe(true);

    await fillAndSubmit(element, 'Olvasó', ['RolesView']);
    const request = http.expectOne('/api/Roles/r1');
    expect(request.request.method).toBe('PUT');
    expect(request.request.body).toEqual({ name: 'Olvasó', permissions: ['UsersView', 'RolesView'] });
  });

  it('shows an error for a taken name', async () => {
    const fixture = TestBed.createComponent(RoleEdit);
    await fixture.whenStable();
    const element = fixture.nativeElement as HTMLElement;

    await fillAndSubmit(element, 'Admin', []);
    http.expectOne('/api/Roles').flush(null, { status: 409, statusText: 'Conflict' });
    await fixture.whenStable();

    expect(element.querySelector('[role=alert]')?.textContent).toContain(
      'Ilyen nevű szerepkör már létezik.',
    );
  });
});
