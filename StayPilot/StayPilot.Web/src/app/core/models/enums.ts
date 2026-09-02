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

// value = what we send to the API, label = what the user reads. Same shape as SORT_OPTIONS.
// The wire names are C# identifiers - "NeedsRenovation", "NewBuild", "PriceChanged" - and
// putting them straight into a dropdown shows the user our enum rather than their language.
export const PROPERTY_CONDITION_OPTIONS: { value: PropertyCondition; label: string }[] = [
  { value: 'Unknown', label: 'Not stated' },
  { value: 'NeedsRenovation', label: 'Needs renovation' },
  { value: 'Used', label: 'Used' },
  { value: 'Good', label: 'Good' },
  { value: 'Renovated', label: 'Renovated' },
  { value: 'NewBuild', label: 'New build' }
];

export const LISTING_STATUS_OPTIONS: { value: ListingStatus; label: string }[] = [
  { value: 'Sold', label: 'Sold' },
  { value: 'Active', label: 'Active' },
  { value: 'PriceChanged', label: 'Price changed' }
];

// The field the API can sort the listing browser by. Names match StayPilot.Domain/Enums/SortBy.cs.
export type SortBy = 'Id' | 'Price' | 'PricePerM2' | 'AreaM2' | 'CreatedAtUtc' | 'DistanceToBeachMeters';

// value = what we send to the API, label = what the user reads in the dropdown.
export const SORT_OPTIONS: { value: SortBy; label: string }[] = [
  { value: 'Id', label: 'Newest (id)' },
  { value: 'Price', label: 'Price' },
  { value: 'PricePerM2', label: 'Price per m²' },
  { value: 'AreaM2', label: 'Area' },
  { value: 'DistanceToBeachMeters', label: 'Distance to beach' },
  { value: 'CreatedAtUtc', label: 'Date added' }
];
