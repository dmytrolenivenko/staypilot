import { PropertyCondition, PropertyType, Typology } from './enums';

// Mirrors StayPilot.Application.Contracts.Request.OwnedPropertyRequest.
// Name, PropertyType, Typology and AreaM2 are required by the API.
export interface OwnedPropertyRequest {
  name: string;
  country?: string;
  district: string;
  municipality: string;
  town: string;
  zone?: string | null;
  purchasePrice?: number | null;
  purchaseDate?: string | null; // ISO date
  propertyType: PropertyType;
  typology: Typology;
  areaM2: number;
  bathrooms: number;
  floor?: number | null;
  totalFloors?: number | null;
  hasElevator?: boolean | null;
  hasAirConditioning?: boolean | null;
  condition?: PropertyCondition | null;
  constructionYear?: number | null;
  renovationYear?: number | null;
  renovationInvestment?: number | null;
  balconyCount?: number | null;
  hasTerrace?: boolean | null;
  hasGarage?: boolean | null;
  hasParking?: boolean | null;
  hasSwimmingPool?: boolean | null;
  isFurnished?: boolean | null;
  hasSeaView?: boolean | null;
  hasCityView?: boolean | null;
  latitude?: number | null;
  longitude?: number | null;
  energyCertificate?: string | null;
  notes?: string | null;
}

// Mirrors StayPilot.Application.Contracts.Response.OwnedPropertyResponse.
export interface OwnedPropertyResponse {
  id: number;
  name: string;
  marketAreaId: number;
  purchasePrice?: number | null;
  purchaseDate?: string | null;
  propertyType: PropertyType;
  typology: Typology;
  areaM2: number;
  bathrooms: number;
  floor?: number | null;
  totalFloors?: number | null;
  hasElevator?: boolean | null;
  hasAirConditioning?: boolean | null;
  condition?: PropertyCondition | null;
  constructionYear?: number | null;
  renovationYear?: number | null;
  renovationInvestment?: number | null;
  balconyCount?: number | null;
  hasTerrace?: boolean | null;
  hasGarage?: boolean | null;
  hasParking?: boolean | null;
  hasSwimmingPool?: boolean | null;
  isFurnished?: boolean | null;
  hasSeaView?: boolean | null;
  hasCityView?: boolean | null;
  distanceToBeachMeters?: number | null;
  nearestBeachMarkerId?: number | null;
  nearestBeachName?: string | null;
  energyCertificate?: string | null;
  notes?: string | null;
}
