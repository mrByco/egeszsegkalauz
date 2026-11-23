import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { provideApiConfiguration } from '../../api/api-configuration';
import { OAuthCallback } from './oauth-callback';

describe('OAuthCallback', () => {
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [OAuthCallback],
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting(), provideApiConfiguration('')],
    });
    http = TestBed.inject(HttpTestingController);
  });

  async function settle(fixture: { detectChanges(): void }) {
    await new Promise((resolve) => setTimeout(resolve));
    fixture.detectChanges();
  }

  async function render(inputs: Record<string, string>) {
    const fixture = TestBed.createComponent(OAuthCallback);
    for (const [name, value] of Object.entries(inputs)) {
      fixture.componentRef.setInput(name, value);
    }
    fixture.detectChanges();
    await Promise.resolve();
    return { fixture, element: fixture.nativeElement as HTMLElement };
  }

  it('completes the connection and runs the first sync', async () => {
    const { fixture, element } = await render({ source: 'hevy', code: 'c1', state: 's1' });

    const complete = http.expectOne('/api/Integrations/Hevy/oauth/complete');
    expect(complete.request.body).toEqual({ code: 'c1', state: 's1' });
    complete.flush({});
    await settle(fixture);
    http.expectOne('/api/Integrations/Hevy/sync').flush({
      fullSync: true,
      deleted: 0,
      endpoints: [
        { endpoint: 'exercise-template', records: 420 },
        { endpoint: 'workout', records: 478 },
      ],
    });
    await settle(fixture);

    expect(element.querySelector('[role=status]')?.textContent).toContain('898 rekord');
  });

  it('explains a declined authorization without calling the API', async () => {
    const { fixture, element } = await render({ source: 'hevy', error: 'access_denied' });
    await settle(fixture);

    expect(element.querySelector('[role=alert]')?.textContent).toContain('elutasítottad');
    http.expectNone('/api/Integrations/Hevy/oauth/complete');
  });

  it('reports an account that is already connected elsewhere', async () => {
    const { fixture, element } = await render({ source: 'hevy', code: 'c1', state: 's1' });

    http.expectOne('/api/Integrations/Hevy/oauth/complete').flush(null, { status: 409, statusText: 'Conflict' });
    await settle(fixture);

    expect(element.querySelector('[role=alert]')?.textContent).toContain('másik felhasználóhoz');
  });
});
