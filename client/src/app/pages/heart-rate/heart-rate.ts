import { DatePipe } from '@angular/common';
import { Component, inject, resource, signal } from '@angular/core';

import { Api } from '../../api/api';
import { garminHeartRateDays } from '../../api/functions';

@Component({
  selector: 'app-heart-rate',
  imports: [DatePipe],
  templateUrl: './heart-rate.html',
})
export class HeartRate {
  private readonly api = inject(Api);

  protected readonly dayOptions = [7, 30, 90];
  protected readonly days = signal(30);

  protected readonly heartRateDays = resource({
    params: () => this.days(),
    loader: ({ params: days }) => this.api.invoke(garminHeartRateDays, { days }),
  });
}
