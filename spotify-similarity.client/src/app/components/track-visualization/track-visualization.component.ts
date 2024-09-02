import { Component, Input, OnChanges, ElementRef, ViewChild, AfterViewInit, HostListener } from '@angular/core';
import { Router } from '@angular/router';
import { Track } from '../models/track.model';

@Component({
  selector: 'app-track-visualization',
  templateUrl: './track-visualization.component.html',
  styleUrls: ['./track-visualization.component.css']
})
export class TrackVisualizationComponent implements OnChanges, AfterViewInit {
  @Input() tracks: { track: Track, similarity: number }[] = [];
  @Input() xMetric: keyof Track = 'spotifyStreams';
  @Input() yMetric: keyof Track = 'allTimeRank';

  @ViewChild('canvas', { static: true }) canvasRef!: ElementRef<HTMLCanvasElement>;
  private ctx!: CanvasRenderingContext2D;
  private circleData: { x: number, y: number, radius: number, track: Track }[] = [];

  private scale = 1;
  private panX = 0;
  private panY = 0;
  private isPanning = false;
  private startX = 0;
  private startY = 0;

  tooltipVisible = false;
  tooltipX = 0;
  tooltipY = 0;
  tooltipTrack: Track | null = null;
  hoveredCircle: { x: number, y: number, radius: number, track: Track } | null = null;

  constructor(private router: Router) { }

  ngOnChanges(): void {
    if (this.tracks.length) {
      this.drawChart();
    }
  }

  ngAfterViewInit(): void {
    this.setupCanvas();
    this.drawChart();
  }

  setupCanvas(): void {
    const canvas = this.canvasRef.nativeElement;
    const dpr = window.devicePixelRatio || 1;
    canvas.width = canvas.clientWidth * dpr;
    canvas.height = canvas.clientWidth * dpr;
    this.ctx = canvas.getContext('2d')!;
    this.ctx.scale(dpr, dpr);
  }

  @HostListener('window:resize', ['$event'])
  onResize(): void {
    this.setupCanvas();
    this.drawChart();
  }

  drawChart(): void {
    const canvas = this.canvasRef.nativeElement;
    this.ctx.clearRect(0, 0, canvas.width, canvas.height);

    this.ctx.save();
    this.ctx.translate(this.panX, this.panY);
    this.ctx.scale(this.scale, this.scale);

    const maxX = Math.max(...this.tracks.map(t => t.track[this.xMetric] as number));
    const minX = Math.min(...this.tracks.map(t => t.track[this.xMetric] as number));
    const maxY = Math.max(...this.tracks.map(t => t.track[this.yMetric] as number));
    const minY = Math.min(...this.tracks.map(t => t.track[this.yMetric] as number));

    const maxRating = 1000;
    const minRating = 0;

    const gridSize = 10; // grid size
    this.circleData = [];

    this.ctx.strokeStyle = '#e0e0e0';
    for (let x = 0; x <= canvas.width; x += canvas.width / gridSize) {
      this.ctx.beginPath();
      this.ctx.moveTo(x, 0);
      this.ctx.lineTo(x, canvas.height);
      this.ctx.stroke();
    }
    for (let y = 0; y <= canvas.height; y += canvas.height / gridSize) {
      this.ctx.beginPath();
      this.ctx.moveTo(0, y);
      this.ctx.lineTo(canvas.width, y);
      this.ctx.stroke();
    }

    this.tracks.forEach(({ track }) => {
      const x = ((track[this.xMetric] as number) - minX) / (maxX - minX) * canvas.clientWidth;
      const y = canvas.clientHeight - ((track[this.yMetric] as number) - minY) / (maxY - minY) * canvas.clientHeight;
      const size = Math.min(Math.sqrt(track.spotifyStreams) / 1000 * this.scale, 20); // Scale the size of circles with zoom level, limit max size
      const hue = ((track.trackScore - minRating) / (maxRating - minRating)) * 120;
      const color = `hsl(${hue}, 100%, 50%)`;

      this.circleData.push({ x, y, radius: size, track });

      this.ctx.beginPath();
      this.ctx.arc(x, y, size, 0, Math.PI * 2);
      this.ctx.fillStyle = color;
      this.ctx.fill();

      if (this.hoveredCircle && this.hoveredCircle.track.id === track.id) {
        this.ctx.strokeStyle = 'black';
        this.ctx.lineWidth = 3;
        this.ctx.stroke();
      }
    });

    this.ctx.restore();
  }

  onCanvasClick(event: MouseEvent): void {
    const canvas = this.canvasRef.nativeElement;
    const rect = canvas.getBoundingClientRect();
    const x = (event.clientX - rect.left - this.panX) / this.scale;
    const y = (event.clientY - rect.top - this.panY) / this.scale;

    for (const { x: cx, y: cy, radius, track } of this.circleData) {
      const distance = Math.sqrt((x - cx) ** 2 + (y - cy) ** 2);
      if (distance <= radius) {
        this.router.navigate(['/track', track.id]);
        break;
      }
    }
  }

  onCanvasWheel(event: WheelEvent): void {
    event.preventDefault();
    const zoom = event.deltaY > 0 ? 0.9 : 1.1;
    const canvas = this.canvasRef.nativeElement;
    const rect = canvas.getBoundingClientRect();
    const mouseX = (event.clientX - rect.left - this.panX) / this.scale;
    const mouseY = (event.clientY - rect.top - this.panY) / this.scale;

    this.panX -= mouseX * (zoom - 1);
    this.panY -= mouseY * (zoom - 1);

    this.scale *= zoom;
    this.drawChart();
  }

  onCanvasMouseDown(event: MouseEvent): void {
    this.isPanning = true;
    this.startX = event.clientX - this.panX;
    this.startY = event.clientY - this.panY;
  }

  onCanvasMouseUp(event: MouseEvent): void {
    this.isPanning = false;
  }

  onCanvasMouseMove(event: MouseEvent): void {
    if (this.isPanning) {
      this.panX = event.clientX - this.startX;
      this.panY = event.clientY - this.startY;
      this.drawChart();
    } else {
      this.checkHover(event);
    }
  }

  onCanvasMouseLeave(event: MouseEvent): void {
    this.isPanning = false;
    this.tooltipVisible = false;
    this.hoveredCircle = null;
    this.drawChart();
  }

  checkHover(event: MouseEvent): void {
    const canvas = this.canvasRef.nativeElement;
    const rect = canvas.getBoundingClientRect();
    const x = (event.clientX - rect.left - this.panX) / this.scale;
    const y = (event.clientY - rect.top - this.panY) / this.scale;

    let hovered = null;
    for (const circle of this.circleData) {
      const distance = Math.sqrt((x - circle.x) ** 2 + (y - circle.y) ** 2);
      if (distance <= circle.radius) {
        hovered = circle;
        break;
      }
    }

    if (hovered) {
      this.tooltipVisible = true;
      this.tooltipX = event.clientX - rect.left + 600;
      this.tooltipY = event.clientY - rect.top + 600;
      this.tooltipTrack = hovered.track;
      this.hoveredCircle = hovered;
    } else {
      this.tooltipVisible = false;
      this.tooltipTrack = null;
      this.hoveredCircle = null;
    }

    this.drawChart();
  }
}
