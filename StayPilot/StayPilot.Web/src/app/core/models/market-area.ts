export interface MarketArea {
  id: number;
  country: string;
  district: string;
  municipality: string;
  town: string;
  zone: string | null;
  notes: string | null;
}
