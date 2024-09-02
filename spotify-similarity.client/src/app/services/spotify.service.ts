import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';

@Injectable({
  providedIn: 'root'
})
export class SpotifyService {
  private accessToken!: string;

  constructor(private http: HttpClient) {
    this.getAccessToken();
  }

  private getAccessToken(): void {
    const clientId = 'f61b04ffcace44ebb224346f05de8e3d';
    const clientSecret = 'fb02f6f95c2a4d26b1174047eaf2f277';
    const auth = btoa(`${clientId}:${clientSecret}`);

    this.http.post('https://accounts.spotify.com/api/token', 'grant_type=client_credentials', {
      headers: new HttpHeaders({
        'Content-Type': 'application/x-www-form-urlencoded',
        'Authorization': `Basic ${auth}`
      })
    }).subscribe((res: any) => {
      this.accessToken = res.access_token;
    });
  }

  searchTrack(trackName: string, artist: string): Observable<any> {
    const headers = new HttpHeaders({
      'Authorization': `Bearer ${this.accessToken}`
    });

    return this.http.get(`https://api.spotify.com/v1/search?q=track:${trackName} artist:${artist}&type=track`, { headers })
      .pipe(
        map((res: any) => {
          if (res.tracks.items.length > 0) {
            const track = res.tracks.items[0];
            return {
              id: track.id,
              image: track.album.images[0]?.url || null
            };
          } else {
            return null;
          }
        })
      );
  }
}
