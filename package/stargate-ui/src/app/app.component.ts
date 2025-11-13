import { Component } from '@angular/core';
import { AstronautDutyService } from './astronaut-duty.service';
import { GetAstronautDutiesByNameResult, AstronautDuty } from './models';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.css']
})
export class AppComponent {
  title = 'Stargate Astronaut Duties';

  nameInput = '';
  loading = false;
  errorMessage: string | null = null;
  result: GetAstronautDutiesByNameResult | null = null;
  hasSearched = false;

  constructor(private dutyService: AstronautDutyService) {}

  search(): void {
    const name = this.nameInput.trim();
    this.errorMessage = null;
    this.result = null;
    this.hasSearched = true;

    if (!name) {
      this.errorMessage = 'Name is required.';
      return;
    }

    this.loading = true;

    this.dutyService.getDutiesByName(name).subscribe({
      next: res => {
        this.loading = false;
        this.result = res;

        if (!res.success) {
          this.errorMessage = res.message || 'Request failed.';
        }
      },
      error: err => {
        console.error('API error', err);
        this.loading = false;

        if (err?.error?.message) {
          this.errorMessage = err.error.message;
        } else if (err.status) {
          this.errorMessage = `Request failed with status ${err.status}.`;
        } else {
          this.errorMessage = 'An unexpected network error occurred.';
        }
      }

    });
  }

  hasDuties(): boolean {
    return !!this.result
      && !!this.result.person
      && this.result.astronautDuties
      && this.result.astronautDuties.length > 0;
  }

  sortedDuties(): AstronautDuty[] {
    if (!this.result) {
      return [];
    }
    return [...this.result.astronautDuties].sort((a, b) =>
      a.dutyStartDate < b.dutyStartDate ? 1 : a.dutyStartDate > b.dutyStartDate ? -1 : 0
    );
  }
}
