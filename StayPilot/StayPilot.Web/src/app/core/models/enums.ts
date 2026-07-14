// These string unions mirror the wire format of the .NET enums exactly
// (the API serializes enums as strings via JsonStringEnumConverter).
// Keep in sync with StayPilot.Domain/Enums on the API side.

export type PropertyType = 'Apartment' | 'Villa' | 'House' | 'Land';

export const PROPERTY_TYPES: PropertyType[] = ['Apartment', 'Villa', 'House', 'Land'];

export type Typology =
  | 'T0' | 'T1' | 'T2' | 'T3' | 'T4' | 'T5'
  | 'T6' | 'T7' | 'T8' | 'T9' | 'T10';

export const TYPOLOGIES: Typology[] = [
  'T0', 'T1', 'T2', 'T3', 'T4', 'T5', 'T6', 'T7', 'T8', 'T9', 'T10'
];

export type PropertyCondition =
  | 'Unknown' | 'NeedsRenovation' | 'Used' | 'Good' | 'Renovated' | 'NewBuild';

export const PROPERTY_CONDITIONS: PropertyCondition[] = [
  'Unknown', 'NeedsRenovation', 'Used', 'Good', 'Renovated', 'NewBuild'
];

export type ListingStatus = 'Sold' | 'Active' | 'PriceChanged';

export const LISTING_STATUSES: ListingStatus[] = ['Sold', 'Active', 'PriceChanged'];
