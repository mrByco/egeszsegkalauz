import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { provideApiConfiguration } from '../../api/api-configuration';
import { ImportJobDto, IntegrationDto } from '../../api/models';
import { Browser } from '../../browser/browser';
import { IntegrationCard, POLL_INTERVAL_MS } from './integration-card';

const STATUS_URL = '/api/Integrations/Garmin/daily-import';

function job(overrides: Partial<ImportJobDto>): ImportJobDto {
  return {
    nanoid: 'j1',
    source: 'Garmin',
    status: 'Running',
    totalQueries: 100,
    completedQueries: 25,
    failedQueries: 0,
    createdAt: '2026-11-22T10:00:00Z',
    ...overrides,
  };
}

function garmin(overrides: Partial<IntegrationDto>): IntegrationDto {
  return {
    source: 'Garmin',
    authKind: 'ApiKey',
    capabilities: ['SummaryImport', 'DailyImport'],
    deletesDataOnDisconnect: false,
    connected: true,
    ...overrides,
  };
}

function hevy(overrides: Partial<IntegrationDto>): IntegrationDto {
  return {
    source: 'Hevy',
    authKind: 'OAuth',
    capabilities: ['Sync'],
    deletesDataOnDisconnect: true,
    connected: true,
    accountName: 'teszt_elek',
    ...overrides,
  };
}

describe('IntegrationCard', () => {
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [IntegrationCard],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideApiConfiguration('')],
    });
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => vi.useRealTimers());

  async function render(integration: IntegrationDto) {
    const fixture = TestBed.createComponent(IntegrationCard);
    fixture.componentRef.setInput('integration', integration);
    const changed: IntegrationDto[] = [];
    fixture.componentInstance.changed.subscribe((x) => changed.push(x));
    await fixture.whenStable();
    return { fixture, element: fixture.nativeElement as HTMLElement, changed };
  }

  function click(element: HTMLElement, text: string) {
    [...element.querySelectorAll('button')].find((b) => b.textContent?.includes(text))!.click();
  }

  it('connects with an API key', async () => {
    const { fixture, element, changed } = await render(garmin({ connected: false }));

    const input = element.querySelector<HTMLInputElement>('input[name=apiKey]')!;
    input.value = 'secret-key';
    input.dispatchEvent(new Event('input'));
    await fixture.whenStable();
    element.querySelector('form')!.dispatchEvent(new Event('submit'));

    const request = http.expectOne('/api/Integrations/Garmin');
    expect(request.request.body).toEqual({ apiKey: 'secret-key' });
    request.flush(garmin({ apiKeyHint: '…-key' }));
    await fixture.whenStable();
    http.expectOne(STATUS_URL).flush({ job: null });

    expect(changed[0].connected).toBe(true);
  });

  it('imports summaries and reports what arrived', async () => {
    const { fixture, element } = await render(garmin({}));
    http.expectOne(STATUS_URL).flush({ job: null });

    click(element, 'Összesítők importálása');
    http.expectOne('/api/Integrations/Garmin/summaries/import').flush({
      from: '2022-01-01',
      to: '2026-11-22',
      endpoints: [
        { endpoint: 'activity', records: 52 },
        { endpoint: 'sleep-summaries', records: 377 },
      ],
    });
    await fixture.whenStable();

    const status = element.querySelector('[role=status]')?.textContent;
    expect(status).toContain('429 rekord');
    expect(status).toContain('sleep-summaries: 377');
  });

  it('polls a running daily import until it completes', async () => {
    const { fixture, element } = await render(garmin({}));
    vi.useFakeTimers();
    const settle = async () => {
      await vi.advanceTimersByTimeAsync(0);
      fixture.detectChanges();
    };

    http.expectOne(STATUS_URL).flush({ job: job({}) });
    await settle();
    expect(element.querySelector('[data-testid=daily-progress]')?.textContent).toContain('25 / 100');

    await vi.advanceTimersByTimeAsync(POLL_INTERVAL_MS);
    http.expectOne(STATUS_URL).flush({ job: job({ completedQueries: 100, status: 'Completed' }) });
    await settle();
    expect(element.querySelector('[data-testid=daily-status]')?.textContent).toContain('Kész: 100');

    await vi.advanceTimersByTimeAsync(POLL_INTERVAL_MS * 3);
    http.expectNone(STATUS_URL);
  });

  it('starts the daily import', async () => {
    const { fixture, element } = await render(garmin({}));
    http.expectOne(STATUS_URL).flush({ job: null });

    click(element, 'Napi adatok importálása');
    const request = http.expectOne(STATUS_URL);
    expect(request.request.method).toBe('POST');
    request.flush(job({ completedQueries: 0 }));
    await fixture.whenStable();

    expect(element.textContent).toContain('Leállítás');
  });

  it('sends an OAuth source to the provider to connect', async () => {
    const navigate = vi.spyOn(TestBed.inject(Browser), 'navigate').mockImplementation(() => undefined);
    const { fixture, element } = await render(hevy({ connected: false }));

    click(element, 'Csatlakozás');
    http.expectOne('/api/Integrations/Hevy/oauth/start').flush({ authorizationUrl: 'https://hevy.com/oauth/authorize?x=1' });
    await fixture.whenStable();

    expect(navigate).toHaveBeenCalledWith('https://hevy.com/oauth/authorize?x=1');
    http.expectNone(STATUS_URL);
  });

  it('syncs workouts on demand', async () => {
    const { fixture, element } = await render(hevy({}));

    click(element, 'Szinkronizálás');
    http.expectOne('/api/Integrations/Hevy/sync').flush({
      fullSync: false,
      deleted: 1,
      endpoints: [{ endpoint: 'workout', records: 2 }],
    });
    await fixture.whenStable();

    expect(element.querySelector('[role=status]')?.textContent).toContain('2 rekord frissült, 1 törölve');
  });

  it('asks before disconnecting a source whose data gets deleted', async () => {
    const confirm = vi.spyOn(TestBed.inject(Browser), 'confirm').mockReturnValue(false);
    const { element } = await render(hevy({}));

    click(element, 'Leválasztás');

    expect(confirm).toHaveBeenCalled();
    http.expectNone('/api/Integrations/Hevy');
  });
});
