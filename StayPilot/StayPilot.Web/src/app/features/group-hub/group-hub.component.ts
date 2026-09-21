import { Component } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { NavGroup } from '../../core/models/nav-groups';
import { PageHeaderComponent } from '../../shared/page-header.component';

// One component for all four group hubs (Listings / Market areas / Portfolio / Tools). Clicking
// a header trigger used to only open the hover dropdown, which meant you saw one option at a
// time instead of everything that group holds — this is the "on the page" version of that same
// dropdown, reached by a real route so it also gets its own back button and URL.
@Component({
  selector: 'app-group-hub',
  standalone: true,
  imports: [RouterLink, PageHeaderComponent],
  templateUrl: './group-hub.component.html',
  styleUrl: './group-hub.component.css'
})
export class GroupHubComponent {
  group: NavGroup;

  constructor(route: ActivatedRoute) {
    this.group = route.snapshot.data['group'];
  }
}
