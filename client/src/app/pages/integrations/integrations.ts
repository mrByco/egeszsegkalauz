import { Component, inject, resource } from '@angular/core';

import { Api } from '../../api/api';
import { integrationsList } from '../../api/functions';
import { IntegrationDto } from '../../api/models';
import { IntegrationCard } from './integration-card';

@Component({
  selector: 'app-integrations',
  imports: [IntegrationCard],
  templateUrl: './integrations.html',
})
export class Integrations {
  private readonly api = inject(Api);

  protected readonly integrations = resource({ loader: () => this.api.invoke(integrationsList) });

  protected replace(updated: IntegrationDto): void {
    this.integrations.update((all) => all?.map((x) => (x.source === updated.source ? updated : x)));
  }
}
