import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { CharacterOverview, TimeSpanLeaderboard } from '../../models/high-scores.models';

@Injectable({
  providedIn: 'root'
})
export class HighScoreApiService {
  private readonly httpClient: HttpClient = inject(HttpClient);

  getLast24HourLeaderboard(): Observable<TimeSpanLeaderboard> {
    return this.httpClient.get<TimeSpanLeaderboard>('https://localhost:7071/HighScore/last24HourLeaderboard');
  }

  getCharacterOverview(characterName: string): Observable<CharacterOverview> {
    return this.httpClient.get<CharacterOverview>(`https://localhost:7071/HighScore/${characterName}`);
  }

  /*
  postItem(payload: any): Observable<any> {
    return this.httpClient.post('https://api.example.com/items', payload);
  }
    */
}
