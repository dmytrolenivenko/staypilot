import { ListingStatus, PropertyCondition, PropertyType, SortBy, Typology } from './enums';
import { PropertyListingResponse } from './property-listing';

// What we POST to /api/ListPropertyListing. Every filter is optional — leave a field
// out (undefined) and the API simply doesn't filter on it. Matches the C# request
// ListPropertyListingRequest.cs field-for-field.
export interface ListPropertyListingRequest {
  location?: string;
  propertyType?: PropertyType;
  typology?: Typology;
  condition?: PropertyCondition;
  listingStatus?: ListingStatus;

  minPrice?: number;
  maxPrice?: number;
  minAreaM2?: number;
  maxAreaM2?: number;
  maxPricePerM2?: number;
  distanceToBeachMeters?: number;

  hasGarage?: boolean;
  hasSwimmingPool?: boolean;
  hasSeaView?: boolean;
  hasElevator?: boolean;

  // Sorting + paging are always sent.
  sortBy: SortBy;
  sortDescending: boolean;
  pageNumber: number;
  pageSize: number;
}

// What the API sends back: one page of listings plus the total count (for paging).
export interface ListPropertyListingResponse {
  items: PropertyListingResponse[];
  pageNumber: number;
  pageSize: number;
  totalRecords: number;
}
