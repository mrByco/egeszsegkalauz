import { HttpErrorResponse, HttpStatusCode } from '@angular/common/http';
import { Component, OnInit, inject, input, signal } from '@angular/core';
import { RouterLink } from '@angular/router';

import { Api } from '../../api/api';
import { integrationsCompleteOAuth, integrationsSync } from '../../api/functions';
import { SyncResultDto } from '../../api/models';
import { SOURCE_LABELS, sourceFromPath } from '../../integrations/source-labels';

type Step = 'connecting' | 'syncing' | 'done' | 'failed';

@Component({
  selector: 'app-oauth-callback',
  imports: [RouterLink],
  templateUrl: './oauth-callback.html',
})
export class OAuthCallback implements OnInit {
  private readonly api = inject(Api);

  readonly source = input<string>();
  readonly code = input<string>();
  readonly state = input<string>();
  readonly error = input<string>();

  protected readonly step = signal<Step>('connecting');
  protected readonly message = signal<string | null>(null);
  protected readonly result = signal<SyncResultDto | null>(null);
  protected readonly label = signal('');

  async ngOnInit(): Promise<void> {
    const source = sourceFromPath(this.source());
    this.label.set(source ? SOURCE_LABELS[source] : (this.source() ?? ''));
    const code = this.code();
    const state = this.state();
    if (!source || this.error() || !code || !state) {
      this.fail(
        this.error() === 'access_denied'
          ? 'A csatlakozást elutasítottad, nem történt változás.'
          : 'Hiányos visszairányítás. Indítsd újra a csatlakozást.',
      );
      return;
    }

    try {
      await this.api.invoke(integrationsCompleteOAuth, { source, body: { code, state } });
    } catch (error) {
      this.fail(completeError(error));
      return;
    }

    this.step.set('syncing');
    try {
      this.result.set(await this.api.invoke(integrationsSync, { source }));
      this.step.set('done');
    } catch {
      this.fail('A fiók csatlakozott, de az első szinkronizálás nem sikerült. Próbáld újra az Integrációk oldalon.');
    }
  }

  protected total(result: SyncResultDto): number {
    return result.endpoints.reduce((sum, endpoint) => sum + endpoint.records, 0);
  }

  private fail(message: string): void {
    this.message.set(message);
    this.step.set('failed');
  }
}

function completeError(error: unknown): string {
  if (error instanceof HttpErrorResponse) {
    switch (error.status) {
      case HttpStatusCode.BadRequest:
        return 'A csatlakozási kérés lejárt vagy érvénytelen. Indítsd újra a csatlakozást.';
      case HttpStatusCode.Conflict:
        return 'Ez a fiók már egy másik felhasználóhoz van csatlakoztatva.';
    }
  }
  return 'A csatlakozás nem sikerült. Próbáld újra később.';
}
