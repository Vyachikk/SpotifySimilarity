import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { TrackListComponent } from './components/track-list/track-list.component';
import { TrackDetailComponent } from './components/track-detail/track-detail.component';

const routes: Routes = [
  { path: '', redirectTo: '/tracks', pathMatch: 'full' },
  { path: 'tracks', component: TrackListComponent },
  { path: 'track/:id', component: TrackDetailComponent }
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }
