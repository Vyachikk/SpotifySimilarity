import { NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { CommonModule } from '@angular/common';
import { AppRoutingModule } from './app-routing.module';
import { AppComponent } from './app.component';
import { TrackDetailComponent } from './components/track-detail/track-detail.component';
import { TrackListComponent } from './components/track-list/track-list.component';
import { HttpClientModule } from '@angular/common/http';
import { TrackVisualizationComponent } from './components/track-visualization/track-visualization.component';
import { FormsModule } from '@angular/forms';

@NgModule({
  declarations: [
    AppComponent,
    TrackDetailComponent,
    TrackListComponent,
    TrackVisualizationComponent
  ],
  imports: [
    BrowserModule,
    CommonModule,
    AppRoutingModule,
    HttpClientModule,
    FormsModule
  ],
  providers: [],
  bootstrap: [AppComponent]
})
export class AppModule { }
