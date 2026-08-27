import { ListingStatus, PropertyCondition, PropertyType, Typology } from './enums';

export interface ListingSnapshotRequest {
  propertyListingId: number;
  price: number;
  pricePerM2: number;
  status: ListingStatus;
  snapshotDateUtc: string; // ISO date-time
}

export interface ListingSnapshotResponse {
  id: number;
  propertyListingId: number;
  price: number;
  pricePerM2: number;
  status: ListingStatus;
  snapshotDateUtc: string;
}

export interface PropertyListingRequest {
  marketAreaId: number | null;
  country?: string;
  district?: string;
  municipality?: string;
  town?: string;
  zone?: string | null;
  propertyType: PropertyType;
  typology: Typology;
  sourceName: string;
  sourceUrl: string;
  areaM2: number;
  bathrooms: number;
  floor: number | null;
  totalFloors: number | null;
  hasElevator: boolean | null;
  hasAirConditioning: boolean | null;
  condition: PropertyCondition;
  constructionYear: number | null;
  renovationYear: number | null;
  balconyCount: number;
  hasTerrace: boolean;
  hasGarage: boolean;
  hasParking: boolean;
  hasSwimmingPool: boolean;
  isFurnished: boolean;
  hasSeaView: boolean;
  hasCityView: boolean;
  latitude: number | null;
  longitude: number | null;
  energyCertificate: string | null;
  notes: string | null;
  listingSnapshot: ListingSnapshotRequest;
}

export interface PropertyListingResponse {
  id: number;
  marketAreaId: number;
  marketAreaDistrict: string;
  marketAreaMunicipality: string;
  marketAreaTown: string;
  marketAreaZone: string;
  propertyType: PropertyType;
  typology: Typology;
  sourceName: string;
  sourceUrl: string;
  areaM2: number;
  bathrooms: number;
  floor: number | null;
  totalFloors: number | null;
  hasElevator: boolean | null;
  hasAirConditioning: boolean | null;
  condition: PropertyCondition;
  distanceToBeachMeters: number | null;
  nearestBeachMarkerId: number | null;
  nearestBeachName: string | null;
  constructionYear: number | null;
  renovationYear: number | null;
  balconyCount: number;
  hasTerrace: boolean;
  hasGarage: boolean;
  hasParking: boolean;
  hasSwimmingPool: boolean;
  isFurnished: boolean;
  hasSeaView: boolean;
  hasCityView: boolean;
  latitude: number | null;
  longitude: number | null;
  energyCertificate: string | null;
  notes: string | null;
  // Populated on the create (POST) response; null on GET /{id} today —
  // the repository doesn't eager-load snapshots on that path yet.
  listingSnapshot: ListingSnapshotResponse | null;
}
