import { DatePipe } from '@angular/common';
import { Component, inject, resource } from '@angular/core';

import { Api } from '../../api/api';
import { usersList } from '../../api/functions';

@Component({
  selector: 'app-home',
  imports: [DatePipe],
  templateUrl: './home.html',
})
export class Home {
  private readonly api = inject(Api);

  protected readonly users = resource({ loader: () => this.api.invoke(usersList) });
}
