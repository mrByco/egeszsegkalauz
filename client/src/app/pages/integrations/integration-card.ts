import { DatePipe } from '@angular/common';
import { HttpErrorResponse, HttpStatusCode } from '@angular/common/http';
import { Component, OnDestroy, OnInit, computed, inject, input, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

import { Api } from '../../api/api';
import {
  integrationsCancelDailyImport,
  integrationsConnect,
  integrationsDailyImportStatus,
  integrationsDisconnect,
  integrationsImportSummaries,
  integrationsStartDailyImport,
  integrationsStartOAuth,
  integrationsSync,
} from '../../api/functions';
import {
  ImportJobDto,
  IntegrationAuthKind,
  IntegrationCapability,
  IntegrationDto,
  SummaryImportResultDto,
  SyncResultDto,
  EndpointRecordCountDto,
} from '../../api/models';
import { Browser } from '../../browser/browser';
import { SOURCE_LABELS } from '../../integrations/source-labels';

export const POLL_INTERVAL_MS = 2000;

@Component({
  selector: 'app-integration-card',
  imports: [DatePipe, FormsModule],
  templateUrl: './integration-card.html',
})
export class IntegrationCard implements OnInit, OnDestroy {
  private readonly api = inject(Api);
  private readonly browser = inject(Browser);
  private pollTimer: ReturnType<typeof setTimeout> | undefined;

  readonly integration = input.required<IntegrationDto>();
  readonly changed = output<IntegrationDto>();

  protected readonly sourceLabels = SOURCE_LABELS;

  protected readonly apiKey = signal('');
  protected readonly busy = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly summaryResult = signal<SummaryImportResultDto | null>(null);
  protected readonly syncResult = signal<SyncResultDto | null>(null);
  protected readonly job = signal<ImportJobDto | null>(null);
  protected readonly jobRunning = computed(() => this.job()?.status === 'Running');
  protected readonly progress = computed(() => {
    const job = this.job();
    return job && job.totalQueries > 0
      ? Math.floor((100 * (job.completedQueries + job.failedQueries)) / job.totalQueries)
      : 0;
  });

  ngOnInit(): void {
    if (this.integration().connected && this.can('DailyImport')) {
      void this.refreshJob();
    }
  }

  protected total(endpoints: EndpointRecordCountDto[]): number {
    return endpoints.reduce((sum, endpoint) => sum + endpoint.records, 0);
  }

  protected can(capability: IntegrationCapability): boolean {
    return this.integration().capabilities.includes(capability);
  }

  protected async startOAuth(): Promise<void> {
    await this.run(async () => {
      const { authorizationUrl } = await this.api.invoke(integrationsStartOAuth, {
        source: this.integration().source,
      });
      this.browser.navigate(authorizationUrl);
    });
  }

  protected async sync(): Promise<void> {
    await this.run(async () => {
      this.syncResult.set(
        await this.api.invoke(integrationsSync, { source: this.integration().source }),
      );
      this.changed.emit({
        ...this.integration(),
        lastSyncAt: new Date().toISOString(),
        lastSyncError: null,
      });
    });
  }

  ngOnDestroy(): void {
    clearTimeout(this.pollTimer);
  }

  protected async connect(): Promise<void> {
    const apiKey = this.apiKey().trim();
    if (!apiKey) {
      return;
    }

    await this.run(async () => {
      const updated = await this.api.invoke(integrationsConnect, {
        source: this.integration().source,
        body: { apiKey },
      });
      this.apiKey.set('');
      this.changed.emit(updated);
      await this.refreshJob();
    });
  }

  protected async disconnect(): Promise<void> {
    const integration = this.integration();
    if (
      integration.deletesDataOnDisconnect &&
      !this.browser.confirm(
        `A leválasztás törli a(z) ${SOURCE_LABELS[integration.source]} szolgáltatásból szinkronizált összes adatot. Folytatod?`,
      )
    ) {
      return;
    }

    await this.run(async () => {
      await this.api.invoke(integrationsDisconnect, { source: integration.source });
      clearTimeout(this.pollTimer);
      this.summaryResult.set(null);
      this.syncResult.set(null);
      this.job.set(null);
      this.changed.emit({
        ...integration,
        connected: false,
        apiKeyHint: null,
        accountName: null,
        lastSyncAt: null,
        lastSyncError: null,
      });
    });
  }

  protected async importSummaries(): Promise<void> {
    await this.run(async () => {
      const result = await this.api.invoke(integrationsImportSummaries, {
        source: this.integration().source,
      });
      this.summaryResult.set(result);
      this.changed.emit({
        ...this.integration(),
        firstDataDate: result.from ?? this.integration().firstDataDate,
        summariesImportedAt: new Date().toISOString(),
      });
    });
  }

  protected async startDailyImport(): Promise<void> {
    await this.run(async () => {
      this.job.set(
        await this.api.invoke(integrationsStartDailyImport, { source: this.integration().source }),
      );
      this.schedulePoll();
    });
  }

  protected async cancelDailyImport(): Promise<void> {
    await this.run(async () => {
      await this.api.invoke(integrationsCancelDailyImport, { source: this.integration().source });
      await this.refreshJob();
    });
  }

  private async refreshJob(): Promise<void> {
    clearTimeout(this.pollTimer);
    try {
      const status = await this.api.invoke(integrationsDailyImportStatus, {
        source: this.integration().source,
      });
      this.job.set(status.job ?? null);
    } catch {
      // A failed poll keeps the last known state; the next poll tries again.
    }
    this.schedulePoll();
  }

  private schedulePoll(): void {
    clearTimeout(this.pollTimer);
    if (this.jobRunning()) {
      this.pollTimer = setTimeout(() => void this.refreshJob(), POLL_INTERVAL_MS);
    }
  }

  private async run(action: () => Promise<void>): Promise<void> {
    this.busy.set(true);
    this.error.set(null);
    try {
      await action();
    } catch (error) {
      this.error.set(errorMessage(error, this.integration().authKind));
    } finally {
      this.busy.set(false);
    }
  }
}

function errorMessage(error: unknown, authKind: IntegrationAuthKind): string {
  if (error instanceof HttpErrorResponse) {
    switch (error.status) {
      case HttpStatusCode.BadRequest:
        return authKind === 'ApiKey'
          ? 'A szolgáltatás nem fogadta el az API kulcsot.'
          : 'A szolgáltatás nem fogadta el a hozzáférést. Csatlakoztasd újra.';
      case HttpStatusCode.BadGateway:
        return 'A szolgáltatás most nem érhető el. Próbáld újra később.';
    }
  }
  return 'A művelet nem sikerült. Próbáld újra később.';
}
