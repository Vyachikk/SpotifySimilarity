import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { TrackService } from 'src/app/services/track.service';
import { Track } from 'src/app/components/models/track.model';

@Component({
  selector: 'app-track-list',
  templateUrl: './track-list.component.html',
  styleUrls: ['./track-list.component.css']
})
export class TrackListComponent implements OnInit {
  tracks: Track[] = [];
  page: number = 1;
  pageSize: number = 10;
  loading: boolean = false;
  allTracksLoaded: boolean = false;
  error: string | null = null;

  constructor(private trackService: TrackService, private router: Router) { }

  ngOnInit(): void {
    this.loadTracks();
  }

  loadTracks(): void {
    if (this.allTracksLoaded || this.loading) return;

    this.loading = true;
    this.trackService.getTracks(this.page, this.pageSize).subscribe(
      (data: Track[]) => {
        if (data.length < this.pageSize) {
          this.allTracksLoaded = true;
        }

        if (this.page === 1) {
          this.tracks = [];
        }

        this.tracks = [...this.tracks, ...data];
        this.loading = false;
        this.error = null;
      },
      (error) => {
        console.error('Error loading tracks:', error);
        this.loading = false;
        this.error = 'Error loading tracks.';
      }
    );
  }

  prevPage(): void {
    if (this.page > 1 && !this.loading) {
      this.page--;
      this.allTracksLoaded = false;
      this.tracks = [];
      this.loadTracks();
    }
  }

  nextPage(): void {
    if (!this.allTracksLoaded && !this.loading) {
      this.page++;
      this.tracks = [];
      this.loadTracks();
    }
  }

  viewTrack(id: number): void {
    this.router.navigate(['/track', id]);
  }
}
