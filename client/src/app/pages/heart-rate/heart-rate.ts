import { DatePipe } from '@angular/common';
import { Component, inject, resource, signal } from '@angular/core';

import { Api } from '../../api/api';
import { heartRateDaily } from '../../api/functions';
import { SOURCE_LABELS } from '../../integrations/source-labels';

@Component({
  selector: 'app-heart-rate',
  imports: [DatePipe],
  templateUrl: './heart-rate.html',
})
export class HeartRate {
  private readonly api = inject(Api);

  protected readonly sourceLabels = SOURCE_LABELS;
  protected readonly dayOptions = [7, 30, 90];
  protected readonly days = signal(30);

  protected readonly daily = resource({
    params: () => this.days(),
    loader: ({ params: days }) => this.api.invoke(heartRateDaily, { days }),
  });
}
