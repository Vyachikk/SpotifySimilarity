import { Component, OnInit, ViewChild, ElementRef } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { TrackService } from 'src/app/services/track.service';
import { Track } from '../models/track.model';

@Component({
  selector: 'app-track-detail',
  templateUrl: './track-detail.component.html',
  styleUrls: ['./track-detail.component.css']
})
export class TrackDetailComponent implements OnInit {
  track: Track | null = null;
  similarTracks: { track: Track, similarity: number }[] = [];
  recommendations: Track[] = [];
  error: string | null = null;
  xMetric: keyof Track = 'spotifyStreams'; // Default metric for X axis
  yMetric: keyof Track = 'allTimeRank'; // Default metric for Y axis

  @ViewChild('audio') audioRef: ElementRef<HTMLAudioElement> | undefined;

  constructor(
    private route: ActivatedRoute,
    private trackService: TrackService
  ) { }

  ngOnInit(): void {
    this.route.paramMap.subscribe(params => {
      const id = params.get('id');
      if (id) {
        this.loadTrack(Number(id));
      }
    });
  }

  loadTrack(id: number): void {
    this.trackService.getTrackById(id).subscribe(
      (data: { track: Track, recommendations: Track[] }) => {
        this.track = data.track;
        this.recommendations = data.recommendations;
        this.loadSimilarTracks(id);
        this.updateAudioPreview(); // Update audio when track changes
      },
      (error) => {
        console.error('Error loading track:', error);
        this.error = 'Error loading track.';
      }
    );
  }

  loadSimilarTracks(id: number): void {
    this.trackService.getSimilarTracks(id, 50).subscribe(
      (data: { track: Track, similarity: number }[]) => {
        this.similarTracks = [{ track: this.track!, similarity: 1 }, ...data];
      },
      (error) => {
        console.error('Error loading similar tracks:', error);
        this.error = 'Error loading similar tracks.';
      }
    );
  }

  private updateAudioPreview(): void {
    if (this.audioRef && this.track && this.track.previewUrl) {
      const audio = this.audioRef.nativeElement;
      audio.src = this.track.previewUrl;
      audio.load();
    }
  }
}
