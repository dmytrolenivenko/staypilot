import { PropertyCondition } from './enums';
import { PropertyListingResponse } from './property-listing';

export interface TopDealsQuery {
  district?: string;
  municipality?: string;
  condition?: PropertyCondition;
  count: number;
}

export interface TopDealResponse {
  listing: PropertyListingResponse;
  townMedianPricePerM2: number;
  discountPercent: number;
}

export interface TopDealsResponse {
  items: TopDealResponse[];
  calculatedAtUtc: string | null;
}
