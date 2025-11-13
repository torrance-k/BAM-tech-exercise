import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { GetAstronautDutiesByNameResult } from './models';
import { environment } from '../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class AstronautDutyService {
  private readonly baseUrl = environment.apiBaseUrl;

  constructor(private http: HttpClient) {}

  getDutiesByName(name: string): Observable<GetAstronautDutiesByNameResult> {
    const encoded = encodeURIComponent(name.trim());
    return this.http.get<GetAstronautDutiesByNameResult>(
      `${this.baseUrl}/AstronautDuty/${encoded}`
    );
  }
}
