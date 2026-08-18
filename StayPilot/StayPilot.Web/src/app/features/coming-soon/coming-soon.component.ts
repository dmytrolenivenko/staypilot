import { Component } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { ComingSoonInfo } from '../../core/models/coming-soon-info';
import { PageHeaderComponent } from '../../shared/page-header.component';

@Component({
  selector: 'app-coming-soon',
  standalone: true,
  imports: [PageHeaderComponent],
  templateUrl: './coming-soon.component.html',
  styleUrl: './coming-soon.component.css'
})
export class ComingSoonComponent {
  info: ComingSoonInfo;

  constructor(route: ActivatedRoute) {
    this.info = route.snapshot.data['info'];
  }
}
