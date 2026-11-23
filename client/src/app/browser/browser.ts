import { DOCUMENT, Injectable, inject } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class Browser {
  private readonly document = inject(DOCUMENT);

  navigate(url: string): void {
    this.document.location.assign(url);
  }

  confirm(message: string): boolean {
    return this.document.defaultView?.confirm(message) ?? false;
  }
}
