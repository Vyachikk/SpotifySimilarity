import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Track } from '../components/models/track.model';

@Injectable({
  providedIn: 'root'
})
export class TrackService {
  private apiUrl = 'https://localhost:7134/api/Tracks';

  constructor(private http: HttpClient) { }

  getTracks(pageNumber: number, pageSize: number): Observable<Track[]> {
    return this.http.get<Track[]>(`${this.apiUrl}?pageNumber=${pageNumber}&pageSize=${pageSize}`);
  }

  getTrackById(id: number): Observable<{ track: Track, recommendations: Track[] }> {
    return this.http.get<{ track: Track, recommendations: Track[] }>(`${this.apiUrl}/${id}`);
  }

  getSimilarTracks(id: number, topN: number): Observable<{ track: Track, similarity: number }[]> {
    return this.http.get<{ track: Track, similarity: number }[]>(`${this.apiUrl}/${id}/similar?topN=${topN}`);
  }
}
